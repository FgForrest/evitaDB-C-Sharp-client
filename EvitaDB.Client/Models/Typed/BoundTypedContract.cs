using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// A <see cref="TypedContractDescriptor"/> resolved against a concrete <see cref="IEntitySchema"/>.
/// Binding is where schema-dependent facts enter: whether an attribute is localized, how many decimal
/// places it indexes, and what its default value is.
/// <remarks>
/// <para>
/// Each member is compiled once into a reader delegate and stored in a frozen dictionary keyed by the
/// accessor <see cref="MethodInfo"/> - the classify-once/execute-many split that keeps reads free of
/// per-call reflection. The key is the accessor method (<c>get_Name</c>), because that is what an
/// intercepted call hands over.
/// </para>
/// <para>
/// Instances are cached per contract type and entity type, mirroring the Java driver's recipe cache
/// key: the same contract can legitimately be bound to more than one entity collection.
/// </para>
/// </remarks>
/// </summary>
public sealed partial class BoundTypedContract
{
    private static readonly
        ConcurrentDictionary<(Type Contract, string EntityType, string? ReferenceName), BoundTypedContract>
        Cache = new();

    private readonly FrozenDictionary<MethodInfo, Func<object, object?[]?, object?>> _readers;

    private readonly FrozenDictionary<MethodInfo, Action<IEntityBuilder, object?[]?>> _writers;

    /// <summary>
    /// Every non-reading member by accessor, including those that cannot be written declaratively - so a
    /// call on one is answered with a write-specific diagnostic instead of "cannot be read".
    /// </summary>
    private readonly FrozenDictionary<MethodInfo, TypedMemberDescriptor> _mutatingMembers;

    private BoundTypedContract(
        TypedContractDescriptor descriptor,
        IEntitySchema? schema,
        IReferenceSchema? referenceSchema,
        FrozenDictionary<MethodInfo, Func<object, object?[]?, object?>> readers,
        FrozenDictionary<MethodInfo, Action<IEntityBuilder, object?[]?>> writers,
        FrozenDictionary<MethodInfo, TypedMemberDescriptor> mutatingMembers,
        IReadOnlySet<MethodInfo> memoizableAccessors,
        FrozenDictionary<string, Func<object, object?[]?, object?>> readersByName,
        FrozenDictionary<string, Action<IEntityBuilder, object?[]?>> writersByName,
        FrozenSet<string> memoizableNames,
        FrozenDictionary<string, Action<IReferenceBuilder, object?[]?>> referenceWriters)
    {
        _referenceWriters = referenceWriters;
        _mutatingMembers = mutatingMembers;
        _readersByName = readersByName;
        _writersByName = writersByName;
        _memoizableNames = memoizableNames;
        Descriptor = descriptor;
        Schema = schema;
        ReferenceSchema = referenceSchema;
        _readers = readers;
        _writers = writers;
        MemoizableAccessors = memoizableAccessors;
    }

    /// <summary>The descriptor this binding was built from.</summary>
    public TypedContractDescriptor Descriptor { get; }

    /// <summary>
    /// The entity schema the contract was bound against, or <c>null</c> for a reference contract bound
    /// without an owning entity schema.
    /// </summary>
    public IEntitySchema? Schema { get; }

    /// <summary>The reference schema this contract was bound against, for reference contracts.</summary>
    public IReferenceSchema? ReferenceSchema { get; }

    /// <summary>
    /// Readers keyed by contract member name, for generated implementations, which know the member they
    /// are forwarding but hold no <see cref="MethodInfo"/>.
    /// </summary>
    private readonly FrozenDictionary<string, Func<object, object?[]?, object?>> _readersByName;

    /// <summary>Writers keyed by contract member name, for generated implementations.</summary>
    private readonly FrozenDictionary<string, Action<IEntityBuilder, object?[]?>> _writersByName;

