using System.Reflection;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Optional mixin a typed contract can extend to reach the entity behind the typed view.
/// <remarks>
/// Counterpart of the Java driver's <c>EvitaProxy</c> / <c>WithEntityContract</c> mixins. Extending it
/// is opt-in: contracts that do not need the raw entity stay free of driver types.
/// </remarks>
/// </summary>
public interface ITypedEntity
{
    /// <summary>The fetched entity this typed view reads from.</summary>
    ISealedEntity Entity { get; }

    /// <summary>The contract type this instance was created for.</summary>
    Type ContractType { get; }
}

/// <summary>
/// Mixin an editor contract extends to reach the builder collecting its mutations, and to hand the
/// result to a session.
/// <remarks>
/// Prices and references are written through <see cref="EntityBuilder"/> rather than through generated
/// setters: each needs several coordinated values, and inventing a setter shape for them would be less
/// clear than the driver's own fluent builder.
/// </remarks>
/// </summary>
public interface ITypedEntityEditor
{
    /// <summary>
    /// The builder collecting mutations, materialized on first access. Reads through the contract see the
    /// builder's view, so a value written here is visible immediately.
    /// </summary>
    IEntityBuilder EntityBuilder { get; }

    /// <summary>True when at least one mutating member has been invoked.</summary>
    bool HasChanges { get; }

    /// <summary>
    /// The accumulated mutation, or <c>null</c> when nothing changed - in which case there is nothing to
    /// send to the server.
    /// </summary>
    IEntityMutation? ToMutation();

    /// <summary>The entity as it would look with the accumulated mutations applied.</summary>
    Entity ToInstance();

    /// <summary>
    /// Sends the accumulated mutations through the given session and returns the reference to the
    /// upserted entity.
    /// </summary>
    /// <param name="session">an open read-write session</param>
    EntityReference UpsertVia(EvitaClientSession session);

    /// <summary>Editors attached to this one, to be upserted together with it.</summary>
    IReadOnlyList<TypedReferenceAttachment> Attachments { get; }

    /// <summary>
    /// Attaches an editor of a referenced entity, so that a later deep upsert writes it first and puts its
    /// primary key into this entity's reference.
    /// <remarks>
    /// This is how an entity and a brand-new entity it points to are written together: the referenced
    /// entity may have no primary key yet, and the server assigns one when it is upserted.
    /// </remarks>
    /// </summary>
    /// <param name="referenceName">name of the reference on this entity</param>
    /// <param name="referenced">editor holding the referenced entity's changes</param>
    void AttachReferenced(string referenceName, ITypedEntityEditor referenced);

    /// <summary>
    /// Upserts this editor together with everything attached to it, children first, using the given
    /// upsert function. Useful when the caller controls how a builder is sent.
    /// </summary>
    /// <param name="upsert">sends one builder and returns the reference to the upserted entity</param>
    /// <returns>references to everything upserted, children before their parents</returns>
    IReadOnlyList<EntityReference> UpsertDeeplyWith(Func<IEntityBuilder, EntityReference> upsert);

    /// <summary>
    /// Upserts this editor together with everything attached to it through the given session.
    /// </summary>
    /// <param name="session">an open read-write session</param>
    /// <returns>references to everything upserted, children before their parents</returns>
    IReadOnlyList<EntityReference> UpsertDeeplyVia(EvitaClientSession session);
}

/// <summary>
/// Creates typed views over fetched entities.
/// <remarks>
/// <para>
/// Binding needs only the entity, because <see cref="IEntity.Schema"/> travels with it - so typed
/// views can be created anywhere an entity is available, without a session.
/// </para>
/// <para>
/// Instances are runtime proxies (<see cref="DispatchProxy"/>) over interface contracts. Contracts
/// declaring default interface members are rejected with a precise error: a proxy cannot delegate to a
/// default implementation without <c>Reflection.Emit</c>, which is unavailable under AOT and in
/// Blazor WebAssembly. Generated contract implementations will support them.
/// </para>
/// </remarks>
/// </summary>
public static class TypedEntityFactory
{
    private static readonly MethodInfo CreateProxyMethod = typeof(DispatchProxy)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(it => it is { Name: nameof(DispatchProxy.Create), IsGenericMethodDefinition: true }
                      && it.GetGenericArguments().Length == 2
                      && it.GetParameters().Length == 0);

    /// <summary>
    /// Creates a typed view of the given entity.
    /// </summary>
    /// <typeparam name="T">an interface contract annotated with <see cref="EvitaEntityAttribute"/>
    /// or <see cref="EvitaEntityRefAttribute"/></typeparam>
    /// <param name="entity">the fetched entity to read from</param>
    public static T Create<T>(ISealedEntity entity) where T : class =>
        (T)Create(typeof(T), entity);

