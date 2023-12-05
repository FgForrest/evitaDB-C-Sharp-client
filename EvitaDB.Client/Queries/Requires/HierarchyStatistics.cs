using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The statistics constraint allows you to retrieve statistics about the hierarchy nodes that are returned by the
/// current query. When used it triggers computation of the queriedEntityCount, childrenCount statistics, or both for
/// each hierarchy node in the returned hierarchy tree.
/// It requires mandatory argument of type <see cref="StatisticsType"/> enum that specifies which statistics to compute:
/// - <see cref="StatisticsType.ChildrenCount"/>: triggers calculation of the count of child hierarchy nodes that exist in
///   the hierarchy tree below the given node; the count is correct regardless of whether the children themselves are
///   requested/traversed by the constraint definition, and respects hierarchyOfReference settings for automatic removal
///   of hierarchy nodes that would contain empty result set of queried entities (<see cref="EmptyHierarchicalEntityBehaviour.RemoveEmpty"/>)
/// - <see cref="StatisticsType.QueriedEntityCount"/>: triggers the calculation of the total number of queried entities that
///   will be returned if the current query is focused on this particular hierarchy node using the hierarchyWithin filter
///   constraint (the possible refining constraint in the form of directRelation and excluding-root is not taken into
///   account).
/// And optional argument of type <see cref="StatisticsBase"/> enum allowing you to specify the base queried entity set that
/// is the source for statistics calculations:
/// - <see cref="Requires.StatisticsBase.CompleteFilter"/>: complete filtering query constraint
/// - <see cref="Requires.StatisticsBase.WithoutUserFilter"/>: filtering query constraint where the contents of optional userFilter
///    are ignored
/// The calculation always ignores hierarchyWithin because the focused part of the hierarchy tree is defined on
/// the requirement constraint level, but including having/excluding constraints. The having/excluding constraints are
/// crucial for the calculation of queriedEntityCount (and therefore also affects the value of childrenCount
/// transitively).
/// 
/// Computational complexity of statistical data calculation
/// The performance price paid for calculating statistics is not negligible. The calculation of <see cref="StatisticsType.ChildrenCount"/>
/// is cheaper because it allows to eliminate "dead branches" early and thus conserve the computation cycles.
/// The calculation of the <see cref="StatisticsType.QueriedEntityCount"/> is more expensive because it requires counting
/// items up to the last one and must be precise.
/// We strongly recommend that you avoid using <see cref="StatisticsType.QueriedEntityCount"/> for root hierarchy nodes for
/// large datasets.
/// This query actually has to filter and aggregate all the records in the database, which is obviously quite expensive,
/// even considering that all the indexes are in-memory. Caching is probably the only way out if you really need
/// to crunch these numbers.
/// </summary>
public class HierarchyStatistics : AbstractRequireConstraintLeaf, IHierarchyOutputRequireConstraint
{
    private const string ConstraintName = "statistics";

    public StatisticsBase StatisticsBase =>
        (StatisticsBase) (Arguments.SingleOrDefault(x => x is StatisticsBase) ?? throw new EvitaInternalError("StatisticsBase is mandatory argument, yet it was not found!"));

    public ISet<StatisticsType> StatisticsTypes =>
        Arguments.Where(x => x is StatisticsType).Cast<StatisticsType>().ToHashSet();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length >= 1;

    private HierarchyStatistics(params object[] arguments) : base(arguments)
    {
    }

    public HierarchyStatistics() : base(ConstraintName, StatisticsBase.WithoutUserFilter)
    {
    }

    public HierarchyStatistics(StatisticsBase statisticsBase) : base(ConstraintName, statisticsBase)
    {
    }

    public HierarchyStatistics(StatisticsBase statisticsBase, params StatisticsType[] statisticsTypes) : base(
        ConstraintName,
        statisticsTypes.Length == 0
            ? new object[] {statisticsBase}
            : new object[] {statisticsBase}.Concat(statisticsTypes.Cast<object>()).ToArray())
    {
    }
}
