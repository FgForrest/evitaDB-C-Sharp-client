using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// Sorting by reference attribute is not as common as sorting by entity attributes, but it allows you to sort entities
/// that are in a particular category or have a particular brand specifically by the priority/order for that particular
/// relationship.
/// To sort products related to a "Sony" brand by the `priority` attribute set on the reference, you need to use the
/// following constraint:
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         referenceHaving(
///             "brand",
///             entityHaving(
///                 attributeEquals("code","sony")
///             )
///         )
///     ),
///     orderBy(
///         referenceProperty(
///             "brand",
///             attributeNatural("orderInBrand", ASC)
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code"),
///             referenceContentWithAttributes(
///                 "brand",
///                 attributeContent("orderInBrand")
///             )
///         )
///     )
/// )
/// </code>
/// **The `referenceProperty` is implicit in requirement `referenceContent`**
/// In the `orderBy` clause within the <see cref="ReferenceContent"/> requirement,
/// the `referenceProperty` constraint is implicit and must not be repeated. All attribute order constraints
/// in `referenceContent` automatically refer to the reference attributes, unless the <see cref="EntityProperty"/> or
/// <see cref="EntityGroupProperty"/> container is used there.
/// The example is based on a simple one-to-zero-or-one reference (a product can have at most one reference to a brand
/// entity). The response will only return the products that have a reference to the "Sony" brand, all of which contain the
/// `orderInBrand` attribute (since it's marked as a non-nullable attribute). Because the example is so simple, the returned
/// result can be anticipated.
/// ## Behaviour of zero or one to many references ordering
/// The situation is more complicated when the reference is one-to-many. What is the expected result of a query that
/// involves ordering by a property on a reference attribute? Is it wise to allow such ordering query in this case?
/// We decided to allow it and bind it with the following rules:
/// ### Non-hierarchical entity
/// If the referenced entity is **non-hierarchical**, and the returned entity references multiple entities, only
/// the reference with the lowest primary key of the referenced entity, while also having the order property set, will be
/// used for ordering.
/// ### Hierarchical entity
/// If the referenced entity is **hierarchical** and the returned entity references multiple entities, the reference used
/// for ordering is the one that contains the order property and is the closest hierarchy node to the root of the filtered
/// hierarchy node.
/// It sounds complicated, but it's really quite simple. If you list products of a certain category and at the same time
/// order them by a property "priority" set on the reference to the category, the first products will be those directly
/// related to the category, ordered by "priority", followed by the products of the first child category, and so on,
/// maintaining the depth-first order of the category tree.
/// </summary>
public class ReferenceProperty : AbstractOrderConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;
    public new bool Necessary => Children.Length >= 1;

    private ReferenceProperty(object[] arguments, params IOrderConstraint?[] children) : base(arguments, children)
    {
    }

    public ReferenceProperty(string referenceName, params IOrderConstraint?[] children) : base(referenceName, children)
    {
    }

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new ReferenceProperty(ReferenceName, children);
    }
}