    /// <summary>
    /// Creates a typed view of the given entity.
    /// </summary>
    /// <param name="contractType">an annotated interface contract</param>
    /// <param name="entity">the fetched entity to read from</param>
    /// <exception cref="EvitaInvalidUsageException">
    /// when the contract is not an interface, carries no entity annotation, or maps to a different
    /// entity type than the one supplied
    /// </exception>
    public static object Create(Type contractType, ISealedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(entity);

        if (!contractType.IsInterface && TypedViewRegistry.Find(contractType) is null)
        {
            throw new EvitaInvalidUsageException(
                $"Typed contract `{contractType}` must be an interface, or have a generated implementation " +
                $"registered - the runtime proxy can only implement interfaces."
            );
        }

        var descriptor = TypedContractAnalyzer.Describe(contractType);

        if (!string.Equals(descriptor.EntityType, entity.Type, StringComparison.Ordinal))
        {
            throw new EvitaInvalidUsageException(
                $"Contract `{contractType.Name}` maps to entity `{descriptor.EntityType}` but the " +
                $"supplied entity is of type `{entity.Type}`."
            );
        }

        var contract = BoundTypedContract.Bind(contractType, entity.Schema);
        return Materialize(contractType, contract, entity);
    }

    /// <summary>
    /// Creates the proxy instance backing a typed view over the given source.
    /// </summary>
    private static object Materialize(Type contractType, BoundTypedContract contract, object source)
    {
        // a generated implementation is preferred: it is a real class, so default interface members are
        // inherited rather than intercepted, and it needs no reflection per call
        if (TypedViewRegistry.Find(contractType) is { } generated)
        {
            return generated(contract, source);
        }

        var proxy = CreateProxyMethod
            .MakeGenericMethod(contractType, typeof(TypedEntityProxy))
            .Invoke(null, null)!;

        ((TypedEntityProxy)proxy).Initialize(contract, source, contractType);
        return proxy;
    }

    /// <summary>
    /// Creates a typed editor over a brand-new entity of the contract's collection.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="schema">schema of the entity collection</param>
    /// <param name="primaryKey">
    /// primary key to assign, or <c>null</c> to let the server generate one
    /// </param>
    public static T CreateNew<T>(IEntitySchema schema, int? primaryKey = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(schema);

        var contractType = typeof(T);
        var descriptor = TypedContractAnalyzer.Describe(contractType);

        if (!string.Equals(descriptor.EntityType, schema.Name, StringComparison.Ordinal))
        {
            throw new EvitaInvalidUsageException(
                $"Contract `{contractType.Name}` maps to entity `{descriptor.EntityType}` but the supplied " +
                $"schema describes `{schema.Name}`."
            );
        }

        var builder = new InitialEntityBuilder(schema, primaryKey);
        var contract = BoundTypedContract.Bind(contractType, schema);
        return (T)Materialize(contractType, contract, builder);
    }

    /// <summary>
    /// Creates a typed editor over an already-fetched entity. The entity itself is never mutated; writes
    /// accumulate in a builder created on first use.
    /// </summary>
    /// <typeparam name="T">an annotated interface contract</typeparam>
    /// <param name="entity">the fetched entity to base the changes on</param>
    public static T OpenForWrite<T>(ISealedEntity entity) where T : class =>
        Create<T>(entity);

    /// <summary>
    /// Creates a typed view of a single reference, exposing the reference's own attributes and its
    /// navigation to the referenced entity and group.
    /// </summary>
    /// <param name="contractType">a reference contract interface</param>
    /// <param name="reference">the reference to read from</param>
    /// <param name="referenceSchema">
    /// schema of the reference; when <c>null</c>, annotated members cannot be validated and resolve
    /// leniently
    /// </param>
    /// <param name="owner">the owner of the reference</param>
    public static object CreateReferenceView(
        Type contractType,
        IReference reference,
        IReferenceSchema? referenceSchema,
        IEntityBuilder? owner = null)
    {
        ArgumentNullException.ThrowIfNull(contractType);
        ArgumentNullException.ThrowIfNull(reference);

        if (!contractType.IsInterface && TypedViewRegistry.Find(contractType) is null)
        {
            throw new EvitaInvalidUsageException(
                $"Reference contract `{contractType}` must be an interface, or have a generated " +
                $"implementation registered - the runtime proxy can only implement interfaces."
            );
        }

        var contract = BoundTypedContract.BindReference(
            contractType, referenceSchema ?? reference.ReferenceSchema
        );

        var view = Materialize(contractType, contract, reference);

        // writing a reference attribute goes through the owning entity's builder, so the view needs it
        switch (view)
        {
            case TypedEntityView typedView:
                typedView.AttachReferenceOwner(owner);
                break;
            case TypedEntityProxy proxy:
                proxy.AttachReferenceOwner(owner);
                break;
        }

        return view;
    }

