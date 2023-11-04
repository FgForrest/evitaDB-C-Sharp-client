namespace EvitaDB.Client.Queries.Filter;

public class And : AbstractFilterConstraintContainer
{
    public And(params IFilterConstraint?[] children) : base(children)
    {
    }
    
    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new And(children);
    }
}