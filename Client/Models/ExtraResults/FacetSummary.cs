using System.Collections.Immutable;
using Client.Models.Data;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.ExtraResults;

public class FacetSummary : IEvitaResponseExtraResult
{
    private readonly Dictionary<string, Dictionary<int, FacetGroupStatistics>> _facetGroupStatisticsWithIds;
    private readonly Dictionary<string, FacetGroupStatistics> _facetGroupStatisticsWithoutIds;

    public FacetSummary(ICollection<FacetGroupStatistics> facetGroupStatistics)
    {
        _facetGroupStatisticsWithIds = new Dictionary<string, Dictionary<int, FacetGroupStatistics>>();
        _facetGroupStatisticsWithoutIds = new Dictionary<string, FacetGroupStatistics>();
        foreach (FacetGroupStatistics stat in facetGroupStatistics)
        {
            int? groupId = stat.GroupEntity?.PrimaryKey;
            if (!groupId.HasValue)
            {
                _facetGroupStatisticsWithoutIds.Add(stat.ReferenceName, stat);
            }
            else
            {
                if (!_facetGroupStatisticsWithIds.ContainsKey(stat.ReferenceName))
                {
                    _facetGroupStatisticsWithIds.Add(stat.ReferenceName, new Dictionary<int, FacetGroupStatistics>());
                }

                Dictionary<int, FacetGroupStatistics> groupById = _facetGroupStatisticsWithIds[stat.ReferenceName];
                Assert.IsPremiseValid(!groupById.ContainsKey(groupId.Value),
                    $"There is already facet group for reference `{stat.ReferenceName}` with id `{groupId}`.");
                groupById.Add(groupId.Value, stat);
            }
        }
    }

    public FacetGroupStatistics? GetFacetGroupStatistics(string referencedEntityType) =>
        _facetGroupStatisticsWithoutIds.TryGetValue(referencedEntityType, out FacetGroupStatistics? stats)
            ? stats
            : null;

    public FacetGroupStatistics? GetFacetGroupStatistics(string referencedEntityType, int groupId) =>
        _facetGroupStatisticsWithIds.TryGetValue(referencedEntityType,
            out Dictionary<int, FacetGroupStatistics>? groupById)
            ? groupById[groupId]
            : null;

    public ICollection<FacetGroupStatistics> GetFacetGroupStatistics() =>
        _facetGroupStatisticsWithIds.Values.SelectMany(x => x.Values)
            .Concat(_facetGroupStatisticsWithoutIds.Values)
            .ToList();

    public override int GetHashCode()
    {
        return HashCode.Combine(_facetGroupStatisticsWithIds);
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        FacetSummary that = (FacetSummary) o;

        foreach (KeyValuePair<string, Dictionary<int, FacetGroupStatistics>> referenceEntry in
                 _facetGroupStatisticsWithIds)
        {
            Dictionary<int, FacetGroupStatistics> statistics = referenceEntry.Value;
            that._facetGroupStatisticsWithIds.TryGetValue(referenceEntry.Key,
                out Dictionary<int, FacetGroupStatistics>? thatStats);
            if (thatStats == null || statistics.Count != thatStats.Count)
            {
                return false;
            }

            using IEnumerator<KeyValuePair<int, FacetGroupStatistics>> it = statistics.GetEnumerator();
            using IEnumerator<KeyValuePair<int, FacetGroupStatistics>> thatIt = thatStats.GetEnumerator();
            while (it.MoveNext())
            {
                KeyValuePair<int, FacetGroupStatistics> entry = it.Current;
                thatIt.MoveNext();
                KeyValuePair<int, FacetGroupStatistics> thatEntry = thatIt.Current;
                if (!Equals(entry.Key, thatEntry.Key) || !Equals(entry.Value, thatEntry.Value))
                {
                    return false;
                }
            }
        }

        foreach (KeyValuePair<string, FacetGroupStatistics> referenceEntry in _facetGroupStatisticsWithoutIds)
        {
            FacetGroupStatistics statistics = referenceEntry.Value;
            that._facetGroupStatisticsWithoutIds.TryGetValue(referenceEntry.Key, out FacetGroupStatistics? thatStats);
            if (thatStats == null || statistics.Count != thatStats.Count)
            {
                return false;
            }

            if (!Equals(statistics, thatStats))
            {
                return false;
            }
        }

        return _facetGroupStatisticsWithIds.Equals(that._facetGroupStatisticsWithIds) &&
               _facetGroupStatisticsWithoutIds.Equals(that._facetGroupStatisticsWithoutIds);
    }

