using System.Collections.Immutable;
using Client.Models.Data;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.ExtraResults;

public class FacetSummary : IEvitaResponseExtraResult
{
    private readonly IDictionary<string, ReferenceStatistics> _referenceStatistics;

    public FacetSummary(IDictionary<string, ICollection<FacetGroupStatistics>> referenceStatistics)
    {
        Dictionary<string, ReferenceStatistics> result = new();
        foreach (KeyValuePair<string, ICollection<FacetGroupStatistics>> stats in referenceStatistics)
        {
            FacetGroupStatistics? nonGroupedStatistics = stats.Value
                .FirstOrDefault(x => x.GroupEntity is null);
            result.Add(stats.Key,
                new ReferenceStatistics(nonGroupedStatistics,
                    stats.Value.Where(x => x.GroupEntity is not null)
                        .ToDictionary(key => key.GroupEntity!.PrimaryKey!.Value, value => value)));
        }

        _referenceStatistics = result.ToImmutableDictionary();
    }

    public FacetSummary(ICollection<FacetGroupStatistics> referenceStatistics)
    {
        _referenceStatistics = referenceStatistics.GroupBy(x => x.ReferenceName).ToList()
            .ToImmutableDictionary(key => key.Key,
                value => new ReferenceStatistics(
                    value.ToList().FirstOrDefault(group => group.GroupEntity is null),
                    value.Where(group => group.GroupEntity is not null)
                        .ToDictionary(key => key.GroupEntity!.PrimaryKey!.Value, v => v)));
    }

    public FacetGroupStatistics? GetFacetGroupStatistics(string referencedEntityType) =>
        _referenceStatistics.TryGetValue(referencedEntityType, out ReferenceStatistics? stats)
            ? stats.NonGroupedStatistics
            : null;

    public FacetGroupStatistics? GetFacetGroupStatistics(string referencedEntityType, int groupId) =>
        _referenceStatistics.TryGetValue(referencedEntityType,
            out ReferenceStatistics? stats)
            ? stats.GetFacetGroupStatistics(groupId)
            : null;

    public ICollection<FacetGroupStatistics> GetFacetGroupStatistics() =>
        _referenceStatistics.Values.SelectMany(x =>
            {
                ICollection<FacetGroupStatistics> rs = x.GroupedStatistics.Values;
                if (x.NonGroupedStatistics is not null)
                {
                    rs.Add(x.NonGroupedStatistics);
                }
                return rs;
            })
            .ToList();

    public override int GetHashCode()
    {
        return HashCode.Combine(_referenceStatistics);
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        FacetSummary that = (FacetSummary) o;

        foreach (KeyValuePair<string, ReferenceStatistics> referenceEntry in _referenceStatistics)
        {
            ReferenceStatistics statistics = referenceEntry.Value;
            ReferenceStatistics? thatStatistics =
                that._referenceStatistics.TryGetValue(referenceEntry.Key, out var refStats) ? refStats : null;
            if (thatStatistics is null || !statistics.Equals(thatStatistics))
            {
                return false;
            }
        }

        return true;
    }

    public override string ToString()
    {
        return ToString(statistics => "", facetStatistics => "");
    }

