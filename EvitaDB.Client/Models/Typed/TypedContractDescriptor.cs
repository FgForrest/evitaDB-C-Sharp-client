using System.Reflection;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Kind of entity data a contract member is bound to.
/// </summary>
public enum TypedMemberKind
{
    /// <summary>The member exposes the entity primary key.</summary>
    PrimaryKey,

    /// <summary>The member exposes the entity type name.</summary>
    EntityType,

    /// <summary>The member exposes an entity attribute, or a reference attribute inside a reference contract.</summary>
    Attribute,

    /// <summary>The member exposes entity associated data.</summary>
    AssociatedData,

    /// <summary>The member exposes stored prices of the entity.</summary>
    Price,

    /// <summary>The member exposes the price evitaDB computed as the selling price.</summary>
    PriceForSale,

    /// <summary>The member exposes a price computed alongside the selling price.</summary>
    AccompanyingPrice,

    /// <summary>The member exposes the cheapest and most expensive selling price.</summary>
    PriceRangeForSale,

    /// <summary>The member exposes an entity reference.</summary>
    Reference,

    /// <summary>The member exposes the parent of a hierarchical entity.</summary>
    ParentEntity,

    /// <summary>Inside a reference contract, the member exposes the referenced entity.</summary>
    ReferencedEntity,

    /// <summary>Inside a reference contract, the member exposes the reference group.</summary>
    ReferencedEntityGroup,

    /// <summary>
    /// The member carries its own implementation and is never bound - either because it is annotated
    /// with <see cref="NotBoundAttribute"/> or because it declares a default implementation.
    /// </summary>
    Unbound
}

/// <summary>
/// Whether a contract member reads data, writes it, or removes it.
/// </summary>
public enum TypedMemberAccess
{
    /// <summary>The member reads data from the entity.</summary>
    Read,

    /// <summary>The member writes data into the entity's builder.</summary>
    Write,

    /// <summary>The member removes data from the entity's builder.</summary>
    Remove
}

/// <summary>
/// What a contract's members are interpreted against.
/// </summary>
public enum TypedContractScope
{
    /// <summary>Members read from a fetched entity.</summary>
    Entity,

    /// <summary>
    /// Members read from a single reference of an entity: its own attributes, the referenced entity and
    /// the reference group. Reference contracts need no type-level annotation - they are recognised by
    /// being the item type of a reference member.
    /// </summary>
    Reference
}

/// <summary>
/// How a reference member materialises its value, decided by the member's declared item type.
/// </summary>
public enum ReferenceProjection
{
    /// <summary>The referenced entity primary key, as an integer.</summary>
    PrimaryKey,

    /// <summary>A lightweight <see cref="Data.IEntityReference"/>.</summary>
    EntityReference,

    /// <summary>A nested typed view of the referenced entity; requires the referenced body to be fetched.</summary>
    ReferencedEntity,

    /// <summary>A nested typed view of the reference itself, exposing its attributes and navigation.</summary>
    ReferenceContract
}

/// <summary>
/// Collection shape a contract member declares around its item type.
/// </summary>
public enum TypedCollectionShape
{
    /// <summary>A single value.</summary>
    Scalar,

    /// <summary>An array of values (<c>T[]</c>).</summary>
    Array,

    /// <summary>A read-only list (<c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>).</summary>
    ReadOnlyList,

    /// <summary>A mutable list (<c>IList&lt;T&gt;</c>, <c>List&lt;T&gt;</c>, <c>ICollection&lt;T&gt;</c>).</summary>
    List,

    /// <summary>A set (<c>ISet&lt;T&gt;</c>, <c>HashSet&lt;T&gt;</c>, <c>IReadOnlySet&lt;T&gt;</c>).</summary>
    Set
}

