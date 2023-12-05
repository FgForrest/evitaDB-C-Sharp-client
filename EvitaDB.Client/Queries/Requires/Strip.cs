using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `strip` requirement controls the number and slice of entities returned in the query response. If the requested
/// strip exceeds the number of available records, a result from the zero offset with retained limit is returned.
/// An empty result is only returned if the query returns no result at all or the limit is set to zero. By automatically
/// returning the first strip result when the requested page is exceeded, we try to avoid the need to issue a secondary
/// request to fetch the data.
/// The information about the actual returned page and data statistics can be found in the query response, which is
/// wrapped in a so-called data chunk object. In case of the strip constraint, the <see cref="StripList{T}"/> is used as data
/// chunk object.
/// Example:
/// <code>
/// strip(52, 24)
/// </code>
/// </summary>
public class Strip : AbstractRequireConstraintLeaf
{
    public int Offset => (int) Arguments[0]!;
    public int Limit => (int) Arguments[1]!;
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 1;
    private Strip(params object?[] arguments) : base(arguments)
    {
    }
    
    public Strip(int? offset, int? limit) : base(offset ?? 0, limit ?? 20)
    {
    }
}
