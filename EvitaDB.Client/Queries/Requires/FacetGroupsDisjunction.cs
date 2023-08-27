using Client.Exceptions;
using Client.Queries.Filter;
using Client.Utils;

namespace Client.Queries.Requires;

public class FacetGroupsDisjunction : AbstractRequireConstraintContainer
{
    public string ReferenceName => (string) Arguments[0]!;

    public FilterBy FacetGroups => AdditionalChildren.OfType<FilterBy>().FirstOrDefault() ??
                                   throw new EvitaInvalidUsageException("FacetGroupsDisjunction requires FilterBy constraint.");

    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 0 && AdditionalChildren.Length > 0;

    private FacetGroupsDisjunction(object[] arguments, params IConstraint[] additionalChildren) : base(arguments,
        NoChildren, additionalChildren)
    {
        foreach (IConstraint child in additionalChildren)
        {
            Assert.IsPremiseValid(child is FilterBy,
                "Only FilterBy constraints are allowed in FacetGroupsDisjunction.");
        }
    }

    public FacetGroupsDisjunction(string referenceName, FilterBy filterBy) : base(new object[] {referenceName},
        NoChildren, filterBy)
    {
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        Assert.IsPremiseValid(children.Length == 0, "Children must be empty.");
        return new FacetGroupsDisjunction(Arguments, additionalChildren);
    }
}