using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Marks a type as a typed contract over an evitaDB entity collection. The contract can be used both
/// to derive an entity schema (see <c>DefineEntitySchemaFromModelTypeAsync</c>) and to read fetched
/// entities as instances of the contract.
/// <remarks>
/// Counterpart of the Java driver's <c>@Entity</c>. Use <see cref="EvitaEntityRefAttribute"/> instead
/// when the contract only reads data and should never drive schema definition.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true)]
public sealed class EvitaEntityAttribute : Attribute
{
    /// <summary>
    /// Declares a contract for the given entity type.
    /// </summary>
    /// <param name="name">
    /// entity type name; when omitted the type name is used with a leading <c>I</c> stripped from
    /// interfaces (<c>IProduct</c> becomes <c>Product</c>)
    /// </param>
    public EvitaEntityAttribute(string? name = null) => Name = name;

    /// <summary>Entity type name, or <c>null</c> to derive it from the CLR type name.</summary>
    public string? Name { get; }

    /// <summary>Optional human-readable description propagated into the derived schema.</summary>
    public string? Description { get; set; }

    /// <summary>Optional deprecation notice propagated into the derived schema.</summary>
    public string? DeprecationNotice { get; set; }

    /// <summary>
    /// When true, the derived schema declares a generated primary key. Ignored when a member is
    /// annotated with <see cref="PrimaryKeyAttribute"/>, whose <see cref="PrimaryKeyAttribute.AutoGenerate"/>
    /// states the intent explicitly and is the only source then. When false and no member is annotated,
    /// the schema's current setting is left alone; a primary key merely inferred from its name (<c>Id</c>)
    /// carries no schema intent.
    /// </summary>
    public bool GeneratedPrimaryKey { get; set; }

    /// <summary>Locales the derived schema should allow, as IETF language tags (for example <c>en</c>, <c>cs</c>).</summary>
    public string[]? AllowedLocales { get; set; }
}

/// <summary>
/// Read-only shortcut binding a contract to an entity type without expressing schema intent.
/// <remarks>Counterpart of the Java driver's <c>@EntityRef</c>.</remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct,
    Inherited = true)]
public sealed class EvitaEntityRefAttribute : Attribute
{
    /// <summary>Binds the contract to the named entity type.</summary>
    /// <param name="name">entity type name; when omitted it is derived from the CLR type name</param>
    public EvitaEntityRefAttribute(string? name = null) => Name = name;

