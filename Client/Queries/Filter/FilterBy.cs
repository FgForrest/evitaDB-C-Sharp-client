﻿namespace Client.Queries.Filter;

public class FilterBy : AbstractFilterConstraintContainer
{
    private FilterBy() {
    }
    
    public FilterBy(IFilterConstraint children) : base(children) {
    }

    public bool IsNecessary => Applicable;

    public IFilterConstraint? Child => GetChildrenCount() == 0 ? null : Children[0];

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children, IConstraint[] additionalChildren)
    {
        return children.Length > 0 ? new FilterBy((IFilterConstraint) children[0]) : new FilterBy();

    }
}