using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

public class HierarchyWithinRoot : AbstractFilterConstraintContainer, ISeparateEntityScopeContainer,
    IConstraintWithSuffix
{
    private const string Suffix = "self";

    public string? ReferenceName
    {
        get
        {
            object? firstArgument = Arguments.Length > 0 ? Arguments[0] : null;
            return firstArgument is int ? null : (string?) firstArgument;
        }
    }

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

    public new bool Necessary => true;
    public new bool Applicable => true;
    public string? SuffixIfApplied => ReferenceName is not null ? null : Suffix;
    public bool ArgumentImplicitForSuffix(object argument) => false;
    private HierarchyWithinRoot(object[] argument, IFilterConstraint[] fineGrainedConstraints,
        params IConstraint[] additionalChildren) : base(argument, fineGrainedConstraints, additionalChildren)
    {
        string? referenceName = ReferenceName;
        foreach (IFilterConstraint filterConstraint in fineGrainedConstraints)
        {
            Assert.IsTrue(
                filterConstraint is HierarchyExcluding or HierarchyHaving ||
                filterConstraint is HierarchyDirectRelation && referenceName is null,
                $"Constraint hierarchyWithinRoot accepts only {(referenceName is null ? "Excluding, Having, or DirectRelation when it targets same entity type" :
                    "Excluding when it targets different entity type")} as inner query!");
        }

        Assert.IsPremiseValid(additionalChildren.Length == 0,
            $"Constraint hierarchyWithinRoot accepts only {(referenceName is null ? "Excluding, Having, or DirectRelation when it targets same entity type" :
                "Excluding when it targets different entity type")} as inner query!");
    }

    public HierarchyWithinRoot(params IHierarchySpecificationFilterConstraint[] with) : this(
        NoArguments, with)
    {
    }

    public HierarchyWithinRoot(string referenceName, params IHierarchySpecificationFilterConstraint[] with) : this(
        new object[] {referenceName}, with)
    {
    }

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint[] additionalChildren)
    {
        return new HierarchyWithinRoot(Arguments, children, additionalChildren);
    }
}