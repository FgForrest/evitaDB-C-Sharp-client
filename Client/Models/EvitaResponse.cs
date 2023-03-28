﻿using System.Collections.Immutable;
using Client.DataTypes;
using Client.Models.Data;
using Client.Queries;

namespace Client.Models;

public abstract class EvitaResponse<T> where T : IEntityClassifier
{
    public Query Query { get; }
    public IDataChunk<T> RecordPage { get; }
    public IDictionary<Type, IEvitaResponseExtraResult> ExtraResults => ExtraResultsInternal
        .ToImmutableDictionary(x => x.Key, x => x.Value);
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