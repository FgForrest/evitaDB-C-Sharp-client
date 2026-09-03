using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Turns an annotated contract type into a <see cref="TypedContractDescriptor"/>. Results are cached
/// per contract type, so the reflection cost is paid once per process.
/// <remarks>
/// This is the compile-time-derivable half of the typed-model layer: member names, shapes and
/// absence policies. Nothing here consults an entity schema - that happens in
/// <see cref="BoundTypedContract"/>, which needs a live schema for localization, decimal places and
/// defaults.
/// </remarks>
/// </summary>
public static partial class TypedContractAnalyzer
{
    private static readonly ConcurrentDictionary<Type, TypedContractDescriptor> Descriptors = new();

    private static readonly ConcurrentDictionary<Type, TypedContractDescriptor> ReferenceDescriptors = new();

    /// <summary>
    /// Member names accepted as the primary key when no annotation is present. Mirrors the Java
    /// driver's <c>PrimaryKeyRef.POSSIBLE_ARGUMENT_NAMES</c>.
    /// </summary>
    private static readonly HashSet<string> PrimaryKeyNames =
        new(StringComparer.OrdinalIgnoreCase) { "id", "primaryKey", "pk", "entityPrimaryKey" };

    /// <summary>
    /// Member names accepted inside a reference contract as the referenced entity primary key, mirroring
    /// the Java driver's <c>ReferencedEntity.POSSIBLE_ARGUMENT_NAMES</c>.
    /// </summary>
    private static readonly HashSet<string> ReferencedEntityKeyNames =
        new(StringComparer.OrdinalIgnoreCase)
            { "referencedEntityPrimaryKey", "referencedPrimaryKey", "entityPrimaryKey", "primaryKey", "id" };

    /// <summary>
    /// Member names accepted inside a reference contract as the reference group primary key, mirroring
    /// the Java driver's <c>ReferencedEntityGroup.POSSIBLE_ARGUMENT_NAMES</c>.
    /// </summary>
    private static readonly HashSet<string> ReferencedGroupKeyNames =
        new(StringComparer.OrdinalIgnoreCase)
            { "groupPrimaryKey", "entityGroupPrimaryKey", "groupPk", "groupId" };

    private static readonly HashSet<Type> IntegerTypes =
    [
        typeof(int), typeof(int?), typeof(long), typeof(long?),
        typeof(short), typeof(short?), typeof(byte), typeof(byte?)
    ];

    /// <summary>
    /// Describes the given contract type, or returns the cached description.
    /// </summary>
    /// <typeparam name="T">the contract type</typeparam>
    /// <exception cref="EvitaInvalidUsageException">when the type carries no entity annotation</exception>
    public static TypedContractDescriptor Describe<T>() => Describe(typeof(T));

    /// <summary>
    /// Describes the given contract type, or returns the cached description.
    /// </summary>
    /// <param name="contractType">the contract type</param>
    /// <exception cref="EvitaInvalidUsageException">when the type carries no entity annotation</exception>
    public static TypedContractDescriptor Describe(Type contractType) =>
        Descriptors.GetOrAdd(contractType, BuildDescriptor);

    /// <summary>
    /// Describes a reference contract - a type used as the item type of a reference member, whose members
    /// are interpreted against a single <see cref="IReference"/> rather than an entity.
    /// <remarks>
    /// Reference contracts need no type-level annotation, mirroring the Java driver, where the
    /// reference-mapping type is simply whatever type the reference member returns.
    /// </remarks>
    /// </summary>
    /// <param name="contractType">the reference contract type</param>
    public static TypedContractDescriptor DescribeReferenceContract(Type contractType) =>
        ReferenceDescriptors.GetOrAdd(contractType, BuildReferenceDescriptor);

    /// <summary>
    /// Tells whether a type is an annotated entity contract, as opposed to a reference contract or a
    /// plain value type.
    /// </summary>
    /// <param name="type">the type to test</param>
    public static bool IsEntityContract(Type type) => ExtractEntityType(type) is not null;

    /// <summary>
    /// Resolves the evitaDB entity type name a contract maps to, without describing its members.
    /// </summary>
    /// <param name="contractType">the contract type</param>
    /// <returns>the entity type name, or <c>null</c> when the type carries no entity annotation</returns>
    public static string? ExtractEntityType(Type contractType)
    {
        var entity = contractType.GetCustomAttribute<EvitaEntityAttribute>(inherit: true);
        if (entity is not null)
        {
            return string.IsNullOrWhiteSpace(entity.Name) ? DeriveEntityType(contractType) : entity.Name;
        }

        var entityRef = contractType.GetCustomAttribute<EvitaEntityRefAttribute>(inherit: true);
        if (entityRef is not null)
        {
            return string.IsNullOrWhiteSpace(entityRef.Name)
                ? DeriveEntityType(contractType)
                : entityRef.Name;
        }

        return null;
    }

