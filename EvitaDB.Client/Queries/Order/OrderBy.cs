namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// This `orderBy` is container for ordering. It is mandatory container when any ordering is to be used. 
/// evitaDB requires a previously prepared sort index to be able to sort entities. This fact makes sorting much faster 
/// than ad-hoc sorting by attribute value. Also, the sorting mechanism of evitaDB is somewhat different from what you
/// might be used to. If you sort entities by two attributes in an orderBy clause of the query, evitaDB sorts them first
/// by the first attribute (if present) and then by the second (but only those where the first attribute is missing). 
/// If two entities have the same value of the first attribute, they are not sorted by the second attribute, but by the
/// primary key (in ascending order). If we want to use fast "pre-sorted" indexes, there is no other way to do it, 
/// because the secondary order would not be known until a query time.
/// This default sorting behavior by multiple attributes is not always desirable, so evitaDB allows you to define 
/// a sortable attribute compound, which is a virtual attribute composed of the values of several other attributes.
/// evitaDB also allows you to specify the order of the "pre-sorting" behavior (ascending/descending) for each of these
/// attributes, and also the behavior for NULL values (first/last) if the attribute is completely missing in the entity.
/// The sortable attribute compound is then used in the orderBy clause of the query instead of specifying the multiple 
/// individual attributes to achieve the expected sorting behavior while maintaining the speed of the "pre-sorted" 
/// indexes.
/// Example:
/// <code>
/// orderBy(
///     ascending("code"),
///     ascending("create"),
///     priceDescending()
/// )
/// </code>
/// </summary>
public class OrderBy : AbstractOrderConstraintContainer, IOrderConstraint
{
    public new bool Necessary => Applicable;
    public OrderBy(params IOrderConstraint?[] children) : base(children)
    {
    }
    public IOrderConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new OrderBy(children);
    }
}
