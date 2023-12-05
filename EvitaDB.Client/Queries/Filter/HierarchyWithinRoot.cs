using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The constraint `hierarchyWithinRoot` allows you to restrict the search to only those entities that are part of
/// the entire hierarchy tree. In e-commerce systems the typical representative of a hierarchical entity is a category.
/// The single difference to <see cref="HierarchyWithin"/> constraint is that it doesn't accept a root node specification.
/// Because evitaDB accepts multiple root nodes in your entity hierarchy, it may be helpful to imagine there is
/// an invisible "virtual" top root above all the top nodes (whose parent property remains NULL) you have in your entity
/// hierarchy and this virtual top root is targeted by this constraint.
/// The constraint accepts following arguments:
/// - optional name of the queried entity reference schema that represents the relationship to the hierarchical entity
///   type, your entity may target different hierarchical entities in different reference types, or it may target
///   the same hierarchical entity through multiple semantically different references, and that is why the reference name
///   is used instead of the target entity type.
/// - optional constraints allow you to narrow the scope of the hierarchy; none or all of the constraints may be present:
/// <list type="bullet">
///     <item><term><see cref="HierarchyDirectRelation"/></term></item>
///     <item><term><see cref="HierarchyHaving"/></term></item>
///     <item><term><see cref="HierarchyExcluding"/></term></item>
/// </list>
/// The `hierarchyWithinRoot`, which targets the Category collection itself, returns all categories except those that
/// would point to non-existent parent nodes, such hierarchy nodes are called orphans and do not satisfy any hierarchy
/// query.
/// <code>
/// query(
///     collection("Category"),
///     filterBy(
///         hierarchyWithinRootSelf()
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// The `hierarchyWithinRoot` constraint can also be used for entities that directly reference a hierarchical entity
/// type. The most common use case from the e-commerce world is a product that is assigned to one or more categories.
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithinRoot("categories")
///     ),
///     require(
///         entityFetch(
///             attributeContent("code")
///         )
///     )
/// )
/// </code>
/// Products assigned to only one orphan category will be missing from the result. Products assigned to two or more
/// categories will only appear once in the response (contrary to what you might expect if you have experience with SQL).
/// </summary>
public class HierarchyWithinRoot : AbstractFilterConstraintContainer, ISeparateEntityScopeContainer,
    IConstraintContainerWithSuffix
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
    private HierarchyWithinRoot(object?[] argument, IFilterConstraint?[] fineGrainedConstraints,
        params IConstraint?[] additionalChildren) : base(argument, fineGrainedConstraints, additionalChildren)
    {
        string? referenceName = ReferenceName;
        foreach (IFilterConstraint? filterConstraint in fineGrainedConstraints)
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

    public HierarchyWithinRoot(params IHierarchySpecificationFilterConstraint?[] with) : this(
        NoArguments, with)
    {
    }

    public HierarchyWithinRoot(string referenceName, params IHierarchySpecificationFilterConstraint?[] with) : this(
        new object[] {referenceName}, with)
    {
    }

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HierarchyWithinRoot(Arguments, children, additionalChildren);
    }
}
