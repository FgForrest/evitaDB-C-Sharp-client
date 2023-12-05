namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `queryTelemetry` requirement triggers creation of the <see cref="QueryTelemetry"/> DTO and including it the evitaDB
/// response.
/// Example:
/// <code>
/// queryTelemetry()
/// </code>
/// </summary>
public class QueryTelemetry : AbstractRequireConstraintLeaf
{
    private QueryTelemetry(params object?[] arguments) : base(arguments)
    {
    }
    
    public QueryTelemetry() : base()
    {
    }
}