    /// <summary>Member names whose read result may be memoized per instance.</summary>
    private readonly FrozenSet<string> _memoizableNames;

    /// <summary>
    /// Writers for the attributes of a reference contract, keyed by member name. They mutate an
    /// <see cref="IReferenceBuilder"/>, which is what
    /// <c>IEntityBuilder.SetReference(name, key, whichIs)</c> hands out - so a reference attribute is
    /// written through the owning entity's builder without the view needing a builder of its own.
    /// </summary>
    private readonly FrozenDictionary<string, Action<IReferenceBuilder, object?[]?>> _referenceWriters;

    /// <summary>
    /// Accessors whose result may be memoized per instance. Members returning nested typed views are
    /// memoized so that reading the same member twice yields the same object - the identity the Java
    /// driver keeps in its per-proxy instance cache, and what makes object-graph walking possible.
    /// </summary>
    public IReadOnlySet<MethodInfo> MemoizableAccessors { get; }

    /// <summary>
    /// Binds a contract to a schema, reusing a cached binding when one exists.
    /// </summary>
    /// <param name="contractType">the annotated contract type</param>
    /// <param name="schema">the entity schema to resolve members against</param>
    public static BoundTypedContract Bind(Type contractType, IEntitySchema schema)
    {
        var descriptor = TypedContractAnalyzer.Describe(contractType);
        return Cache.GetOrAdd(
            (contractType, schema.Name, null),
            _ => BuildBinding(descriptor, schema, null)
        );
    }

    /// <summary>
    /// Binds a reference contract against the schema of the reference it reads, reusing a cached binding
    /// when one exists.
    /// </summary>
    /// <param name="contractType">the reference contract type</param>
    /// <param name="referenceSchema">
    /// schema of the reference, or <c>null</c> when the reference is not described by a schema - members
    /// then resolve leniently
    /// </param>
    public static BoundTypedContract BindReference(Type contractType, IReferenceSchema? referenceSchema)
    {
        var descriptor = TypedContractAnalyzer.DescribeReferenceContract(contractType);
        return Cache.GetOrAdd(
            (contractType, referenceSchema?.ReferencedEntityType ?? string.Empty, referenceSchema?.Name ?? string.Empty),
            _ => BuildBinding(descriptor, null, referenceSchema)
        );
    }

    /// <summary>
    /// Reads a member of the given entity through this binding.
    /// </summary>
    /// <param name="accessor">the accessor method that was invoked</param>
    /// <param name="source">the fetched entity or reference to read from</param>
    /// <param name="arguments">invocation arguments, if any</param>
    /// <exception cref="EvitaInvalidUsageException">when the member cannot be bound</exception>
    public object? Read(MethodInfo accessor, object source, object?[]? arguments)
    {
        if (_readers.TryGetValue(accessor, out var reader))
        {
            return reader(source, arguments);
        }

        var member = Descriptor.Members.FirstOrDefault(it => it.Accessor == accessor);
        var reason = member?.UnboundReason ?? "the member is not part of the analyzed contract";
        throw new EvitaInvalidUsageException(
            $"Member `{Descriptor.ContractType.Name}.{member?.MemberName ?? accessor.Name}` cannot be " +
            $"read: {reason}."
        );
    }

    /// <summary>
    /// Tells whether the given accessor writes or removes data, in which case it needs a builder.
    /// </summary>
    /// <param name="accessor">the accessor method that was invoked</param>
    public bool IsMutating(MethodInfo accessor) =>
        _writers.ContainsKey(accessor) || _mutatingMembers.ContainsKey(accessor);

    /// <summary>
    /// Returns the descriptor of the member an accessor belongs to, or <c>null</c> when the accessor is
    /// not part of this contract.
    /// </summary>
    /// <param name="accessor">the accessor method that was invoked</param>
    public TypedMemberDescriptor? FindMember(MethodInfo accessor) =>
        _mutatingMembers.GetValueOrDefault(accessor)
        ?? Descriptor.Members.FirstOrDefault(it => it.Accessor == accessor);

