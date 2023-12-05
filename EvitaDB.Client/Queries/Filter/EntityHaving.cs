namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `entityHaving` constraint is used to examine the attributes or other filterable properties of the referenced
/// entity. It can only be used within the referenceHaving constraint, which defines the name of the entity reference
/// that identifies the target entity to be subjected to the filtering restrictions in the entityHaving constraint.
/// The filtering constraints for the entity can use entire range of filtering operators.
/// Example:
/// <code>
/// referenceHaving(
///     "brand",
///     entityHaving(
///         attributeEquals("code", "apple")
///     )
/// )
/// </code>
/// </summary>
public class EntityHaving : AbstractFilterConstraintContainer
{
    private EntityHaving()
    {
    }

    public EntityHaving(IFilterConstraint child) : base(child)
    {
    }

    public IFilterConstraint Child => Children.Length > 0 ? Children[0] : null!;

    public new bool Necessary => Children.Length > 0;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return children.Length == 0 ? new EntityHaving() : new EntityHaving(children[0]!);
    }
}
