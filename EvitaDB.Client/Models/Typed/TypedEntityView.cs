using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Base class for generated typed views. It carries all of the plumbing - the bound contract, the read
/// source, the lazily materialized builder, nested-view memoization and the editor mixin - so a generated
/// implementation contains nothing but member forwarding.
/// <remarks>
/// <para>
/// This is what makes the generated path strictly more capable than the runtime proxy: a real class can
/// implement an interface *partially*, leaving default interface members to be inherited. The runtime
/// proxy intercepts those members and cannot delegate back to them without <c>Reflection.Emit</c>.
/// </para>
/// <para>
/// Generated code addresses members by name rather than by <c>MethodInfo</c>, which keeps it free of
/// reflection and therefore trim- and AOT-safe.
/// </para>
/// </remarks>
/// </summary>
public abstract class TypedEntityView : ITypedEntity, ITypedEntityEditor
{
    private object _source;
    private IEntityBuilder? _builder;
    private Dictionary<string, object?>? _nestedViews;
    private List<TypedReferenceAttachment>? _attachments;
    private IEntityBuilder? _referenceOwner;

    /// <summary>
    /// Initializes the view.
    /// </summary>
    /// <param name="contract">the contract bound to the source's schema</param>
    /// <param name="source">the entity, entity builder or reference to read from</param>
    protected TypedEntityView(BoundTypedContract contract, object source)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    /// <summary>The bound contract this view reads and writes through.</summary>
    protected BoundTypedContract Contract { get; }

    /// <summary>
    /// The source reads go to. Once a builder exists it supersedes the original entity, so a value just
    /// written reads back immediately.
    /// </summary>
    protected object ReadSource => _builder ?? _source;

    /// <inheritdoc />
    public ISealedEntity Entity => (_builder as ISealedEntity)
                                   ?? _source as ISealedEntity
                                   ?? throw new EvitaInvalidUsageException(
                                       $"`{Contract.Descriptor.ContractType.Name}` is not bound to an " +
                                       $"entity, so it exposes none.");

    /// <inheritdoc />
    public Type ContractType => Contract.Descriptor.ContractType;

    /// <inheritdoc />
    public IEntityBuilder EntityBuilder => Builder;

    /// <inheritdoc />
    public bool HasChanges => _builder is not null && Normalize(_builder.ToMutation()) is not null;

    /// <inheritdoc />
    public IEntityMutation? ToMutation() => Normalize(Builder.ToMutation());

    /// <summary>
    /// Drops null local mutations, and reports "no mutation" when nothing survives.
    /// <remarks>
    /// The driver's change-set builders can contribute a null local mutation - Java filters those out with
    /// <c>filter(Objects::nonNull)</c> and this port dropped it in several places. Guarding here as well as
    /// in the builder means the typed API keeps its promise (an untouched editor produces nothing, and a
    /// mutation never carries a null) whatever driver build it is running against.
    /// </remarks>
    /// </summary>
    /// <param name="mutation">the builder's mutation, if any</param>
    public static IEntityMutation? Normalize(IEntityMutation? mutation)
    {
        if (mutation is not EntityUpsertMutation upsert)
        {
            return mutation;
        }

        var real = upsert.LocalMutations.Where(it => it is not null).ToList();

        if (real.Count == 0)
        {
            return null;
        }

        return real.Count == upsert.LocalMutations.Count
            ? mutation
            : new EntityUpsertMutation(upsert.EntityType, upsert.EntityPrimaryKey, upsert.EntityExistence, real);
    }

    /// <inheritdoc />
    public Entity ToInstance() => Builder.ToInstance();

    /// <inheritdoc />
    public EntityReference UpsertVia(EvitaClientSession session) => Builder.UpsertVia(session);

    /// <inheritdoc />
    public IReadOnlyList<TypedReferenceAttachment> Attachments =>
        _attachments ?? (IReadOnlyList<TypedReferenceAttachment>)[];