/// <summary>
/// Describes one member of a typed entity contract. Everything here is derivable without a live
/// entity schema; schema-dependent facts (localization, indexed decimal places, defaults) are
/// resolved later, when the descriptor is bound to a schema.
/// </summary>
public sealed class TypedMemberDescriptor
{
    internal TypedMemberDescriptor(
        MethodInfo accessor,
        string memberName,
        TypedMemberKind kind,
        string? dataName,
        bool nameDeclaredExplicitly,
        Type declaredType,
        Type itemType,
        bool wrapInMaybe,
        TypedCollectionShape collectionShape,
        bool hasLocaleParameter,
        EvitaAttributeAttribute? attributeDeclaration,
        bool autoGeneratePrimaryKey,
        string? unboundReason,
        EvitaReferenceAttribute? referenceDeclaration = null,
        ReferenceProjection referenceProjection = ReferenceProjection.PrimaryKey,
        EvitaAssociatedDataAttribute? associatedDataDeclaration = null,
        EvitaPriceAttribute? priceDeclaration = null,
        PriceForSaleAttribute? priceForSaleDeclaration = null,
        string[]? priceLists = null,
        TypedMemberAccess access = TypedMemberAccess.Read,
        bool fluent = false,
        int localeParameterIndex = -1,
        ReflectedReferenceAttribute? reflectedDeclaration = null)
    {
        ReflectedDeclaration = reflectedDeclaration;
        Access = access;
        Fluent = fluent;
        LocaleParameterIndex = localeParameterIndex;
        ReferenceDeclaration = referenceDeclaration;
        ReferenceProjection = referenceProjection;
        AssociatedDataDeclaration = associatedDataDeclaration;
        PriceDeclaration = priceDeclaration;
        PriceForSaleDeclaration = priceForSaleDeclaration;
        PriceLists = priceLists ?? [];
        Accessor = accessor;
        MemberName = memberName;
        Kind = kind;
        DataName = dataName;
        NameDeclaredExplicitly = nameDeclaredExplicitly;
        DeclaredType = declaredType;
        ItemType = itemType;
        WrapInMaybe = wrapInMaybe;
        CollectionShape = collectionShape;
        HasLocaleParameter = hasLocaleParameter;
        AttributeDeclaration = attributeDeclaration;
        AutoGeneratePrimaryKey = autoGeneratePrimaryKey;
        UnboundReason = unboundReason;
    }

    /// <summary>The method that reads the member - a property getter, or the method itself.</summary>
    public MethodInfo Accessor { get; }

    /// <summary>Name of the property or method as declared on the contract.</summary>
    public string MemberName { get; }

    /// <summary>Kind of entity data this member reads.</summary>
    public TypedMemberKind Kind { get; }

    /// <summary>
    /// Name of the entity data this member reads, already derived from the member name when the
    /// annotation left it blank. <c>null</c> only for kinds that carry no name.
    /// </summary>
    public string? DataName { get; }

    /// <summary>
    /// True when an annotation named the data explicitly or the member carried an evitaDB annotation.
    /// Such members are resolved strictly - a missing schema element is an error. Members without any
    /// annotation are resolved leniently, mirroring the Java driver's soft inference branch.
    /// </summary>
    public bool NameDeclaredExplicitly { get; }

    /// <summary>The member's declared return type, before any unwrapping.</summary>
    public Type DeclaredType { get; }

    /// <summary>
    /// The innermost value type, after unwrapping <see cref="Maybe{T}"/>, the collection shape and
    /// <see cref="Nullable{T}"/>.
    /// </summary>
    public Type ItemType { get; }

    /// <summary>True when the member returns <see cref="Maybe{T}"/> and wants three-state semantics.</summary>
    public bool WrapInMaybe { get; }

    /// <summary>Collection shape declared around <see cref="ItemType"/>.</summary>
    public TypedCollectionShape CollectionShape { get; }

    /// <summary>True when the member is a method taking a single <see cref="System.Globalization.CultureInfo"/>.</summary>
    public bool HasLocaleParameter { get; }

    /// <summary>The attribute declaration driving schema derivation, when present.</summary>
    public EvitaAttributeAttribute? AttributeDeclaration { get; }

    /// <summary>Whether a primary-key member declares the key as server-generated.</summary>
    public bool AutoGeneratePrimaryKey { get; }