    public string ToString(Func<FacetGroupStatistics, string> groupRenderer,
        Func<FacetStatistics, string> facetRenderer)
    {
        return "Facet summary:\n" + string.Join("\n", _referenceStatistics
            .OrderBy(entry => entry.Key)
            .SelectMany(groupsByReferenceName =>
                {
                    ReferenceStatistics stats = groupsByReferenceName.Value;
                    ICollection<FacetGroupStatistics> groupStatistics = stats.GroupedStatistics.Values;
                    if (stats.NonGroupedStatistics is not null)
                    {
                        groupStatistics.Add(stats.NonGroupedStatistics);
                    }

                    return groupStatistics.Select(statistics => "\t" + groupsByReferenceName.Key + ": " +
                                                                (groupRenderer(statistics).Trim() != ""
                                                                    ? groupRenderer(statistics)
                                                                    : statistics.GroupEntity?.PrimaryKey.ToString() ??
                                                                      "") +
                                                                " [" + statistics.Count + "]:\n" +
                                                                string.Join("\n", statistics
                                                                    .GetFacetStatistics()
                                                                    .Select(facet => "\t\t[" +
                                                                        (facet.Requested ? "X" : " ") + "] " +
                                                                        (facetRenderer(facet).Trim() != ""
                                                                            ? facetRenderer(facet)
                                                                            : facet.FacetEntity.PrimaryKey.ToString()) +
                                                                        " (" + facet.Count + ")" +
                                                                        (facet.Impact != null ? " " + facet.Impact : "")
                                                                    )
                                                                )
                    );
                }
            ));
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

public record ReferenceStatistics(FacetGroupStatistics? NonGroupedStatistics,
    Dictionary<int, FacetGroupStatistics> GroupedStatistics)
{
    public FacetGroupStatistics? GetFacetGroupStatistics(int groupId)
    {
        return GroupedStatistics.TryGetValue(groupId, out var groupStatistics) ? groupStatistics : null;
    }

    public override int GetHashCode()
    {
        int result = NonGroupedStatistics != null ? NonGroupedStatistics.GetHashCode() : 0;
        result = 31 * result + GroupedStatistics.GetHashCode();
        return result;
    }

    public virtual bool Equals(ReferenceStatistics? that)
    {
        if (this == that) return true;
        if (that == null || GetType() != that.GetType()) return false;

        if (!Equals(NonGroupedStatistics, that.NonGroupedStatistics))
        {
            return false;
        }

        Dictionary<int, FacetGroupStatistics> statistics = GroupedStatistics;
        Dictionary<int, FacetGroupStatistics> thatStatistics = that.GroupedStatistics;
        if (statistics.Count != thatStatistics.Count)
        {
            return false;
        }

        using Dictionary<int, FacetGroupStatistics>.Enumerator it = statistics.GetEnumerator();
        using Dictionary<int, FacetGroupStatistics>.Enumerator thatIt = statistics.GetEnumerator();
        while (it.MoveNext())
        {
            KeyValuePair<int, FacetGroupStatistics> kvp = it.Current;
            thatIt.MoveNext();
            KeyValuePair<int, FacetGroupStatistics> thatKvp = thatIt.Current;
            if (!Equals(kvp.Key, thatKvp.Key) || !Equals(kvp.Value, thatKvp.Value))
            {
                return false;
            }
        }

        return true;
    }
}

public class FacetGroupStatistics
{
    public string ReferenceName { get; }
    public IEntityClassifier? GroupEntity { get; }
    public int Count { get; }

    private readonly Dictionary<int, FacetStatistics> _facetStatistics;

    public FacetStatistics? GetFacetStatistics(int facetId) =>
        _facetStatistics.TryGetValue(facetId, out FacetStatistics? facetStatistics) ? facetStatistics : null;

    public ICollection<FacetStatistics> GetFacetStatistics() =>
        _facetStatistics.Values.ToImmutableList();

    private static void VerifyGroupType(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity)
    {
        if (groupEntity != null)
        {
            string schemaGroupType = referenceSchema.ReferencedGroupType ?? referenceSchema.ReferencedEntityType;
            Assert.IsPremiseValid(groupEntity.Type.Equals(schemaGroupType),
                $"Group entity is from different collection (`{groupEntity.Type}`) than the group or entity (`{schemaGroupType}`).");
        }
    }

    public FacetGroupStatistics(string referenceName, IEntityClassifier? groupEntity, int count,
        Dictionary<int, FacetStatistics> facetStatistics)
    {
        ReferenceName = referenceName;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatistics = facetStatistics;
    }

    public FacetGroupStatistics(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity, int count,
        Dictionary<int, FacetStatistics> facetStatistics)
    {
        VerifyGroupType(referenceSchema, groupEntity);
        ReferenceName = referenceSchema.Name;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatistics = facetStatistics;
    }

    public FacetGroupStatistics(ReferenceSchema referenceSchema, IEntityClassifier? groupEntity, int count,
        ICollection<FacetStatistics> facetStatistics)
    {
        VerifyGroupType(referenceSchema, groupEntity);
        ReferenceName = referenceSchema.Name;
        GroupEntity = groupEntity;
        Count = count;
        _facetStatistics = facetStatistics
            .Where(x => x.FacetEntity.PrimaryKey != null)
            .GroupBy(x => x)
            .Select(y => y.Key)
            .ToDictionary(
                key => key.FacetEntity.PrimaryKey!.Value,
                value => value
            );
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        FacetGroupStatistics that = (FacetGroupStatistics) o;
        if (!ReferenceName.Equals(that.ReferenceName) ||
            Count != that.Count ||
            Equals(GroupEntity, that.GroupEntity) ||
            _facetStatistics.Count != that._facetStatistics.Count)
        {
            return false;
        }

        using IEnumerator<KeyValuePair<int, FacetStatistics>> it = _facetStatistics.GetEnumerator();
        using IEnumerator<KeyValuePair<int, FacetStatistics>> thatIt = that._facetStatistics.GetEnumerator();
        while (it.MoveNext())
        {
            KeyValuePair<int, FacetStatistics> entry = it.Current;
            KeyValuePair<int, FacetStatistics> thatEntry = thatIt.Current;
            if (!Equals(entry.Key, thatEntry.Key) || !Equals(entry.Value, thatEntry.Value))
            {
                return false;
            }
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReferenceName, GroupEntity?.Type, Count, _facetStatistics);
    }
}