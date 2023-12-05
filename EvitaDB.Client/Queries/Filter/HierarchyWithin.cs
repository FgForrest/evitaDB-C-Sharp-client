using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `hierarchyWithin` allows you to restrict the search to only those entities that are part of
/// the hierarchy tree starting with the root node identified by the first argument of this constraint. In e-commerce
/// systems the typical representative of a hierarchical entity is a category, which will be used in all of our examples.
/// The constraint accepts following arguments:
/// - optional name of the queried entity reference schema that represents the relationship to the hierarchical entity
///   type, your entity may target different hierarchical entities in different reference types, or it may target
///   the same hierarchical entity through multiple semantically different references, and that is why the reference name
///   is used instead of the target entity type.
/// - a single mandatory filter constraint that identifies one or more hierarchy nodes that act as hierarchy roots;
///   multiple constraints must be enclosed in AND / OR containers
/// - optional constraints allow you to narrow the scope of the hierarchy; none or all of the constraints may be present:
/// <list type="bullet">
///     <item><term><see cref="HierarchyDirectRelation"/></term></item>
///     <item><term><see cref="HierarchyHaving"/></term></item>
///     <item><term><see cref="HierarchyExcluding"/></term></item>
///     <item><term><see cref="HierarchyExcludingRoot"/></term></item>
/// </list>
/// The most straightforward usage is filtering the hierarchical entities themselves.
/// <code>
/// query(
///     collection("Category"),
///     filterBy(
///         hierarchyWithinSelf(
///             attributeEquals("code", "accessories")
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// The `hierarchyWithin` constraint can also be used for entities that directly reference a hierarchical entity type.
/// The most common use case from the e-commerce world is a product that is assigned to one or more categories.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "accessories")
///         )
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// Products assigned to two or more subcategories of Accessories category will only appear once in the response
/// (contrary to what you might expect if you have experience with SQL).
/// </summary>
public class HierarchyWithin : AbstractFilterConstraintContainer, IHierarchyFilterConstraint, IConstraintContainerWithSuffix
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
    private HierarchyWithin(object?[] argument, IFilterConstraint?[] fineGrainedConstraints,
        IConstraint?[] additionalChildren) : base(argument, fineGrainedConstraints)
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

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyWithin(Arguments, children, additionalChildren);
    }
}
