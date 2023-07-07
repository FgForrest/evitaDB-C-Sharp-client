using Client.Exceptions;

namespace Client.Queries.Requires;

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
            : new object[] {statisticsBase}.Concat(statisticsTypes.Cast<object>()))
    {
    }
}