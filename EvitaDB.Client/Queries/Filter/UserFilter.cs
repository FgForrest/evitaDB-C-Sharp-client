using System.Collections.Immutable;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `userFilter` works identically to the and constraint, but it distinguishes the filter scope, which is controlled
/// by the user through some kind of user interface, from the rest of the query, which contains the mandatory constraints
/// on the result set. The user-defined scope can be modified during certain calculations (such as the facet or histogram
/// calculation), while the mandatory part outside of `userFilter` cannot.
/// Example:
/// <code>
/// userFilter(
///   facetHaving(
///     "brand",
///     entityHaving(
///       attributeInSet("code", "amazon")
///     )
///   )
/// )
/// </code>
/// </summary>
public class UserFilter : AbstractFilterConstraintContainer
{
    private static readonly ISet<Type> ForbiddenTypes;

    static UserFilter()
    {
        ForbiddenTypes = new HashSet<Type>
        {
            typeof(EntityLocaleEquals),
            typeof(PriceInCurrency),
            typeof(PriceInPriceLists),
            typeof(PriceValidIn),
            typeof(HierarchyWithin),
            typeof(HierarchyWithinRoot),
            typeof(ReferenceHaving),
            typeof(UserFilter)
        }.ToImmutableHashSet();
    }

    public UserFilter(params IFilterConstraint?[] children) : base(children)
    {
        if (children.Select(x => x?.Type).Any(ForbiddenTypes.Contains!))
        {
            throw new EvitaInvalidUsageException(
                $"Constraint(s) {string.Join(", ", children.Select(x => x?.Type)
                    .Where(ForbiddenTypes.Contains!).Select(y => y?.Name)
                    .Select(StringUtils.Uncapitalize!))} are forbidden in {Name} query container!"
                );
        }
    }

    public new bool Necessary => Children.Length > 0;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new UserFilter(children);
    }
}
