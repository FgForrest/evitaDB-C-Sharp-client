using System.Text;
using Client.Utils;

namespace Client.Models.ExtraResults;

public class QueryTelemetry : IEvitaResponseExtraResult
{
    public QueryPhase Operation { get; }
    public long Start { get; }
    public List<QueryTelemetry> Steps { get; } = new();
    public string[] Arguments { get; private set; }
    public long SpentTime { get; private set; }

    public QueryTelemetry(QueryPhase operation, params string[] arguments)
    {
        Operation = operation;
        Arguments = arguments;
        Start = DateTime.UtcNow.Ticks;
    }
    
    public QueryTelemetry(QueryPhase operation, long start, long spentTime, List<QueryTelemetry> steps, params string[] arguments)
	{
		Operation = operation;
		Arguments = arguments;
		Start = start;
		Steps = steps;
		SpentTime = spentTime;
	}

    public QueryTelemetry Finish(params string[] arguments)
    {
        SpentTime += DateTime.UtcNow.Ticks - Start;
        Assert.IsTrue(arguments.Length == 0, "Arguments have been already set!");
        Arguments = arguments;
        return this;
    }

    public QueryTelemetry AddStep(QueryPhase operation, params string[] arguments)
    {
        QueryTelemetry step = new(operation, arguments);
        Steps.Add(step);
        return step;
    }

    public void AddStep(QueryTelemetry step)
    {
        Steps.Add(step);
    }

    public QueryTelemetry Finish()
    {
        SpentTime += DateTime.UtcNow.Ticks - Start;
        return this;
    }

    public override string ToString() => ToString(0);

    public string ToString(int indent)
    {
        StringBuilder sb = new StringBuilder(new string(' ', indent));
        sb.Append(Operation);
        if (Arguments.Length > 0)
        {
            sb.Append("(")
                .Append(string.Join(", ", Arguments.Select(x => x.ToString())))
                .Append(") ");
        }

        sb.Append(": ").Append(SpentTime).Append("\n");
        if (Steps.Any())
        {
            foreach (QueryTelemetry step in Steps)
            {
                sb.Append(step.ToString(indent + 5));
            }
        }

        return sb.ToString();
    }

    public enum QueryPhase
    {
        /**
		 * Entire query execution time.
		 */
        Overall,

        /**
		 * Entire planning phase of the query execution.
		 */
        Planning,

        /**
		 * Planning phase of the inner query execution.
		 */
        PlanningNestedQuery,

        /**
		 * Determining which indexes should be used.
		 */
        PlanningIndexUsage,

        /**
		 * Creating formula for filtering entities.
		 */
        PlanningFilter,

        /**
		 * Creating formula for nested query.
		 */
        PlanningFilterNestedQuery,

        /**
		 * Creating alternative formula for filtering entities.
		 */
        PlanningFilterAlternative,

        /**
		 * Creating formula for sorting result entities.
		 */
        PlanningSort,

        /**
		 * Creating alternative formula for sorting result entities.
		 */
        PlanningSortAlternative,

        /**
		 * Creating factories for requested extra results.
		 */
        PlanningExtraResultFabrication,

        /**
		 * Creating factories for requested extra results based on alternative indexes.
		 */
        PlanningExtraResultFabricationAlternative,

        /**
		 * Entire query execution phase.
		 */
        Execution,

        /**
		 * Prefetching entities that should be examined instead of consulting indexes.
		 */
        ExecutionPrefetch,

        /**
		 * Computing entities that should be returned in output (filtering).
		 */
        ExecutionFilter,

        /**
		 * Sorting output entities and slicing requested page.
		 */
        ExecutionSortAndSlice,

        /**
		 * Fabricating requested extra results.
		 */
        ExtraResultsFabrication,

        /**
		 * Fabricating requested single extra result.
		 */
        ExtraResultItemFabrication,

        /**
		 * Fetching rich data from the storage based on computed entity primary keys.
		 */
        Fetching,

        /**
		 * Fetching referenced entities and entity groups from the storage based on referenced primary keys information.
		 */
        FetchingReferences
    }
}