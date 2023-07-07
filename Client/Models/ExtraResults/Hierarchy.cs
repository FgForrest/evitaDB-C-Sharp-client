﻿using System.Text;
using Client.Models.Data;

namespace Client.Models.ExtraResults;

public class Hierarchy : IEvitaResponseExtraResult
{
    private readonly IDictionary<string, List<LevelInfo>>? _selfStatistics;
    private readonly IDictionary<string, Dictionary<string, List<LevelInfo>>>? _referenceHierarchies;

    public IDictionary<string, List<LevelInfo>> GetSelfHierarchy() =>
        _selfStatistics ?? new Dictionary<string, List<LevelInfo>>();

    public List<LevelInfo> GetSelfHierarchy(string outputName) =>
        _selfStatistics?[outputName] ?? new List<LevelInfo>();

    public List<LevelInfo> GetReferenceHierarchy(string referenceName, string outputName) =>
        _referenceHierarchies?[referenceName][outputName] ?? new List<LevelInfo>();

    public IDictionary<string, List<LevelInfo>>? GetReferenceHierarchy(string referenceName) => _referenceHierarchies?[referenceName] ?? new Dictionary<string, List<LevelInfo>>();
    public IDictionary<string, Dictionary<string, List<LevelInfo>>>? GetReferenceHierarchies() => _referenceHierarchies;

    public Hierarchy(IDictionary<string, List<LevelInfo>>? selfStatistics,
        IDictionary<string, Dictionary<string, List<LevelInfo>>> referenceHierarchies)
    {
        _selfStatistics = selfStatistics;
        _referenceHierarchies = referenceHierarchies;
    }

    private static bool NotEquals(List<LevelInfo> stats, List<LevelInfo>? otherStats)
    {
        if (otherStats is null)
        {
            return false;
        }

        for (int i = 0; i < stats.Count; i++)
        {
            LevelInfo levelInfo = stats[i];
            LevelInfo otherLevelInfo = otherStats[i];

            if (!levelInfo.Equals(otherLevelInfo))
            {
                return true;
            }
        }

        return false;
    }

    public override bool Equals(object? o)
    {
        if (this == o) return true;
        if (o == null || GetType() != o.GetType()) return false;
        Hierarchy that = (Hierarchy) o;

        if (_selfStatistics == null && that._selfStatistics != null && that._selfStatistics.Any())
        {
            return false;
        }

        if (_selfStatistics != null && _selfStatistics.Any() && that._selfStatistics == null)
        {
            return false;
        }

        if (_selfStatistics != null)
        {
            foreach (var (key, stats) in _selfStatistics)
            {
                List<LevelInfo>? otherStats =
                    that._selfStatistics != null &&
                    that._selfStatistics.TryGetValue(key, out List<LevelInfo>? value)
                        ? value
                        : null;

                int otherSize = otherStats?.Count ?? 0;
                if (stats.Count != otherSize)
                {
                    return false;
                }

                if (NotEquals(stats, otherStats))
                {
                    return false;
                }
            }
        }

        if (_referenceHierarchies is not null)
        {
            foreach (var (key, stats) in _referenceHierarchies)
            {
                Dictionary<string, List<LevelInfo>>? otherStats = that._referenceHierarchies?[key];

                int otherSize = otherStats?.Count ?? 0;
                if (stats.Count != otherSize)
                {
                    return false;
                }

                foreach (KeyValuePair<string, List<LevelInfo>> entry in stats)
                {
                    List<LevelInfo> innerStats = entry.Value;
                    List<LevelInfo>? innerOtherStats =
                        otherStats != null && otherStats.TryGetValue(entry.Key, out List<LevelInfo>? value)
                            ? value
                            : null;

                    int innerSize = innerOtherStats?.Count ?? 0;
                    if (innerStats.Count != innerSize)
                    {
                        return false;
                    }

                    if (NotEquals(innerStats, innerOtherStats))
                    {
                        return false;
                    }
                }
            }
        }

        else if (_referenceHierarchies is null && that._referenceHierarchies is not null &&
                 that._referenceHierarchies.Any())
        {
            return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_selfStatistics, _referenceHierarchies);
    }

    public override string ToString()
    {
        StringBuilder treeBuilder = new StringBuilder();

        if (_selfStatistics != null)
        {
            foreach (KeyValuePair<string, List<LevelInfo>> statsByOutputName in _selfStatistics)
            {
                treeBuilder.Append(statsByOutputName.Key).Append(Environment.NewLine);

                foreach (LevelInfo levelInfo in statsByOutputName.Value)
                {
                    AppendLevelInfoTreeString(treeBuilder, levelInfo, 1);
                }
            }
        }

        foreach (KeyValuePair<string, Dictionary<string, List<LevelInfo>>> statisticsEntry in _referenceHierarchies)
        {
            treeBuilder.Append(statisticsEntry.Key).Append(Environment.NewLine);
            foreach (KeyValuePair<string, List<LevelInfo>> statisticsByType in statisticsEntry.Value)
            {
                treeBuilder.Append("    ").Append(statisticsByType.Key).Append(Environment.NewLine);

                foreach (LevelInfo levelInfo in statisticsByType.Value)
                {
                    AppendLevelInfoTreeString(treeBuilder, levelInfo, 2);
                }
            }
        }

        return treeBuilder.ToString();
    }

    private void AppendLevelInfoTreeString(StringBuilder treeBuilder, LevelInfo levelInfo, int currentLevel)
    {
        treeBuilder.Append(string.Concat(Enumerable.Repeat("    ", currentLevel)))
            .Append(levelInfo)
            .Append(Environment.NewLine);

        foreach (LevelInfo child in levelInfo.Children)
        {
            AppendLevelInfoTreeString(treeBuilder, child, currentLevel + 1);
        }
    }
}

public record LevelInfo(IEntityClassifier Entity, int? QueriedEntityCount, int? ChildrenCount, List<LevelInfo> Children)
{
    public LevelInfo(LevelInfo levelInfo, List<LevelInfo> children) : this(levelInfo.Entity,
        levelInfo.QueriedEntityCount, levelInfo.ChildrenCount, children)
    {
    }

    public override string ToString()
    {
        if (QueriedEntityCount is null && ChildrenCount is null)
        {
            return Entity.ToString()!;
        }

        return $"[{QueriedEntityCount}:{ChildrenCount} {Entity}]";
    }
};