    /// <summary>
    /// Applies a writing or removing member to the given builder.
    /// </summary>
    /// <param name="accessor">the accessor method that was invoked</param>
    /// <param name="builder">builder collecting the mutations</param>
    /// <param name="arguments">invocation arguments</param>
    /// <exception cref="EvitaInvalidUsageException">when the member cannot be written</exception>
    public void Write(MethodInfo accessor, IEntityBuilder builder, object?[]? arguments)
    {
        if (_writers.TryGetValue(accessor, out var writer))
        {
            writer(builder, arguments);
            return;
        }

        var member = FindMember(accessor);
        throw new EvitaInvalidUsageException(
            $"Member `{Descriptor.ContractType.Name}.{member?.MemberName ?? accessor.Name}` cannot be " +
            $"written: {member?.UnboundReason ?? "prices and references are written through " +
                "ITypedEntityEditor.EntityBuilder, not through generated setters"}."
        );
    }

    /// <summary>
    /// Reads a member by its contract member name. Used by generated implementations.
    /// </summary>
    /// <param name="memberName">name of the property or method on the contract</param>
    /// <param name="source">the entity, builder or reference to read from</param>
    /// <param name="arguments">invocation arguments, if any</param>
    public object? ReadByMember(string memberName, object source, object?[]? arguments)
    {
        if (_readersByName.TryGetValue(memberName, out var reader))
        {
            return reader(source, arguments);
        }

        var member = Descriptor.Members.FirstOrDefault(
            it => it.MemberName == memberName && it.Access == TypedMemberAccess.Read
        );
        throw new EvitaInvalidUsageException(
            $"Member `{Descriptor.ContractType.Name}.{memberName}` cannot be read: " +
            $"{member?.UnboundReason ?? "the member is not part of the analyzed contract"}."
        );
    }

    /// <summary>
    /// Writes a member by its contract member name. Used by generated implementations.
    /// </summary>
    /// <param name="memberName">name of the property or method on the contract</param>
    /// <param name="builder">builder collecting the mutations</param>
    /// <param name="arguments">invocation arguments</param>
    public void WriteByMember(string memberName, IEntityBuilder builder, object?[]? arguments)
    {
        if (_writersByName.TryGetValue(memberName, out var writer))
        {
            writer(builder, arguments);
            return;
        }

        var member = Descriptor.Members.FirstOrDefault(
            it => it.MemberName == memberName && it.Access != TypedMemberAccess.Read
        );
        throw new EvitaInvalidUsageException(
            $"Member `{Descriptor.ContractType.Name}.{memberName}` cannot be written: " +
            $"{member?.UnboundReason ?? "the member is not a writable part of the contract"}."
        );
    }

    /// <summary>
    /// Writes an attribute of one reference through the owning entity's builder.
    /// </summary>
    /// <param name="memberName">member of the reference contract being written</param>
    /// <param name="owner">builder of the entity that owns the reference</param>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedPrimaryKey">key of the referenced entity</param>
    /// <param name="arguments">invocation arguments</param>
    public void WriteReferenceAttribute(
        string memberName,
        IEntityBuilder owner,
        string referenceName,
        int referencedPrimaryKey,
        object?[]? arguments)
    {
        if (!_referenceWriters.TryGetValue(memberName, out var writer))
        {
            var member = Descriptor.Members.FirstOrDefault(
                it => it.MemberName == memberName && it.Access != TypedMemberAccess.Read
            );
            throw new EvitaInvalidUsageException(
                $"Member `{Descriptor.ContractType.Name}.{memberName}` cannot be written: " +
                $"{member?.UnboundReason ?? "only attributes of a reference contract are writable"}."
            );
        }

        // ExistingEntityBuilder.SetReference updates in place (it routes an existing reference through
        // ExistingReferenceBuilder), so one call both guarantees the reference exists and carries the
        // change. InitialEntityBuilder instead does a dictionary Add and throws on a repeat - Java has a
        // separate updateReference for that case, which this driver never ported. On a brand-new entity the
        // reference exists only client-side, so rebuilding it is safe as long as its attributes come along.
        if (owner is InitialEntityBuilder && owner.ReferencesAvailable())
        {
            var existing = owner.GetReferences(referenceName)
                .FirstOrDefault(it => it.ReferencedPrimaryKey == referencedPrimaryKey);

            if (existing is not null)
            {
                owner.RemoveReference(referenceName, referencedPrimaryKey);
                owner.SetReference(referenceName, referencedPrimaryKey, reference =>
                {
                    foreach (var carried in existing.GetAttributeValues())
                    {
                        if (carried.Key.Locale is null)
                        {
                            reference.SetAttribute(carried.Key.AttributeName, carried.Value);
                        }
                        else
                        {
                            reference.SetAttribute(
                                carried.Key.AttributeName, carried.Key.Locale, carried.Value);
                        }
                    }

                    writer(reference, arguments);
                });

                return;
            }
        }

        owner.SetReference(referenceName, referencedPrimaryKey, reference => writer(reference, arguments));
    }

    /// <summary>Tells whether the named member writes an attribute of a reference contract.</summary>
    /// <param name="memberName">member of the reference contract</param>
    public bool IsReferenceAttributeWriter(string memberName) => _referenceWriters.ContainsKey(memberName);

    /// <summary>
    /// Tells whether reads of the named member should be memoized, because it materializes nested views
    /// whose identity must stay stable.
    /// </summary>
    /// <param name="memberName">name of the property or method on the contract</param>
    public bool IsMemoizableMember(string memberName) => _memoizableNames.Contains(memberName);

    private static BoundTypedContract BuildBinding(
        TypedContractDescriptor descriptor,
        IEntitySchema? schema,
        IReferenceSchema? referenceSchema)
    {
        var readers = new Dictionary<MethodInfo, Func<object, object?[]?, object?>>();
        var writers = new Dictionary<MethodInfo, Action<IEntityBuilder, object?[]?>>();
        var mutatingMembers = new Dictionary<MethodInfo, TypedMemberDescriptor>();
        var memoizable = new HashSet<MethodInfo>();
        var readersByName = new Dictionary<string, Func<object, object?[]?, object?>>();
        var referenceWriters = new Dictionary<string, Action<IReferenceBuilder, object?[]?>>();
        var writersByName = new Dictionary<string, Action<IEntityBuilder, object?[]?>>();
        var memoizableNames = new HashSet<string>();

        foreach (var member in descriptor.Members)
        {
            if (member.Access != TypedMemberAccess.Read)
            {
                mutatingMembers[member.Accessor] = member;

                // inside a reference contract a writable member targets the reference's own attributes
                if (descriptor.Scope == TypedContractScope.Reference)
                {
                    var referenceWriter = BuildReferenceAttributeWriter(descriptor, member, referenceSchema);
                    if (referenceWriter is not null)
                    {
                        referenceWriters[member.MemberName] = referenceWriter;
                    }

                    continue;
                }

                var writer = BuildWriter(descriptor, member, schema);
                if (writer is not null)
                {
                    writers[member.Accessor] = writer;
                    writersByName[member.MemberName] = writer;
                }

                continue;
            }

            var reader = member.Kind switch
            {
                TypedMemberKind.PrimaryKey => BuildPrimaryKeyReader(descriptor, member),
                TypedMemberKind.EntityType => BuildEntityTypeReader(descriptor, member),
                TypedMemberKind.Attribute => BuildAttributeReader(descriptor, member, schema, referenceSchema),
                TypedMemberKind.AssociatedData => BuildAssociatedDataReader(descriptor, member, schema),
                TypedMemberKind.Price => BuildPriceReader(descriptor, member),
                TypedMemberKind.PriceForSale => BuildPriceForSaleReader(descriptor, member),
                TypedMemberKind.AccompanyingPrice => BuildAccompanyingPriceReader(descriptor, member),
                TypedMemberKind.PriceRangeForSale => BuildPriceRangeForSaleReader(descriptor, member),
                TypedMemberKind.Reference => BuildReferenceReader(descriptor, member, schema!),
                TypedMemberKind.ParentEntity => BuildParentReader(descriptor, member),
                TypedMemberKind.ReferencedEntity => BuildReferencedEntityReader(descriptor, member),
                TypedMemberKind.ReferencedEntityGroup => BuildReferenceGroupReader(descriptor, member),
                _ => null
            };

            if (reader is null)
            {
                continue;
            }

            readers[member.Accessor] = reader;
            readersByName[member.MemberName] = reader;

            // Memoization is keyed by member, not by arguments, so only a member that takes none can be
            // cached - otherwise a filtered reference member would hand back the previous call's result.
            if (member.Accessor.GetParameters().Length == 0 && ReturnsNestedView(member))
            {
                memoizable.Add(member.Accessor);
                memoizableNames.Add(member.MemberName);
            }
        }

        return new BoundTypedContract(
            descriptor, schema, referenceSchema, readers.ToFrozenDictionary(),
            writers.ToFrozenDictionary(), mutatingMembers.ToFrozenDictionary(), memoizable,
            readersByName.ToFrozenDictionary(), writersByName.ToFrozenDictionary(),
            memoizableNames.ToFrozenSet(), referenceWriters.ToFrozenDictionary()
        );
    }

    /// <summary>
    /// Tells whether a member materialises nested typed views, whose object identity should be stable
    /// across repeated reads.
    /// </summary>
    private static bool ReturnsNestedView(TypedMemberDescriptor member) =>
        member.Kind is TypedMemberKind.Reference or TypedMemberKind.ParentEntity
            or TypedMemberKind.ReferencedEntity or TypedMemberKind.ReferencedEntityGroup
        && member.ReferenceProjection is ReferenceProjection.ReferencedEntity
            or ReferenceProjection.ReferenceContract;

    private static Func<object, object?[]?, object?> BuildPrimaryKeyReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member) =>
        (source, _) => Wrap(
            member,
            EvitaDataTypes.ToTargetType(AsEntity(descriptor, member, source).PrimaryKey, member.ItemType),
            fetched: true,
            member.ItemType
        );

    private static Func<object, object?[]?, object?> BuildEntityTypeReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member) =>
        (source, _) => Wrap(
            member, AsEntity(descriptor, member, source).Type, fetched: true, member.ItemType
        );

    private static Func<object, object?[]?, object?>? BuildAttributeReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema,
        IReferenceSchema? referenceSchema)
    {
        var attributeName = member.DataName!;
        var attributeSchema = referenceSchema is not null
            ? ResolveAttributeSchema(referenceSchema, attributeName)
            : schema is null
                ? null
                : ResolveAttributeSchema(schema, attributeName);

        if (attributeSchema is null)
        {
            if (member.NameDeclaredExplicitly)
            {
                // an annotated member names data that must exist - fail loudly at bind time
                throw new EvitaInvalidUsageException(
                    $"Attribute `{attributeName}` referenced by " +
                    $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of " +
                    $"{(referenceSchema is not null
                        ? $"reference `{referenceSchema.Name}`"
                        : $"entity `{schema!.Name}`")}."
                );
            }

            // an inferred member declines quietly, exactly like the Java driver's soft branch; the
            // failure surfaces only if the member is actually read
            return null;
        }

        var isArray = member.CollectionShape != TypedCollectionShape.Scalar;
        var decimalPlaces = attributeSchema.IndexedDecimalPlaces;
        var defaultValue = attributeSchema.DefaultValue;
        var resolvedName = attributeSchema.Name;

        // a localized attribute read without an explicit locale resolves against the locale the entity
        // was fetched with, which the driver handles inside GetAttribute - so both paths differ only in
        // whether a locale is passed through
        object? Read(object source, CultureInfo? locale)
        {
            // IAttributes<out TS> is covariant and both IEntity and IReference implement it over
            // IAttributeSchema, so one reader serves entity and reference scopes alike
            var holder = (IAttributes<IAttributeSchema>)source;
            var fetched = locale is null
                ? holder.AttributesAvailable()
                : holder.AttributesAvailable(locale);

            if (!fetched)
            {
                // a schema default must not stand in for data that was never fetched - that would hide
                // the difference between "no value" and "not requested"
                return Convert(member, null, fetched: false, decimalPlaces);
            }

            var raw = locale is null
                ? isArray ? holder.GetAttributeArray(resolvedName) : holder.GetAttribute(resolvedName)
                : isArray
                    ? holder.GetAttributeArray(resolvedName, locale)
                    : holder.GetAttribute(resolvedName, locale);

            return Convert(member, raw ?? defaultValue, fetched: true, decimalPlaces);
        }

        if (member.HasLocaleParameter)
        {
            return (source, args) =>
            {
                var locale = args?.Length > 0 ? args[0] as CultureInfo : null;
                if (locale is null)
                {
                    throw new EvitaInvalidUsageException(
                        $"Member `{descriptor.ContractType.Name}.{member.MemberName}` requires a " +
                        $"non-null CultureInfo argument."
                    );
                }

                return Read(source, locale);
            };
        }

        return (source, _) => Read(source, null);
    }

    /// <summary>
    /// Resolves an attribute schema by exact name, falling back to evitaDB's camel-case name variant
    /// index so that a CLR member named <c>Name</c> also matches an attribute stored as <c>name</c>.
    /// </summary>
    private static IAttributeSchema? ResolveAttributeSchema<T>(
        IAttributeSchemaProvider<T> provider,
        string attributeName) where T : IAttributeSchema =>
        provider.GetAttribute(attributeName)
        ?? provider.GetAttributeByName(attributeName, NamingConvention.CamelCase);

    /// <summary>
    /// Converts one stored value to the member's item type.
    /// <remarks>
    /// evitaDB has no enum data type, so an enum-typed member is stored as its name - the same mapping the
    /// Java driver applies. Conversion is case-insensitive on the way in, because a value may have been
    /// written by another client.
    /// </remarks>
    /// </summary>
    internal static object? ConvertValue(object? value, Type itemType, int decimalPlaces)
    {
        if (value is null)
        {
            return null;
        }

        if (!itemType.IsEnum)
        {
            return EvitaDataTypes.ToTargetType(value, itemType, decimalPlaces);
        }

        if (itemType.IsInstanceOfType(value))
        {
            return value;
        }

        var name = value as string ?? value.ToString();

        return Enum.TryParse(itemType, name, ignoreCase: true, out var parsed)
            ? parsed
            : throw new EvitaInvalidUsageException(
                $"Value `{name}` cannot be read as `{itemType.Name}` - the enum has no such member. " +
                $"evitaDB stores enums by name, so the stored value must match one."
            );
    }

    private static object? Convert(
        TypedMemberDescriptor member,
        object? rawValue,
        bool fetched,
        int decimalPlaces)
    {
        if (rawValue is null)
        {
            return Wrap(member, null, fetched, member.ItemType);
        }

        object? converted;

        if (member.CollectionShape == TypedCollectionShape.Scalar)
        {
            converted = ConvertValue(rawValue, member.ItemType, decimalPlaces);
        }
        else
        {
            var source = rawValue as object[] ?? [rawValue];
            var typedArray = Array.CreateInstance(member.ItemType, source.Length);
            for (var i = 0; i < source.Length; i++)
            {
                typedArray.SetValue(ConvertValue(source[i], member.ItemType, decimalPlaces), i);
            }

            converted = member.CollectionShape switch
            {
                TypedCollectionShape.Array or TypedCollectionShape.ReadOnlyList => typedArray,
                TypedCollectionShape.List => Activator.CreateInstance(
                    typeof(List<>).MakeGenericType(member.ItemType), typedArray),
                TypedCollectionShape.Set => Activator.CreateInstance(
                    typeof(HashSet<>).MakeGenericType(member.ItemType), typedArray),
                _ => typedArray
            };
        }

        return Wrap(member, converted, fetched, member.ItemType);
    }

    /// <summary>
    /// Materialises already-projected values into the collection shape the member declares.
    /// </summary>
    private static object MaterializeCollection(TypedMemberDescriptor member, IReadOnlyList<object?> values)
    {
        var typedArray = Array.CreateInstance(member.ItemType, values.Count);
        for (var i = 0; i < values.Count; i++)
        {
            typedArray.SetValue(values[i], i);
        }

        return member.CollectionShape switch
        {
            TypedCollectionShape.List => Activator.CreateInstance(
                typeof(List<>).MakeGenericType(member.ItemType), typedArray)!,
            TypedCollectionShape.Set => Activator.CreateInstance(
                typeof(HashSet<>).MakeGenericType(member.ItemType), typedArray)!,
            _ => typedArray
        };
    }

    /// <summary>
    /// Applies the member's absence policy: <see cref="Maybe{T}"/> members carry the three states
    /// explicitly, plain nullable members collapse both absence kinds to <c>null</c>, and members
    /// whose type cannot express absence at all raise <see cref="ContextMissingException"/> rather
    /// than silently returning a zero value for data that was never fetched.
    /// </summary>
    private static object? Wrap(TypedMemberDescriptor member, object? value, bool fetched, Type itemType)
    {
        if (member.WrapInMaybe)
        {
            var payloadType = MaybePayloadType(member, itemType);
            var maybeType = typeof(Maybe<>).MakeGenericType(payloadType);

            if (!fetched)
            {
                return StaticProperty(maybeType, nameof(Maybe<object>.NotFetched));
            }

            if (value is null)
            {
                // `Of(null)` must not be reached reflectively for a value-type payload: MethodBase.Invoke
                // substitutes default(T) for a null argument, so Maybe<decimal>.Of(null) would report
                // Present(0) instead of Absent
                return StaticProperty(maybeType, nameof(Maybe<object>.Absent));
            }

            return maybeType.GetMethod(nameof(Maybe<object>.Of),
                BindingFlags.Public | BindingFlags.Static)!.Invoke(null, [value]);
        }

        if (value is not null)
        {
            return value;
        }

        var declaredType = member.DeclaredType;
        var nonNullableValueType = declaredType.IsValueType && Nullable.GetUnderlyingType(declaredType) is null;

        if (nonNullableValueType && !fetched)
        {
            throw new ContextMissingException(
                $"Data backing member `{member.MemberName}` was not fetched and the member type " +
                $"`{declaredType.Name}` cannot express absence - fetch the data, or declare the member " +
                $"as `{declaredType.Name}?` or `Maybe<{declaredType.Name}>`."
            );
        }

        return nonNullableValueType ? Activator.CreateInstance(declaredType) : null;
    }

    private static object? StaticProperty(Type maybeType, string propertyName) =>
        maybeType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)!.GetValue(null);

    /// <summary>
    /// Determines the type argument of the <see cref="Maybe{T}"/> a member returns, which is the
    /// declared inner type rather than the unwrapped item type for collection-shaped members.
    /// </summary>
    private static Type MaybePayloadType(TypedMemberDescriptor member, Type itemType) =>
        member.DeclaredType.IsGenericType &&
        member.DeclaredType.GetGenericTypeDefinition() == typeof(Maybe<>)
            ? member.DeclaredType.GetGenericArguments()[0]
            : itemType;
}