    /// <summary>
    /// Converts a CLR member name to the camel-case convention evitaDB uses for data names
    /// (<c>Name</c> becomes <c>name</c>, <c>ShortDescription</c> becomes <c>shortDescription</c>).
    /// </summary>
    /// <param name="memberName">the CLR member name</param>
    public static string ToCamelCase(string memberName)
    {
        if (string.IsNullOrEmpty(memberName) || char.IsLower(memberName[0]))
        {
            return memberName;
        }

        return char.ToLowerInvariant(memberName[0]) + memberName[1..];
    }

    /// <summary>
    /// Derives the evitaDB data name a member refers to. Accessor-style prefixes on methods are
    /// stripped first, so <c>GetTitle(CultureInfo)</c> refers to <c>title</c> rather than
    /// <c>getTitle</c> - the counterpart of the Java driver's
    /// <c>getPropertyNameFromMethodNameIfPossible</c>.
    /// </summary>
    /// <param name="member">the contract member</param>
    public static string DeriveDataName(MemberInfo member)
    {
        var name = member.Name;

        if (member is MethodInfo)
        {
            foreach (var prefix in AccessorPrefixes)
            {
                if (name.Length > prefix.Length
                    && name.StartsWith(prefix, StringComparison.Ordinal)
                    && char.IsUpper(name[prefix.Length]))
                {
                    name = name[prefix.Length..];
                    break;
                }
            }
        }

        return ToCamelCase(name);
    }

    /// <summary>
    /// Method-name prefixes stripped when deriving a data name. Ordered so that longer prefixes are
    /// tested before shorter ones that would also match.
    /// </summary>
    private static readonly string[] AccessorPrefixes = ["Get", "Set", "Is"];

    private static TypedContractDescriptor BuildDescriptor(Type contractType)
    {
        var entityDeclaration = contractType.GetCustomAttribute<EvitaEntityAttribute>(inherit: true);
        var entityType = ExtractEntityType(contractType)
                         ?? throw new EvitaInvalidUsageException(
                             $"Type `{contractType}` is not a typed entity contract - annotate it with " +
                             $"[EvitaEntity] or [EvitaEntityRef].");

        var members = new List<TypedMemberDescriptor>();
        foreach (var member in CollectMembers(contractType))
        {
            members.Add(DescribeMember(member));
        }

        members.AddRange(DescribeWriteMembers(contractType));

        return new TypedContractDescriptor(
            contractType,
            entityType,
            entityDeclaration is not null,
            entityDeclaration,
            members
        );
    }

    private static TypedContractDescriptor BuildReferenceDescriptor(Type contractType)
    {
        List<TypedMemberDescriptor> members = [];
        foreach (var member in CollectMembers(contractType))
        {
            members.Add(DescribeReferenceMember(member));
        }

        // a writable member of a reference contract targets the reference's own attributes
        members.AddRange(DescribeWriteMembers(contractType));

        return new TypedContractDescriptor(
            contractType,
            string.Empty,
            drivesSchema: false,
            entityDeclaration: null,
            members,
            TypedContractScope.Reference
        );
    }

