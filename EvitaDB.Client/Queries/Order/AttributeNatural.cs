using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The constraint allows output entities to be sorted by their attributes in their natural order (numeric, alphabetical,
/// temporal). It requires specification of a single attribute and the direction of the ordering (default ordering is
/// <see cref="OrderDirection.Asc"/>.
/// Ordering is executed by natural order of the Java's Comparable interface.
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     orderBy(
///         attributeNatural("orderedQuantity", DESC)
///     ),
///     require(
///         entityFetch(
///             attributeContent("code", "orderedQuantity")
///         )
///     )
/// )
/// </code>
/// If you want to sort products by their name, which is a localized attribute, you need to specify the <see cref="EntityLocaleEquals"/>
/// constraint in the <see cref="FilterBy"/> part of the query. The correct collator is used to
/// order the localized attribute string, so that the order is consistent with the national customs of the language.
/// The sorting mechanism of evitaDB is somewhat different from what you might be used to. If you sort entities by two
/// attributes in an `orderBy` clause of the query, evitaDB sorts them first by the first attribute (if present) and then
/// by the second (but only those where the first attribute is missing). If two entities have the same value of the first
/// attribute, they are not sorted by the second attribute, but by the primary key (in ascending order).
/// If we want to use fast "pre-sorted" indexes, there is no other way to do it, because the secondary order would not be
/// known until a query time. If you want to sort by multiple attributes in the conventional way, you need to define the
/// sortable attribute compound in advance and use its name instead of the default attribute name. The sortable attribute
/// compound will cover multiple attributes and prepares a special sort index for this particular combination of
/// attributes, respecting the predefined order and NULL values behaviour. In the query, you can then use the compound
/// name instead of the default attribute name and achieve the expected results.
/// </summary>
public class AttributeNatural : AbstractOrderConstraintLeaf
{
    public string AttributeName => (string) Arguments[0]!;
    public OrderDirection Direction => (OrderDirection) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 2;
    private AttributeNatural(params object?[] args) : base(args)
    {
    }

    public AttributeNatural(string attributeName) : base(attributeName, OrderDirection.Asc)
    {
    }
    
    public AttributeNatural(string attributeName, OrderDirection direction) : base(attributeName, direction)
    {
    }
}