    /// <summary>Entity type name, or <c>null</c> to derive it from the CLR type name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Binds a member to the entity primary key.
/// <remarks>
/// An unannotated member is also accepted as the primary key when it is integer-typed and named
/// <c>Id</c>, <c>PrimaryKey</c>, <c>Pk</c> or <c>EntityPrimaryKey</c> - mirroring the Java driver's
/// <c>PrimaryKeyRef.POSSIBLE_ARGUMENT_NAMES</c> fallback.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class PrimaryKeyAttribute : Attribute
{
    /// <summary>
    /// When true (the default) the derived schema declares the primary key as server-generated; when false
    /// it declares the key as caller-supplied, turning generation <em>off</em> on a schema that had it on.
    /// This setting wins over <see cref="EvitaEntityAttribute.GeneratedPrimaryKey"/>.
    /// </summary>
    public bool AutoGenerate { get; set; } = true;
}

/// <summary>
/// Binds a member to the entity type name of the fetched entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class EntityTypeAttribute : Attribute;

/// <summary>
/// Binds a member to an entity attribute and, when the contract drives schema derivation, declares
/// that attribute's schema.
/// <remarks>
/// Counterpart of the Java driver's <c>@Attribute</c>. Named <c>EvitaAttribute</c> because
/// <c>Attribute</c> collides with <see cref="System.Attribute"/>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class EvitaAttributeAttribute : Attribute
{
    /// <summary>Binds the member to an entity attribute.</summary>
    /// <param name="name">
    /// attribute name; when omitted the member name is converted to camel case (<c>Name</c> becomes
    /// <c>name</c>)
    /// </param>
    public EvitaAttributeAttribute(string? name = null) => Name = name;

    /// <summary>Attribute name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }

    /// <summary>Optional description propagated into the derived attribute schema.</summary>
    public string? Description { get; set; }

    /// <summary>Optional deprecation notice propagated into the derived attribute schema.</summary>
    public string? DeprecationNotice { get; set; }

    /// <summary>Declares the attribute filterable in the derived schema.</summary>
    public bool Filterable { get; set; }

    /// <summary>Declares the attribute sortable in the derived schema.</summary>
    public bool Sortable { get; set; }

    /// <summary>Declares the attribute localized in the derived schema.</summary>
    public bool Localized { get; set; }

    /// <summary>Declares the attribute nullable in the derived schema.</summary>
    public bool Nullable { get; set; } = true;

    /// <summary>Declares the attribute representative in the derived schema.</summary>
    public bool Representative { get; set; }

    /// <summary>Uniqueness constraint declared in the derived schema.</summary>
    public AttributeUniquenessType Unique { get; set; } = AttributeUniquenessType.NotUnique;

    /// <summary>
    /// Number of decimal places indexed for decimal attributes. Only meaningful for
    /// <see cref="decimal"/>-typed members.
    /// </summary>
    public int IndexedDecimalPlaces { get; set; }
}

/// <summary>
/// Read-only shortcut binding a member to an existing attribute without expressing schema intent.
/// <remarks>
/// On a <b>parameter</b> of a reference member it means something related but distinct: the argument is
/// compared against that attribute of each reference, keeping the ones that match.
/// Counterpart of the Java driver's <c>@AttributeRef</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AttributeRefAttribute : Attribute
{
    /// <summary>Binds the member to the named attribute.</summary>
    /// <param name="name">attribute name; when omitted it is derived from the member name</param>
    public AttributeRefAttribute(string? name = null) => Name = name;

    /// <summary>Attribute name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Binds a member to entity associated data and, when the contract drives schema derivation, declares
/// that associated data's schema.
/// <remarks>
/// Associated data carries payloads that are stored but never indexed. Members typed as one of
/// evitaDB's supported data types are read directly; any other type is deserialized from the stored
/// complex object, so a plain POCO works as the member type.
/// Counterpart of the Java driver's <c>@AssociatedData</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class EvitaAssociatedDataAttribute : Attribute
{
    /// <summary>Binds the member to associated data.</summary>
    /// <param name="name">
    /// associated data name; when omitted the member name is converted to camel case
    /// </param>
    public EvitaAssociatedDataAttribute(string? name = null) => Name = name;

    /// <summary>Associated data name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }

    /// <summary>Optional description propagated into the derived schema.</summary>
    public string? Description { get; set; }

    /// <summary>Optional deprecation notice propagated into the derived schema.</summary>
    public string? DeprecationNotice { get; set; }

    /// <summary>Declares the associated data localized in the derived schema.</summary>
    public bool Localized { get; set; }

    /// <summary>Declares the associated data nullable in the derived schema.</summary>
    public bool Nullable { get; set; } = true;
}

/// <summary>
/// Read-only shortcut binding a member to existing associated data without expressing schema intent.
/// <remarks>Counterpart of the Java driver's <c>@AssociatedDataRef</c>.</remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class AssociatedDataRefAttribute : Attribute
{
    /// <summary>Binds the member to the named associated data.</summary>
    /// <param name="name">associated data name; when omitted it is derived from the member name</param>
    public AssociatedDataRefAttribute(string? name = null) => Name = name;

    /// <summary>Associated data name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Binds a member to the entity's stored prices, and declares the entity priced when the contract
/// drives schema derivation.
/// <remarks>
/// The member's item type must be assignable to <see cref="IPrice"/>. A collection member returns all
/// matching prices; a scalar member returns the first. Counterpart of the Java driver's <c>@Price</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class EvitaPriceAttribute : Attribute
{
    /// <summary>Restricts the member to prices of the given price lists; empty means all prices.</summary>
    public string[]? PriceList { get; set; }

    /// <summary>
    /// Number of decimal places indexed for prices, propagated into the derived entity schema.
    /// </summary>
    public int IndexedPricePlaces { get; set; } = 2;

    /// <summary>
    /// Currency codes the derived schema should allow (for example <c>CZK</c>, <c>EUR</c>).
    /// </summary>
    public string[]? AllowedCurrencies { get; set; }
}

/// <summary>
/// Binds a member to the price evitaDB computed as the selling price for the current query context.
/// <remarks>
/// A scalar member returns the selling price; a collection member returns all prices eligible for sale.
/// Requires the query to have established a price context (currency and price lists).
/// Counterpart of the Java driver's <c>@PriceForSale</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class PriceForSaleAttribute : Attribute
{
    /// <summary>
    /// Number of decimal places indexed for prices, propagated into the derived entity schema.
    /// </summary>
    public int IndexedPricePlaces { get; set; } = 2;

    /// <summary>Currency codes the derived schema should allow.</summary>
    public string[]? AllowedCurrencies { get; set; }
}

/// <summary>
/// Binds a member to the range of prices for which the entity could be sold.
/// <remarks>
/// <para>
/// The member's type must be <see cref="Data.PriceRangeForSale"/>. Populated from the server-computed
/// <c>priceForSaleMin</c> / <c>priceForSaleMax</c>, so it needs the same price context as
/// <see cref="PriceForSaleAttribute"/> and nothing more in the query.
/// </para>
/// <para>
/// This is what a product with variants shows as "from EUR 5 to EUR 6.50". Note that for a product <em>set</em>
/// the bounds are the cheapest and most expensive component rather than a range around the selling price - see
/// <see cref="Data.PriceRangeForSale"/> before rendering one.
/// </para>
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class PriceRangeForSaleAttribute : Attribute
{
}

/// <summary>
/// Binds a member to an accompanying price - one evitaDB computed alongside the price for sale, from a
/// different price-list priority.
/// <remarks>
/// Accompanying prices are requested per query with <c>AccompanyingPriceContent(name, priceLists)</c>, not
/// declared in the schema, so this annotation only reads. The member's type must be <see cref="IPrice"/>.
/// Counterpart of the Java driver's <c>@AccompanyingPrice</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class AccompanyingPriceAttribute : Attribute
{
    /// <summary>Binds the member to an accompanying price.</summary>
    /// <param name="name">
    /// name used in the query's <c>AccompanyingPriceContent</c>; when omitted the member name is converted
    /// to camel case, and <c>"default"</c> matches a query that named none
    /// </param>
    public AccompanyingPriceAttribute(string? name = null) => Name = name;

    /// <summary>Accompanying price name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Binds a member to an entity reference and, when the contract drives schema derivation, declares
/// that reference's schema.
/// <remarks>
/// <para>
/// The member's item type decides what is returned: an integer yields the referenced primary key,
/// <see cref="IEntityReference"/> yields a lightweight reference, a type annotated with
/// <see cref="EvitaEntityAttribute"/> or <see cref="EvitaEntityRefAttribute"/> yields a nested typed
/// view of the referenced entity, and any other type is treated as a reference contract exposing the
/// reference's own attributes.
/// </para>
/// <para>Counterpart of the Java driver's <c>@Reference</c>.</para>
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class EvitaReferenceAttribute : Attribute
{
    /// <summary>Binds the member to an entity reference.</summary>
    /// <param name="name">
    /// reference name; when omitted the member name is converted to camel case
    /// </param>
    public EvitaReferenceAttribute(string? name = null) => Name = name;

    /// <summary>Reference name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }

    /// <summary>Optional description propagated into the derived reference schema.</summary>
    public string? Description { get; set; }

    /// <summary>Optional deprecation notice propagated into the derived reference schema.</summary>
    public string? DeprecationNotice { get; set; }

    /// <summary>
    /// Entity type this reference points to. Required for schema derivation unless the member's item
    /// type is itself an annotated entity contract, in which case it is taken from there.
    /// </summary>
    public string? Entity { get; set; }

    /// <summary>Whether the referenced entity type is managed by evitaDB (the default).</summary>
    public bool Managed { get; set; } = true;

    /// <summary>Optional group entity type of the reference.</summary>
    public string? GroupEntity { get; set; }

    /// <summary>Whether the group entity type is managed by evitaDB (the default).</summary>
    public bool GroupEntityManaged { get; set; } = true;

    /// <summary>Declares the reference indexed in the derived schema.</summary>
    public bool Indexed { get; set; }

    /// <summary>Declares the reference faceted in the derived schema. Implies indexing on the server.</summary>
    public bool Faceted { get; set; }

    /// <summary>
    /// Whether the reference may be missing. Together with the declared collection shape this decides
    /// the derived <see cref="Cardinality"/>.
    /// </summary>
    public bool AllowEmpty { get; set; } = true;
}

/// <summary>
/// Binds a member to a <b>reflected</b> reference - one evitaDB maintains automatically as the inverse of
/// a reference declared on the other entity.
/// <remarks>
/// <para>
/// A category does not store its products; the product stores its categories, and the category exposes the
/// reflection of that. Reading works exactly like a normal reference, so every projection
/// (<see cref="int"/>, <see cref="IEntityReference"/>, a nested entity contract, or a reference contract)
/// applies here too.
/// </para>
/// <para>
/// Reflected references are <b>read-only</b>: the server derives them from the other side, so the way to
/// change one is to change the reference it reflects.
/// </para>
/// <para>Counterpart of the Java driver's <c>@ReflectedReference</c>.</para>
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ReflectedReferenceAttribute : Attribute
{
    /// <summary>Binds the member to a reflected reference.</summary>
    /// <param name="name">
    /// name of the reflected reference on this entity; when omitted it is derived from the member name
    /// </param>
    public ReflectedReferenceAttribute(string? name = null) => Name = name;

    /// <summary>Reflected reference name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }

    /// <summary>Entity declaring the reference that is being reflected.</summary>
    public string? OfEntity { get; set; }

    /// <summary>Name of the reference on <see cref="OfEntity"/> that is being reflected.</summary>
    public string? OfName { get; set; }

    /// <summary>Declares the reflected reference faceted.</summary>
    public bool Faceted { get; set; }

    /// <summary>How attributes of the reflected reference are inherited from the original.</summary>
    public AttributeInheritanceBehavior AttributeInheritance { get; set; } =
        AttributeInheritanceBehavior.InheritAllExcept;

    /// <summary>Attribute names excluded from (or selected for) inheritance, per <see cref="AttributeInheritance"/>.</summary>
    public string[]? AttributeInheritanceFilter { get; set; }
}

/// <summary>
/// Read-only shortcut binding a member to an existing reference without expressing schema intent.
/// <remarks>Counterpart of the Java driver's <c>@ReferenceRef</c>.</remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ReferenceRefAttribute : Attribute
{
    /// <summary>Binds the member to the named reference.</summary>
    /// <param name="name">reference name; when omitted it is derived from the member name</param>
    public ReferenceRefAttribute(string? name = null) => Name = name;

    /// <summary>Reference name, or <c>null</c> to derive it from the member name.</summary>
    public string? Name { get; }
}

/// <summary>
/// Binds a member to the parent of a hierarchical entity, and declares the entity hierarchical when
/// the contract drives schema derivation.
/// <remarks>
/// An integer member yields the parent primary key; a member typed as an entity contract yields a
/// nested typed view, which requires the parent body to have been fetched.
/// Counterpart of the Java driver's <c>@ParentEntity</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ParentEntityAttribute : Attribute;

/// <summary>
/// Inside a reference contract, binds a member to the referenced entity itself.
/// <remarks>Counterpart of the Java driver's <c>@ReferencedEntity</c>.</remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ReferencedEntityAttribute : Attribute;

/// <summary>
/// Inside a reference contract, binds a member to the reference's group.
/// <remarks>
/// An integer member yields the group primary key, an <see cref="IEntityReference"/> member the group
/// reference, and an entity-contract member a nested typed view of the group entity (which requires the
/// group body to have been fetched). Counterpart of the Java driver's <c>@ReferencedEntityGroup</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class ReferencedEntityGroupAttribute : Attribute;

/// <summary>
/// Marks a member of an editor contract as removing the data it is bound to, rather than writing it.
/// <remarks>
/// The member takes no value - only an optional <see cref="System.Globalization.CultureInfo"/> when the
/// data is localized. Counterpart of the Java driver's <c>@RemoveWhenExists</c>.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RemoveWhenExistsAttribute : Attribute;

/// <summary>
/// Excludes a member from automatic binding. The member must then carry its own implementation -
/// a default interface member, or an implemented member on a class contract.
/// <remarks>
/// Counterpart of the Java driver's "a method with a body is left
/// alone" rule.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class NotBoundAttribute : Attribute;
