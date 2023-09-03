namespace EvitaDB.Client.Queries.Requires;

public class QueryTelemetry : AbstractRequireConstraintLeaf
{
    private QueryTelemetry(params object[] arguments) : base(arguments)
    {
    }
    
    public QueryTelemetry() : base()
    {
    }
}