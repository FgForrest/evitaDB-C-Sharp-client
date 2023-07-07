namespace Client.Queries.Filter;

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

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children,
        IConstraint[] additionalChildren)
    {
        return children.Length == 0 ? new EntityHaving() : new EntityHaving(children[0]);
    }
}