    /// <summary>Why the member is <see cref="TypedMemberKind.Unbound"/>, for diagnostics.</summary>
    public string? UnboundReason { get; }

    /// <summary>The reference declaration driving schema derivation, when present.</summary>
    public EvitaReferenceAttribute? ReferenceDeclaration { get; }

    /// <summary>
    /// The reflected-reference declaration, when the member reads a reference evitaDB maintains as the
    /// inverse of one declared on another entity.
    /// </summary>
    public ReflectedReferenceAttribute? ReflectedDeclaration { get; }

    /// <summary>
    /// How a <see cref="TypedMemberKind.Reference"/>, <see cref="TypedMemberKind.ParentEntity"/> or
    /// <see cref="TypedMemberKind.ReferencedEntityGroup"/> member materialises its value.
    /// </summary>
    public ReferenceProjection ReferenceProjection { get; }

    /// <summary>The associated-data declaration driving schema derivation, when present.</summary>
    public EvitaAssociatedDataAttribute? AssociatedDataDeclaration { get; }

    /// <summary>The price declaration driving schema derivation, when present.</summary>
    public EvitaPriceAttribute? PriceDeclaration { get; }

    /// <summary>The price-for-sale declaration driving schema derivation, when present.</summary>
    public PriceForSaleAttribute? PriceForSaleDeclaration { get; }

    /// <summary>Price lists a <see cref="TypedMemberKind.Price"/> member is restricted to; empty means all.</summary>
    public IReadOnlyList<string> PriceLists { get; }

    /// <summary>Whether the member reads, writes or removes data.</summary>
    public TypedMemberAccess Access { get; }

    /// <summary>
    /// True when a writing member returns the contract itself and should therefore return the editor for
    /// chaining, rather than the value it just wrote.
    /// </summary>
    public bool Fluent { get; }

    /// <summary>
    /// Position of the <see cref="System.Globalization.CultureInfo"/> parameter of a writing member, or
    /// <c>-1</c> when it takes none. Writers accept the locale on either side of the value.
    /// </summary>
    public int LocaleParameterIndex { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{MemberName} -> {Access} {Kind}{(DataName is null ? "" : $"('{DataName}')")}";
}

/// <summary>
/// Describes a typed entity contract: which entity type it maps to, whether it may drive schema
/// derivation, and how each of its members binds to entity data.
/// <remarks>
/// Produced by <see cref="TypedContractAnalyzer"/> through reflection today. A source generator can
/// emit the same shape at compile time without changing anything downstream.
/// </remarks>
/// </summary>
public sealed class TypedContractDescriptor
{
    internal TypedContractDescriptor(
        Type contractType,
        string entityType,
        bool drivesSchema,
        EvitaEntityAttribute? entityDeclaration,
        IReadOnlyList<TypedMemberDescriptor> members,
        TypedContractScope scope = TypedContractScope.Entity)
    {
        Scope = scope;
        ContractType = contractType;
        EntityType = entityType;
        DrivesSchema = drivesSchema;
        EntityDeclaration = entityDeclaration;
        Members = members;
    }

    /// <summary>The CLR type of the contract.</summary>
    public Type ContractType { get; }

    /// <summary>The evitaDB entity type the contract maps to.</summary>
    public string EntityType { get; }

    /// <summary>
    /// True when the contract was annotated with <see cref="EvitaEntityAttribute"/> and may therefore
    /// be used to derive an entity schema. Contracts annotated only with
    /// <see cref="EvitaEntityRefAttribute"/> are read-only.
    /// </summary>
    public bool DrivesSchema { get; }

    /// <summary>The entity declaration driving schema derivation, when present.</summary>
    public EvitaEntityAttribute? EntityDeclaration { get; }

    /// <summary>All described members, in declaration order.</summary>
    public IReadOnlyList<TypedMemberDescriptor> Members { get; }

    /// <summary>What the contract's members are interpreted against.</summary>
    public TypedContractScope Scope { get; }

    /// <inheritdoc />
    public override string ToString() =>
        $"{ContractType.Name} -> entity `{EntityType}` ({Members.Count} members)";
}
