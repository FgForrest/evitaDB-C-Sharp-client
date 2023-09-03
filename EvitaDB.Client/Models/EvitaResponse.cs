using System.Collections.Immutable;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Queries;

namespace EvitaDB.Client.Models;

public abstract class EvitaResponse<T> where T : IEntityClassifier
{
    public Query Query { get; }
    public IDataChunk<T> RecordPage { get; }
    public IDictionary<Type, IEvitaResponseExtraResult> ExtraResults => ExtraResultsInternal
        .ToImmutableDictionary(x => x.Key, x => x.Value);
    public IList<T> RecordData => RecordPage.Data ?? new List<T>();
    private Dictionary<Type, IEvitaResponseExtraResult> ExtraResultsInternal { get; } = new();
    protected EvitaResponse(Query query, IDataChunk<T> recordPage)
    {
        Query = query;
        RecordPage = recordPage;
    }
    
    protected EvitaResponse(Query query, IDataChunk<T> recordPage, params IEvitaResponseExtraResult[] extraResults)
    {
        Query = query;
        RecordPage = recordPage;
        foreach (var extraResult in extraResults)
        {
            ExtraResultsInternal.Add(extraResult.GetType(), extraResult);
        }
    }
    
    public ISet<Type> GetExtraResultTypes()
    {
        return new HashSet<Type>(ExtraResults.Keys);
    }
    
    public IEvitaResponseExtraResult? GetExtraResult(Type extraResultType)
    {
        return ExtraResults[extraResultType];
    }
}