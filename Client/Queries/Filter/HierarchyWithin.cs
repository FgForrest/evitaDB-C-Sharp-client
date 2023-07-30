using Client.Exceptions;
using Client.Utils;

namespace Client.Queries.Filter;

public class HierarchyWithin : AbstractFilterConstraintContainer, IHierarchyFilterConstraint, IConstraintWithSuffix
{
    private const string Suffix = "self";

    public string? ReferenceName => Arguments.Length == 0 ? null : (string?) Arguments[0];
    public bool DirectRelation => Children.Any(x => x is HierarchyDirectRelation);

    public IFilterConstraint[] HavingChildrenFilter => Children.Where(x => x is HierarchyHaving)
        .Select(y => ((HierarchyHaving) y).Filtering)
        .FirstOrDefault() ?? Array.Empty<IFilterConstraint>();

    public IFilterConstraint[] ExcludeChildrenFilter => Children.Where(x => x is HierarchyExcluding)
        .Select(y => ((HierarchyExcluding) y).Filtering)
        .FirstOrDefault() ?? Array.Empty<IFilterConstraint>();

    public IFilterConstraint ParentFilter => Children
                                                 .FirstOrDefault(
                                                     x => x is not IHierarchySpecificationFilterConstraint) ??
                                             throw new EvitaInvalidUsageException(
                                                 "No filtering was specified for the HierarchyWithin constraint!");

    public IHierarchySpecificationFilterConstraint[] HierarchySpecificationConstraints => Children
        .OfType<IHierarchySpecificationFilterConstraint>()
        .ToArray();
    
    public bool ExcludingRoot => Children.Any(x => x is HierarchyExcludingRoot);

    public new bool Necessary => base.Necessary || Applicable;
    public new bool Applicable => Children.Length > 0;
    public string? SuffixIfApplied => ReferenceName is not null ? null : Suffix;
    bool IConstraintWithSuffix.ArgumentImplicitForSuffix(object argument) => false;
    private HierarchyWithin(object[] argument, IFilterConstraint[] fineGrainedConstraints,
        IConstraint[] additionalChildren) : base(argument, fineGrainedConstraints)
    {
        Assert.IsPremiseValid(additionalChildren.Length == 0,
            "Constraint hierarchyWithin accepts only filtering inner constraints!");
    }

    public HierarchyWithin(IFilterConstraint ofParent, params IHierarchySpecificationFilterConstraint[] with) : base(
        NoArguments, new[] {ofParent}.Concat(with).ToArray())
    {
    }

    public HierarchyWithin(string referenceName, IFilterConstraint ofParent,
        params IHierarchySpecificationFilterConstraint[] with) : base(
        new object[] {referenceName}, new[] {ofParent}.Concat(with).ToArray())
    {
    }

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint[] children,
        IConstraint[] additionalChildren)
    {
        return new HierarchyWithin(Arguments, children, additionalChildren);
    }
}