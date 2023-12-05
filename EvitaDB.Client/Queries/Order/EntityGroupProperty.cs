using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `entityGroupProperty` ordering constraint can only be used within the <see cref="ReferenceContent"/> requirement. It
/// allows to change the context of the reference ordering from attributes of the reference itself to attributes of
/// the entity group the reference is aggregated within.
/// In other words, if the `Product` entity has multiple references to `Parameter` entities (blue/red/yellow) grouped
/// within `ParameterType` (color) entity, you can sort those references by, for example, the `priority` or `name`
/// attribute of the `ParameterType` entity.
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         attributeEquals("code", "garmin-vivoactive-4")
///     ),
///     require(
///         entityFetch(
///             attributeContent("code"),
///             referenceContent(
///                 "parameterValues",
///                 orderBy(
///                     entityGroupProperty(
///                         attributeNatural("code", DESC)
///                     )
///                 ),
///                 entityFetch(
///                     attributeContent("code")
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// Most of the time, you will want to group primarily by a group property and secondarily by a referenced entity
/// property, which can be achieved in the following way:
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         attributeEquals("code", "garmin-vivoactive-4")
///     ),
///     require(
///         entityFetch(
///             attributeContent("code"),
///             referenceContent(
///                 "parameterValues",
///                 orderBy(
///                     entityGroupProperty(
///                         attributeNatural("code", DESC)
///                     ),
///                     entityProperty(
///                         attributeNatural("code", DESC)
///                     )
///                 ),
///                 entityFetch(
///                     attributeContent("code")
///                 )
///             )
///         )
///     )
/// )
/// </code>
/// </summary>
public class EntityGroupProperty : AbstractOrderConstraintContainer
{
    public new bool Necessary => Children.Length >= 1;

    private EntityGroupProperty(object?[] arguments, params IOrderConstraint?[] children) : base(arguments, children)
    {
    }
    
    public EntityGroupProperty(params IOrderConstraint?[] children) : base(children)
    {
    }
    
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityGroupProperty(children);
    }
}