    /// <summary>
    /// Describes a member of a reference contract. Only reference-scoped kinds are recognised here:
    /// the reference's own attributes, the referenced entity and the reference group.
    /// </summary>
    private static TypedMemberDescriptor DescribeReferenceMember(MemberInfo member)
    {
        var (accessor, returnType, parameters) = ReadMemberSignature(member);
        var shape = UnwrapShape(returnType);
        var hasLocaleParameter = parameters.Length == 1 && parameters[0].ParameterType == typeof(CultureInfo);

        if (UnbindableReason(member, accessor, parameters, hasLocaleParameter) is { } reason)
        {
            return Unbound(accessor, member.Name, returnType, shape, hasLocaleParameter, reason);
        }

        if (member.GetCustomAttribute<ReferencedEntityAttribute>() is not null)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.ReferencedEntity, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ResolveReferenceProjection(shape.ItemType));
        }

        if (member.GetCustomAttribute<ReferencedEntityGroupAttribute>() is not null)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.ReferencedEntityGroup,
                null, true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, false, null, null, ResolveReferenceProjection(shape.ItemType));
        }

        // an unannotated integer member named after the referenced key is accepted, mirroring the Java
        // driver's ReferencedEntity/Group possible-argument-name fallbacks
        if (!hasLocaleParameter && shape.Collection == TypedCollectionShape.Scalar
                                && IntegerTypes.Contains(shape.ItemType))
        {
            if (ReferencedEntityKeyNames.Contains(member.Name))
            {
                return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.ReferencedEntity,
                    null, false, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                    hasLocaleParameter, null, false, null, null, ReferenceProjection.PrimaryKey);
            }

            if (ReferencedGroupKeyNames.Contains(member.Name))
            {
                return new TypedMemberDescriptor(accessor, member.Name,
                    TypedMemberKind.ReferencedEntityGroup, null, false, returnType, shape.ItemType,
                    shape.WrapInMaybe, shape.Collection, hasLocaleParameter, null, false, null, null,
                    ReferenceProjection.PrimaryKey);
            }
        }

        // everything else is an attribute of the reference itself
        if (member.GetCustomAttribute<EvitaAttributeAttribute>() is { } attribute)
        {
            var name = string.IsNullOrWhiteSpace(attribute.Name) ? DeriveDataName(member) : attribute.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute, name, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                attribute, false, null);
        }

        if (member.GetCustomAttribute<AttributeRefAttribute>() is { } attributeRef)
        {
            var name = string.IsNullOrWhiteSpace(attributeRef.Name)
                ? DeriveDataName(member)
                : attributeRef.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute, name, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null);
        }

        return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute,
            DeriveDataName(member), false, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
            hasLocaleParameter, null, false, null);
    }

    /// <summary>
    /// Decides how a reference-shaped member materialises, from its declared item type.
    /// </summary>
    private static ReferenceProjection ResolveReferenceProjection(Type itemType)
    {
        if (IntegerTypes.Contains(itemType))
        {
            return ReferenceProjection.PrimaryKey;
        }

        if (typeof(IEntityReference).IsAssignableFrom(itemType))
        {
            return ReferenceProjection.EntityReference;
        }

        return IsEntityContract(itemType)
            ? ReferenceProjection.ReferencedEntity
            : ReferenceProjection.ReferenceContract;
    }

    /// <summary>
    /// Collects candidate members. For interfaces the whole implemented-interface graph is walked,
    /// because C# reflection does not report inherited interface members on the derived interface.
    /// </summary>
    private static IEnumerable<MemberInfo> CollectMembers(Type contractType)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var types = contractType.IsInterface
            ? new[] { contractType }.Concat(contractType.GetInterfaces())
            : [contractType];

        foreach (var type in types)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var property in type.GetProperties(flags))
            {
                if (property.GetMethod is not null && seen.Add(property.Name))
                {
                    yield return property;
                }
            }

            foreach (var method in type.GetMethods(flags))
            {
                // property accessors are handled above; object's own members are never bound
                if (method.IsSpecialName || method.DeclaringType == typeof(object))
                {
                    continue;
                }

                // a method that takes a value, or removes data, is a write member and is described by
                // the write analysis instead - describing it here too would shadow it with a reader
                if (IsWriteShaped(method))
                {
                    continue;
                }

                if (seen.Add(method.Name))
                {
                    yield return method;
                }
            }
        }
    }

    private static TypedMemberDescriptor DescribeMember(MemberInfo member)
    {
        var (accessor, returnType, parameters) = ReadMemberSignature(member);
        var shape = UnwrapShape(returnType);
        var hasLocaleParameter = parameters.Length == 1 && parameters[0].ParameterType == typeof(CultureInfo);

        if (UnbindableReason(member, accessor, parameters, hasLocaleParameter) is { } reason)
        {
            return Unbound(accessor, member.Name, returnType, shape, hasLocaleParameter, reason);
        }

        // explicit annotations, in resolution order
        if (member.GetCustomAttribute<PrimaryKeyAttribute>() is { } primaryKey)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.PrimaryKey, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, primaryKey.AutoGenerate, null);
        }

        if (member.GetCustomAttribute<EntityTypeAttribute>() is not null)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.EntityType, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, false, null);
        }

        if (member.GetCustomAttribute<EvitaAttributeAttribute>() is { } attribute)
        {
            var name = string.IsNullOrWhiteSpace(attribute.Name) ? DeriveDataName(member) : attribute.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute, name,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, attribute, false, null);
        }

        if (member.GetCustomAttribute<AttributeRefAttribute>() is { } attributeRef)
        {
            var name = string.IsNullOrWhiteSpace(attributeRef.Name)
                ? DeriveDataName(member)
                : attributeRef.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute, name,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, false, null);
        }

        if (member.GetCustomAttribute<EvitaAssociatedDataAttribute>() is { } associatedData)
        {
            var name = string.IsNullOrWhiteSpace(associatedData.Name)
                ? DeriveDataName(member)
                : associatedData.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.AssociatedData, name,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ReferenceProjection.PrimaryKey, associatedData);
        }

        if (member.GetCustomAttribute<AssociatedDataRefAttribute>() is { } associatedDataRef)
        {
            var name = string.IsNullOrWhiteSpace(associatedDataRef.Name)
                ? DeriveDataName(member)
                : associatedDataRef.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.AssociatedData, name,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null);
        }

        if (member.GetCustomAttribute<PriceForSaleAttribute>() is { } priceForSale)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.PriceForSale, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ReferenceProjection.PrimaryKey, null, null, priceForSale);
        }

        if (member.GetCustomAttribute<PriceRangeForSaleAttribute>() is not null)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.PriceRangeForSale, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null);
        }

        if (member.GetCustomAttribute<AccompanyingPriceAttribute>() is { } accompanying)
        {
            var name = string.IsNullOrWhiteSpace(accompanying.Name)
                ? DeriveDataName(member)
                : accompanying.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.AccompanyingPrice,
                name, true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, false, null);
        }

        if (member.GetCustomAttribute<EvitaPriceAttribute>() is { } price)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Price, null, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ReferenceProjection.PrimaryKey, null, price, null,
                price.PriceList);
        }

        if (member.GetCustomAttribute<EvitaReferenceAttribute>() is { } reference)
        {
            var name = string.IsNullOrWhiteSpace(reference.Name) ? DeriveDataName(member) : reference.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Reference, name, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, reference, ResolveReferenceProjection(shape.ItemType));
        }

        if (member.GetCustomAttribute<ReferenceRefAttribute>() is { } referenceRef)
        {
            var name = string.IsNullOrWhiteSpace(referenceRef.Name)
                ? DeriveDataName(member)
                : referenceRef.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Reference, name, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ResolveReferenceProjection(shape.ItemType));
        }

        // a reflected reference is a reference at read time - the server materialises it like any other
        if (member.GetCustomAttribute<ReflectedReferenceAttribute>() is { } reflected)
        {
            var name = string.IsNullOrWhiteSpace(reflected.Name) ? DeriveDataName(member) : reflected.Name;
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Reference, name, true,
                returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ResolveReferenceProjection(shape.ItemType), null, null, null,
                null, TypedMemberAccess.Read, false, -1, reflected);
        }

        if (member.GetCustomAttribute<ParentEntityAttribute>() is not null)
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.ParentEntity, null,
                true, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection, hasLocaleParameter,
                null, false, null, null, ResolveReferenceProjection(shape.ItemType));
        }

        // unannotated primary-key fallback by name and type
        if (!hasLocaleParameter
            && shape.Collection == TypedCollectionShape.Scalar
            && IntegerTypes.Contains(shape.ItemType)
            && PrimaryKeyNames.Contains(member.Name))
        {
            return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.PrimaryKey, null,
                false, returnType, shape.ItemType, shape.WrapInMaybe, shape.Collection,
                hasLocaleParameter, null, true, null);
        }

        // unannotated members are inferred as attributes and resolved leniently against the schema
        return new TypedMemberDescriptor(accessor, member.Name, TypedMemberKind.Attribute,
            DeriveDataName(member), false, returnType, shape.ItemType, shape.WrapInMaybe,
            shape.Collection, hasLocaleParameter, null, false, null);
    }

    /// <summary>
    /// Reads the accessor, return type and parameters of a property or method member.
    /// </summary>
    private static (MethodInfo Accessor, Type ReturnType, ParameterInfo[] Parameters) ReadMemberSignature(
        MemberInfo member) =>
        member switch
        {
            PropertyInfo property => (property.GetMethod!, property.PropertyType, []),
            MethodInfo method => (method, method.ReturnType, method.GetParameters()),
            _ => throw new EvitaInvalidUsageException($"Unsupported contract member `{member.Name}`.")
        };

    /// <summary>
    /// Returns why a member cannot be bound, or <c>null</c> when it can. Shared by entity and reference
    /// contracts, since the reasons are properties of the member rather than of the scope.
    /// </summary>
    private static string? UnbindableReason(
        MemberInfo member,
        MethodInfo accessor,
        ParameterInfo[] parameters,
        bool hasLocaleParameter)
    {
        if (member.GetCustomAttribute<NotBoundAttribute>() is not null)
        {
            return "the member is annotated with [NotBound]";
        }

        // A default interface member cannot be bound: DispatchProxy intercepts it, and re-dispatching to
        // the default body non-virtually would require Reflection.Emit, which is unavailable under AOT
        // and in Blazor WebAssembly. Generated contract implementations inherit such members naturally -
        // that is the supported path for computed members.
        if (accessor.DeclaringType?.IsInterface == true && !accessor.IsAbstract)
        {
            return "the member declares a default interface implementation, which the runtime proxy " +
                   "cannot delegate to";
        }

        // a reference member may take a filter argument - a referenced key, a predicate, or a value
        // compared against one of the reference's attributes
        if (parameters.Length == 1 && !hasLocaleParameter && CarriesReferenceAnnotation(member))
        {
            return null;
        }

        if (parameters.Length > 1 || (parameters.Length == 1 && !hasLocaleParameter))
        {
            return "only parameterless members, members taking a single CultureInfo, and reference " +
                   "members taking one filter argument can be bound";
        }

        return null;
    }

    private static TypedMemberDescriptor Unbound(
        MethodInfo accessor,
        string memberName,
        Type returnType,
        (Type ItemType, bool WrapInMaybe, TypedCollectionShape Collection) shape,
        bool hasLocaleParameter,
        string reason) =>
        new(accessor, memberName, TypedMemberKind.Unbound, null, false, returnType, shape.ItemType,
            shape.WrapInMaybe, shape.Collection, hasLocaleParameter, null, false, reason);

    /// <summary>
    /// Unwraps the declared return type into its item type, remembering the collection shape and
    /// whether three-state <see cref="Maybe{T}"/> semantics were requested. The order matters:
    /// <c>Maybe&lt;IReadOnlyList&lt;string&gt;&gt;</c> unwraps to a read-only list of strings.
    /// </summary>
    private static (Type ItemType, bool WrapInMaybe, TypedCollectionShape Collection) UnwrapShape(
        Type declaredType)
    {
        var current = declaredType;
        var wrapInMaybe = false;

        if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Maybe<>))
        {
            wrapInMaybe = true;
            current = current.GetGenericArguments()[0];
        }

        var collection = TypedCollectionShape.Scalar;

        if (current.IsArray && current != typeof(byte[]))
        {
            collection = TypedCollectionShape.Array;
            current = current.GetElementType()!;
        }
        else if (current.IsGenericType)
        {
            var definition = current.GetGenericTypeDefinition();
            var itemType = current.GetGenericArguments()[0];

            if (definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>) ||
                definition == typeof(IEnumerable<>))
            {
                collection = TypedCollectionShape.ReadOnlyList;
                current = itemType;
            }
            else if (definition == typeof(IList<>) || definition == typeof(List<>) ||
                     definition == typeof(ICollection<>))
            {
                collection = TypedCollectionShape.List;
                current = itemType;
            }
            else if (definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>) ||
                     definition == typeof(HashSet<>))
            {
                collection = TypedCollectionShape.Set;
                current = itemType;
            }
        }

        // a nullable value type carries no extra meaning for binding - the absence is expressed by
        // the null itself, or by Maybe<T> when the member wants three-state semantics
        current = Nullable.GetUnderlyingType(current) ?? current;

        return (current, wrapInMaybe, collection);
    }

    /// <summary>
    /// Derives an entity type name from a CLR type name: a leading <c>I</c> on interfaces and a
    /// trailing <c>Contract</c> are stripped, so <c>IProductContract</c> maps to <c>Product</c>.
    /// </summary>
    private static string DeriveEntityType(Type contractType)
    {
        var name = contractType.Name;

        if (contractType.IsInterface && name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
        {
            name = name[1..];
        }

        const string contractSuffix = "Contract";
        if (name.Length > contractSuffix.Length && name.EndsWith(contractSuffix, StringComparison.Ordinal))
        {
            name = name[..^contractSuffix.Length];
        }

        return name;
    }
}
