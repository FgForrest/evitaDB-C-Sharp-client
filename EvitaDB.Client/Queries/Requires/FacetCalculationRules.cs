namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `facetCalculationRules` requirement changes the default boolean relations used when combining selected
/// facets - the first argument applies to facets of the same group, the second to facets of different groups.
/// Example:
/// <code>
/// facetCalculationRules(CONJUNCTION, EXCLUSIVITY)
/// </code>
/// </summary>
public class FacetCalculationRules : AbstractRequireConstraintLeaf
{
    public FacetRelationType FacetsWithSameGroup => (FacetRelationType) Arguments[0]!;

    public FacetRelationType FacetsWithDifferentGroups => (FacetRelationType) Arguments[1]!;

    private FacetCalculationRules(params object?[] arguments) : base(arguments)
    {
    }

    public FacetCalculationRules(FacetRelationType? facetsWithSameGroup, FacetRelationType? facetsWithDifferentGroups)
        : base(
            facetsWithSameGroup ?? FacetRelationType.Disjunction,
            facetsWithDifferentGroups ?? FacetRelationType.Conjunction
        )
    {
    }
}