    /// <summary>
    /// Creates a typed view of a single reference, taking the reference schema from the reference itself.
    /// </summary>
    /// <typeparam name="T">a reference contract interface</typeparam>
    /// <param name="reference">the reference to read from</param>
    public static T CreateReferenceView<T>(IReference reference) where T : class =>
        (T)CreateReferenceView(typeof(T), reference, reference.ReferenceSchema);

    /// <summary>
    /// Returns the entity behind a typed view, whether or not the contract extends
    /// <see cref="ITypedEntity"/>.
    /// </summary>
    /// <param name="typedEntity">an instance created by this factory</param>
    /// <exception cref="EvitaInvalidUsageException">when the instance is not a typed view</exception>
    public static ISealedEntity GetEntity(object typedEntity)
    {
        ArgumentNullException.ThrowIfNull(typedEntity);

        return typedEntity switch
        {
            ITypedEntity typed => typed.Entity,
            TypedEntityProxy { BackingSource: ISealedEntity entity } => entity,
            TypedEntityProxy => throw new EvitaInvalidUsageException(
                $"Instance of `{typedEntity.GetType()}` is a reference view, not an entity view - use " +
                $"GetReference(...) instead."),
            _ => throw new EvitaInvalidUsageException(
                $"Instance of `{typedEntity.GetType()}` is not a typed entity view.")
        };
    }

    /// <summary>
    /// Returns the reference behind a typed reference view.
    /// </summary>
    /// <param name="typedReference">an instance created by <see cref="CreateReferenceView{T}"/></param>
    /// <exception cref="EvitaInvalidUsageException">when the instance is not a reference view</exception>
    public static IReference GetReference(object typedReference)
    {
        ArgumentNullException.ThrowIfNull(typedReference);

        return typedReference is TypedEntityProxy { BackingSource: IReference reference }
            ? reference
            : throw new EvitaInvalidUsageException(
                $"Instance of `{typedReference.GetType()}` is not a typed reference view.");
    }
}

/// <summary>
/// Runtime proxy that answers contract members from a bound contract and a fetched entity.
/// <remarks>Public only because <see cref="DispatchProxy"/> requires it; not intended for direct use.</remarks>
/// </summary>
public class TypedEntityProxy : DispatchProxy
{
    private BoundTypedContract _contract = null!;
    private object _source = null!;
    private Type _contractType = null!;

    /// <summary>
    /// Builder collecting mutations, created on the first write. Until then the proxy is read-only and
    /// costs nothing extra.
    /// </summary>
    private IEntityBuilder? _builder;

    /// <summary>
    /// Nested typed views already handed out, so that reading the same member twice yields the same
    /// object. Created lazily - most contracts have no nested members at all.
    /// </summary>
    private Dictionary<MethodInfo, object?>? _nestedViews;

    /// <summary>Editors attached for a deep upsert.</summary>
    private List<TypedReferenceAttachment>? _attachments;

    /// <summary>Builder of the entity owning the reference, when this proxy views a reference.</summary>
    private IEntityBuilder? _referenceOwner;

    /// <summary>The entity or reference this proxy reads from.</summary>
    internal object BackingSource => _source;

    /// <summary>
    /// The builder collecting mutations, materialized on demand. Reads are redirected to it once it
    /// exists, so a written value is visible immediately.
    /// </summary>
    internal IEntityBuilder Builder => _builder ??= _source switch
    {
        IEntityBuilder builder => builder,
        Entity entity => new ExistingEntityBuilder(entity),
        _ => throw new EvitaInvalidUsageException(
            $"`{_contractType.Name}` cannot be edited: it is bound to " +
            $"{(_source is IReference ? "a reference" : $"`{_source.GetType().Name}`")}, which carries no " +
            $"entity to build upon.")
    };

    /// <summary>True when a mutating member has been invoked.</summary>
    internal bool HasChanges =>
        _builder is not null && TypedEntityView.Normalize(_builder.ToMutation()) is not null;

