using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `entityProperty` ordering constraint can only be used within the <see cref="ReferenceContent"/> requirement. It allows
/// to change the context of the reference ordering from attributes of the reference itself to attributes of the entity
/// the reference points to.
/// In other words, if the `Product` entity has multiple references to `Parameter` entities, you can sort those references
/// by, for example, the `priority` or `name` attribute of the `Parameter` entity.
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
public class EntityProperty : AbstractOrderConstraintContainer
{
    public new bool Necessary => Children.Length >= 1;

    private EntityProperty(object[] arguments, params IOrderConstraint[] children) : base(arguments, children) {
    }
    
    public EntityProperty(params IOrderConstraint?[] children) : base(children) {
    }
    
    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityProperty(children);
    }
}