    public override string ToString()
    {
        return ToString(statistics => "", facetStatistics => "");
    }

    public string ToString(Func<FacetGroupStatistics, string> groupRenderer,
        Func<FacetStatistics, string> facetRenderer)
    {
        return "Facet summary:\n" + string.Join("\n", _facetGroupStatisticsWithIds
            .OrderBy(entry => entry.Key)
            .SelectMany(groupsByReferenceName => groupsByReferenceName.Value
                .Values
                .Select(statistics => "\t" + groupsByReferenceName.Key + ": " +
                                      (groupRenderer(statistics).Trim() != ""
                                          ? groupRenderer(statistics)
                                          : statistics.GroupEntity?.PrimaryKey.ToString() ?? "") +
                                      " [" + statistics.Count + "]:\n" +
                                      string.Join("\n", statistics
                                          .GetFacetStatistics()
                                          .Concat(_facetGroupStatisticsWithoutIds.Values
                                              .SelectMany(x => x.GetFacetStatistics()))
                                          .Select(facet => "\t\t[" + (facet.Requested ? "X" : " ") + "] " +
                                                           (facetRenderer(facet).Trim() != ""
                                                               ? facetRenderer(facet)
                                                               : facet.FacetEntity.PrimaryKey.ToString()) +
                                                           " (" + facet.Count + ")" +
                                                           (facet.Impact != null ? " " + facet.Impact : "")
                                          )
                                      )
                )
            )
        );
    }
}

public record RequestImpact(int Difference, int MatchCount)
{
    public bool HasSense => MatchCount > 0;

    public override int GetHashCode()
    {
        return HashCode.Combine(Difference, MatchCount);
    }

    public virtual bool Equals(RequestImpact? other)
    {
        if (this == other) return true;
        return Difference == other?.Difference && MatchCount == other.MatchCount;
    }

    public override string ToString()
    {
        return Difference switch
        {
            > 0 => $"+{Difference}",
            < 0 => $"{Difference}",
            _ => "0"
        };
    }
}

public class FacetStatistics : IComparable<FacetStatistics>
{
    public IEntityClassifier FacetEntity { get; }
    public bool Requested { get; }
    public int Count { get; }
    public RequestImpact? Impact { get; }

    public FacetStatistics(IEntityClassifier facetEntity, bool requested, int count, RequestImpact? impact)
    {
        FacetEntity = facetEntity;
        Requested = requested;
        Count = count;
        Impact = impact;
    }

    public int CompareTo(FacetStatistics? other)
    {
        if (FacetEntity.PrimaryKey is null && other?.FacetEntity.PrimaryKey is not null)
        {
            return -1;
        }

        if (FacetEntity.PrimaryKey is not null && other?.FacetEntity.PrimaryKey is null)
        {
            return 1;
        }

        if (FacetEntity.PrimaryKey is null && other?.FacetEntity.PrimaryKey is null)
        {
            return 0;
        }

        return (int) FacetEntity.PrimaryKey?.CompareTo(other?.FacetEntity.PrimaryKey)!;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FacetEntity, Requested, Count, Impact);
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        FacetStatistics that = (FacetStatistics) o;
        return Requested == that.Requested && Count == that.Count && Equals(FacetEntity, that.FacetEntity) &&
               Equals(Impact, that.Impact);
    }

    public override string ToString()
    {
        return $"FacetStatistics[facetEntity={FacetEntity}, requested={Requested}, count={Count}, impact={Impact}]";
    }
}

public class FacetGroupStatistics
{
    public string ReferenceName { get; }
    public IEntityClassifier? GroupEntity { get; }
    public int Count { get; }

    private readonly Dictionary<int, FacetStatistics> _facetStatisticsWithGroups;
    private readonly ICollection<FacetStatistics> _facetStatisticsWithoutGroups;