    internal void Initialize(BoundTypedContract contract, object source, Type contractType)
    {
        _contract = contract;
        _source = source;
        _contractType = contractType;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        if (targetMethod.DeclaringType == typeof(ITypedEntityEditor))
        {
            return targetMethod.Name switch
            {
                $"get_{nameof(ITypedEntityEditor.EntityBuilder)}" => Builder,
                $"get_{nameof(ITypedEntityEditor.HasChanges)}" => HasChanges,
                nameof(ITypedEntityEditor.ToMutation) => TypedEntityView.Normalize(Builder.ToMutation()),
                nameof(ITypedEntityEditor.ToInstance) => Builder.ToInstance(),
                nameof(ITypedEntityEditor.UpsertVia) => Builder.UpsertVia(
                    (EvitaClientSession)args![0]!),
                $"get_{nameof(ITypedEntityEditor.Attachments)}" =>
                    _attachments ?? (IReadOnlyList<TypedReferenceAttachment>)[],
                nameof(ITypedEntityEditor.AttachReferenced) => AttachReferenced(
                    (string)args![0]!, (ITypedEntityEditor)args[1]!),
                nameof(ITypedEntityEditor.UpsertDeeplyWith) => TypedDeepUpsert.Upsert(
                    (ITypedEntityEditor)(object)this,
                    (Func<IEntityBuilder, EntityReference>)args![0]!),
                nameof(ITypedEntityEditor.UpsertDeeplyVia) => TypedDeepUpsert.Upsert(
                    (ITypedEntityEditor)(object)this,
                    builder => builder.UpsertVia((EvitaClientSession)args![0]!)),
                _ => throw new EvitaInvalidUsageException(
                    $"Unsupported editor member `{targetMethod.Name}`.")
            };
        }

        if (_contract.IsMutating(targetMethod))
        {
            var mutating = _contract.FindMember(targetMethod);

            // a reference contract writes the reference's own attributes, through the owning entity
            if (_source is IReference viewedReference && mutating is not null
                                                      && _contract.IsReferenceAttributeWriter(mutating.MemberName))
            {
                _contract.WriteReferenceAttribute(
                    mutating.MemberName,
                    _referenceOwner ?? throw new EvitaInvalidUsageException(
                        $"`{_contractType.Name}` was read from an entity that is not open for writing, so " +
                        $"its reference attributes cannot be changed - open the owning entity for write " +
                        $"first."),
                    viewedReference.ReferenceName,
                    viewedReference.ReferencedPrimaryKey,
                    args
                );

                _nestedViews?.Clear();
                return mutating is { Fluent: true } ? this : null;
            }

            _contract.Write(targetMethod, Builder, args);

            // a nested view handed out earlier may now be stale, and the builder view supersedes the
            // sealed entity for every subsequent read
            _nestedViews?.Clear();

            return _contract.FindMember(targetMethod) is { Fluent: true } ? this : null;
        }

        // members of the opt-in mixin are answered here, never bound to entity data
        if (targetMethod.DeclaringType == typeof(ITypedEntity))
        {
            return targetMethod.Name switch
            {
                $"get_{nameof(ITypedEntity.Entity)}" => (_builder as ISealedEntity)
                    ?? _source as ISealedEntity
                    ?? throw new EvitaInvalidUsageException(
                        $"`{_contractType.Name}` is bound to a reference, so it exposes no entity."),
                $"get_{nameof(ITypedEntity.ContractType)}" => _contractType,
                _ => _contract.Read(targetMethod, _source, args)
            };
        }

        // once a builder exists it holds the authoritative view, including uncommitted writes
        var readSource = _builder ?? _source;

        // memoize nested views only over an immutable source - see TypedEntityView.Read
        if (!_contract.MemoizableAccessors.Contains(targetMethod) || readSource is IEntityBuilder)
        {
            return _contract.Read(targetMethod, readSource, args);
        }

        _nestedViews ??= new Dictionary<MethodInfo, object?>();
        if (_nestedViews.TryGetValue(targetMethod, out var cached))
        {
            return cached;
        }

        var value = _contract.Read(targetMethod, readSource, args);
        _nestedViews[targetMethod] = value;
        return value;
    }

    /// <summary>
    /// Remembers the entity builder that owns the reference this proxy views, enabling attribute writes.
    /// </summary>
    internal void AttachReferenceOwner(IEntityBuilder? owner) => _referenceOwner = owner;

    /// <summary>Records an attachment and returns null, so it can sit in the member switch above.</summary>
    private object? AttachReferenced(string referenceName, ITypedEntityEditor referenced)
    {
        (_attachments ??= []).Add(new TypedReferenceAttachment(referenceName, referenced));
        return null;
    }

    // object's own members are inherited by the generated proxy type rather than routed through
    // Invoke, so they are overridden here instead of being special-cased above

    /// <inheritdoc />
    public override string ToString() => _source switch
    {
        null => base.ToString()!,
        ISealedEntity entity => $"{_contractType.Name}({entity.Type}:{entity.PrimaryKey})",
        IReference reference =>
            $"{_contractType.Name}({reference.ReferenceName}:{reference.ReferencedPrimaryKey})",
        _ => base.ToString()!
    };

    /// <inheritdoc />
    public override int GetHashCode() =>
        _source is null ? base.GetHashCode() : HashCode.Combine(_contractType, _source);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj)
        || (obj is TypedEntityProxy other
            && other._contractType == _contractType
            && ReferenceEquals(other._source, _source));
}