    /// <inheritdoc />
    public void AttachReferenced(string referenceName, ITypedEntityEditor referenced)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceName);
        ArgumentNullException.ThrowIfNull(referenced);

        (_attachments ??= []).Add(new TypedReferenceAttachment(referenceName, referenced));
    }

    /// <inheritdoc />
    public IReadOnlyList<EntityReference> UpsertDeeplyWith(Func<IEntityBuilder, EntityReference> upsert) =>
        TypedDeepUpsert.Upsert(this, upsert);

    /// <inheritdoc />
    public IReadOnlyList<EntityReference> UpsertDeeplyVia(EvitaClientSession session) =>
        UpsertDeeplyWith(builder => builder.UpsertVia(session));

    /// <summary>
    /// Remembers the entity builder that owns the reference this view reads, enabling attribute writes.
    /// </summary>
    /// <param name="owner">builder of the owning entity, or <c>null</c> when read-only</param>
    internal void AttachReferenceOwner(IEntityBuilder? owner) => _referenceOwner = owner;

    /// <summary>
    /// The builder collecting mutations, materialized on first use.
    /// </summary>
    protected IEntityBuilder Builder => _builder ??= _source switch
    {
        IEntityBuilder builder => builder,
        Entity entity => new ExistingEntityBuilder(entity),
        _ => throw new EvitaInvalidUsageException(
            $"`{Contract.Descriptor.ContractType.Name}` cannot be edited: it is bound to " +
            $"`{_source.GetType().Name}`, which carries no entity to build upon.")
    };

    /// <summary>
    /// Reads the named contract member. Generated members call this.
    /// </summary>
    /// <param name="memberName">name of the member being read</param>
    /// <param name="arguments">invocation arguments, if any</param>
    protected object? Read(string memberName, object?[]? arguments = null)
    {
        // Nested views are memoized for stable identity, but only over an immutable source. Once a builder
        // exists the entity is being edited, and a cached view would keep reporting the state the reference
        // had before the last write.
        if (!Contract.IsMemoizableMember(memberName) || ReadSource is IEntityBuilder)
        {
            return Contract.ReadByMember(memberName, ReadSource, arguments);
        }

        _nestedViews ??= new Dictionary<string, object?>(StringComparer.Ordinal);

        if (_nestedViews.TryGetValue(memberName, out var cached))
        {
            return cached;
        }

        var value = Contract.ReadByMember(memberName, ReadSource, arguments);
        _nestedViews[memberName] = value;
        return value;
    }

    /// <summary>
    /// Writes the named contract member. Generated members call this.
    /// </summary>
    /// <param name="memberName">name of the member being written</param>
    /// <param name="arguments">invocation arguments</param>
    protected void Write(string memberName, object?[]? arguments = null)
    {
        // a reference contract writes the reference's own attributes, through the owning entity's builder
        if (_source is IReference reference && Contract.IsReferenceAttributeWriter(memberName))
        {
            Contract.WriteReferenceAttribute(
                memberName,
                _referenceOwner ?? throw new EvitaInvalidUsageException(
                    $"`{ContractType.Name}` was read from an entity that is not open for writing, so its " +
                    $"reference attributes cannot be changed - open the owning entity for write first."),
                reference.ReferenceName,
                reference.ReferencedPrimaryKey,
                arguments
            );

            _nestedViews?.Clear();
            return;
        }

        Contract.WriteByMember(memberName, Builder, arguments);

        // nested views handed out earlier may no longer match the builder's view
        _nestedViews?.Clear();
    }

    /// <inheritdoc />
    public override string ToString() => ReadSource switch
    {
        IEntity entity => $"{ContractType.Name}({entity.Type}:{entity.PrimaryKey})",
        IReference reference =>
            $"{ContractType.Name}({reference.ReferenceName}:{reference.ReferencedPrimaryKey})",
        _ => base.ToString()!
    };

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(ContractType, _source);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is TypedEntityView other
            && other.ContractType == ContractType
            && ReferenceEquals(other._source, _source));
}

/// <summary>
/// Registry of generated typed-view implementations. Generated code registers itself from a module
/// initializer, so no assembly scanning or reflection is involved.
/// </summary>
public static class TypedViewRegistry
{
    private static readonly Dictionary<Type, Func<BoundTypedContract, object, object>> Factories = new();
    private static readonly Lock Gate = new();

    /// <summary>
    /// Registers the implementation to use for a contract type. Later registrations win, so a project can
    /// override a generated implementation.
    /// </summary>
    /// <param name="contractType">the contract the implementation satisfies</param>
    /// <param name="factory">creates an instance from a bound contract and a read source</param>
    public static void Register(Type contractType, Func<BoundTypedContract, object, object> factory)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(factory);

        lock (Gate)
        {
            Factories[contractType] = factory;
        }
    }

    /// <summary>
    /// Returns the registered implementation factory for a contract, or <c>null</c> when the contract has
    /// none and the runtime proxy should be used instead.
    /// </summary>
    /// <param name="contractType">the contract to look up</param>
    public static Func<BoundTypedContract, object, object>? Find(Type contractType)
    {
        lock (Gate)
        {
            return Factories.GetValueOrDefault(contractType);
        }
    }

    /// <summary>Number of registered implementations, for diagnostics.</summary>
    public static int Count
    {
        get
        {
            lock (Gate)
            {
                return Factories.Count;
            }
        }
    }
}