    public FacetStatistics? GetFacetStatistics(int facetId) =>
        _facetStatisticsWithGroups.TryGetValue(facetId, out FacetStatistics? facetStatistics) ? facetStatistics : null;

    public ICollection<FacetStatistics> GetFacetStatistics() =>
        _facetStatisticsWithGroups.Values.Concat(_facetStatisticsWithoutGroups).ToImmutableList();

    private static void VerifyGroupType(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity)
    {
        if (groupEntity != null)
        {
            string schemaGroupType = referenceSchema.ReferencedGroupType ?? referenceSchema.ReferencedEntityType;
            Assert.IsPremiseValid(groupEntity.EntityType.Equals(schemaGroupType),
                $"Group entity is from different collection (`{groupEntity.EntityType}`) than the group or entity (`{schemaGroupType}`).");
        }
    }

    public FacetGroupStatistics(string referenceName, IEntityClassifier? groupEntity, int count,
        Dictionary<int, FacetStatistics> facetStatisticsWithGroups,
        ICollection<FacetStatistics> facetStatisticsWithoutGroups)
    {
        ReferenceName = referenceName;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatisticsWithGroups = facetStatisticsWithGroups;
        _facetStatisticsWithoutGroups = facetStatisticsWithoutGroups;
    }

    public FacetGroupStatistics(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity, int count,
        Dictionary<int, FacetStatistics> facetStatisticsWithGroups,
        ICollection<FacetStatistics> facetStatisticsWithoutGroups)
    {
        VerifyGroupType(referenceSchema, groupEntity);
        ReferenceName = referenceSchema.Name;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatisticsWithGroups = facetStatisticsWithGroups;
        _facetStatisticsWithoutGroups = facetStatisticsWithoutGroups;
    }

    public FacetGroupStatistics(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity, int count,
        ICollection<FacetStatistics> facetStatistics)
    {
        VerifyGroupType(referenceSchema, groupEntity);
        ReferenceName = referenceSchema.Name;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatisticsWithGroups = facetStatistics
            .Where(x=>x.FacetEntity.PrimaryKey != null)
            .GroupBy(x => x)
            .Select(y => y.Key)
            .ToDictionary(
                key => key.FacetEntity.PrimaryKey!.Value,
                value => value
            );
        _facetStatisticsWithoutGroups = facetStatistics
            .Where(x=>x.FacetEntity.PrimaryKey == null)
            .ToImmutableList();
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        FacetGroupStatistics that = (FacetGroupStatistics) o;
        if (!ReferenceName.Equals(that.ReferenceName) ||
            Count != that.Count ||
            Equals(GroupEntity, that.GroupEntity) ||
            _facetStatisticsWithGroups.Count != that._facetStatisticsWithGroups.Count ||
            _facetStatisticsWithoutGroups.Count != that._facetStatisticsWithoutGroups.Count)
        {
            return false;
        }

        using IEnumerator<KeyValuePair<int, FacetStatistics>> it = _facetStatisticsWithGroups.GetEnumerator();
        using IEnumerator<KeyValuePair<int, FacetStatistics>> thatIt = that._facetStatisticsWithGroups.GetEnumerator();
        while (it.MoveNext())
        {
            KeyValuePair<int, FacetStatistics> entry = it.Current;
            KeyValuePair<int, FacetStatistics> thatEntry = thatIt.Current;
            if (!Equals(entry.Key, thatEntry.Key) || !Equals(entry.Value, thatEntry.Value))
            {
                return false;
            }
        }

        using IEnumerator<FacetStatistics> itWithout = _facetStatisticsWithoutGroups.GetEnumerator();
        using IEnumerator<FacetStatistics> thatItWithout = that._facetStatisticsWithoutGroups.GetEnumerator();
        while (it.MoveNext())
        {
            FacetStatistics entry = itWithout.Current;
            FacetStatistics thatEntry = thatItWithout.Current;
            if (!Equals(entry, thatEntry))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReferenceName, GroupEntity?.EntityType, Count, _facetStatisticsWithGroups,
            _facetStatisticsWithoutGroups);
    }
}