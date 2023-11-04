using System.Collections.Immutable;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

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