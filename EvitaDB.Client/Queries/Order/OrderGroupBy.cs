using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `entityGroupProperty` ordering constraint can only be used within the <see cref="ReferenceContent"/> requirement.
/// It allows the context of the reference ordering to be changed from attributes of the reference itself to attributes
/// of the group entity within which the reference is aggregated.
/// In other words, if the Product entity has multiple references to ParameterValue entities that are grouped by their
/// assignment to the Parameter entity, you can sort those references primarily by the name attribute of the grouping
/// entity, and secondarily by the name attribute of the referenced entity. Let's look at an example:
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         attributeEquals("code", "garmin-vivoactive-4"),
///         entityLocaleEquals("en")
///     ),
///     require(
///         entityFetch(
///             attributeContent("code"),
///             referenceContent(
///                 "parameterValues",
///                 orderBy(
///                     entityGroupProperty(
///                         attributeNatural("name", ASC)
///                     ),
///                     entityProperty(
///                         attributeNatural("name", ASC)
///                     )
///                 ),
///                 entityFetch(
///                     attributeContent("name")
///                 ),
///                 entityGroupFetch(
///                     attributeContent("name")
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// </summary>
public class OrderGroupBy : AbstractOrderConstraintContainer
{
    public new bool Necessary => Applicable;
    
    public OrderGroupBy(params IOrderConstraint?[] children) : base(children)
    {
    }
    
    public IOrderConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new OrderGroupBy(children);
    }
}
