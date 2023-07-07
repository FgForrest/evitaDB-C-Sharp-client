using System.Runtime.InteropServices.JavaScript;
using Client.Converters.DataTypes;
using Client.Converters.Models.Data;
using Client.DataTypes;
using Client.Exceptions;
using Client.Models;
using Client.Models.ExtraResults;
using Client.Models.Schemas.Dtos;
using Client.Queries;
using Client.Queries.Requires;
using Client.Utils;
using EvitaDB;
using AttributeHistogram = Client.Models.ExtraResults.AttributeHistogram;
using FacetSummary = Client.Queries.Requires.FacetSummary;
using PriceHistogram = Client.Models.ExtraResults.PriceHistogram;
using QueryTelemetry = Client.Models.ExtraResults.QueryTelemetry;

namespace Client.Converters.Models;

public static class ResponseConverter
{
    public static IDataChunk<T> ConvertToDataChunk<T>(GrpcQueryResponse grpcResponse,
        Func<GrpcDataChunk, List<T>> converter)
    {
        GrpcDataChunk grpcRecordPage = grpcResponse.RecordPage;
        if (grpcRecordPage.ChunkCase == GrpcDataChunk.ChunkOneofCase.PaginatedList)
        {
            GrpcPaginatedList grpcPaginatedList = grpcRecordPage.PaginatedList;
            return new PaginatedList<T>(
                grpcPaginatedList.PageNumber,
                grpcPaginatedList.PageSize,
                grpcRecordPage.TotalRecordCount,
                converter.Invoke(grpcRecordPage)
            );
        }

        if (grpcRecordPage.ChunkCase == GrpcDataChunk.ChunkOneofCase.StripList)
        {
            GrpcStripList grpcStripList = grpcRecordPage.StripList;
            return new StripList<T>(
                grpcStripList.Offset,
                grpcStripList.Limit,
                grpcRecordPage.TotalRecordCount,
                converter.Invoke(grpcRecordPage)
            );
        }

        throw new EvitaInternalError(
            "Only PaginatedList or StripList expected, but got none!"
        );
    }

    public static IEvitaResponseExtraResult[] ToExtraResults(Func<GrpcSealedEntity, EntitySchema> entitySchemaFetcher,
        Query query, GrpcExtraResults? extraResults)
    {
        if (extraResults is null)
            return Array.Empty<IEvitaResponseExtraResult>();
        List<IEvitaResponseExtraResult> extraResultList = new List<IEvitaResponseExtraResult>();
        GrpcQueryTelemetry grpcQueryTelemetry = extraResults.QueryTelemetry;
        if (grpcQueryTelemetry is not null)
        {
            extraResultList.Add(ToQueryTelemetry(grpcQueryTelemetry));
        }

        GrpcHistogram grpcPriceHistogram = extraResults.PriceHistogram;
        if (grpcPriceHistogram is not null)
        {
            extraResultList.Add(new PriceHistogram(ToHistogram(grpcPriceHistogram)));
        }

        if (extraResults.AttributeHistogram.Count > 0)
        {
            extraResultList.Add(
                new AttributeHistogram(
                    extraResults.AttributeHistogram.ToDictionary(x =>
                        x.Key, value => ToHistogram(value.Value))
                )
            );
        }

        if (extraResults.SelfHierarchy is not null || extraResults.Hierarchy.Count > 0)
        {
            List<IRootHierarchyConstraint> hierarchyConstraints =
                QueryUtils.FindRequires<IRootHierarchyConstraint>(query);
            extraResultList.Add(new Hierarchy(
                extraResults.SelfHierarchy is not null
                    ? ToHierarchy(
                        entitySchemaFetcher,
                        hierarchyConstraints[0],
                        extraResults.SelfHierarchy
                    )
                    : null,
                extraResults.Hierarchy.ToDictionary(
                    x => x.Key,
                    x => ToHierarchy(entitySchemaFetcher,
                        hierarchyConstraints
                            .OfType<HierarchyOfReference>()
                            .First(it => it.ReferenceNames.Any(name => name == x.Key)), x.Value)))
            );
        }

        if (extraResults.FacetGroupStatistics.Count > 0)
        {
            FacetSummary? facetSummaryRequirementsDefaults = QueryUtils.FindRequire<FacetSummary>(query);
            EntityFetch? defaultEntityFetch = facetSummaryRequirementsDefaults?.FacetEntityRequirement;
            EntityGroupFetch? defaultEntityGroupFetch = facetSummaryRequirementsDefaults?.GroupEntityRequirement;

            Dictionary<string, FacetSummaryOfReference> facetSummaryRequestIndex =
                QueryUtils.FindRequires<FacetSummaryOfReference>(query)
                    .ToDictionary(x => x.ReferenceName, x => x);

            extraResultList.Add(new Client.Models.ExtraResults.FacetSummary(
                extraResults.FacetGroupStatistics.Select(it =>
                {
                    string referenceName = it.ReferenceName;
                    EntityFetch? entityFetch;
                    EntityGroupFetch? entityGroupFetch;
                    if (facetSummaryRequestIndex.TryGetValue(referenceName,
                            out FacetSummaryOfReference? facetSummaryOfReference))
                    {
                        entityFetch = facetSummaryOfReference.FacetEntityRequirement ?? defaultEntityFetch;
                        entityGroupFetch = facetSummaryOfReference.GroupEntityRequirement ?? defaultEntityGroupFetch;
                    }
                    else
                    {
                        entityFetch = defaultEntityFetch;
                        entityGroupFetch = defaultEntityGroupFetch;
                    }

                    return ToFacetGroupStatistics(entitySchemaFetcher, entityFetch, entityGroupFetch, it);
                }).ToList()
            ));
        }

        return extraResultList.ToArray();
    }

    private static FacetGroupStatistics ToFacetGroupStatistics(
        Func<GrpcSealedEntity, EntitySchema> entitySchemaFetcher,
        EntityFetch? entityFetch,
        EntityGroupFetch? entityGroupFetch,
        GrpcFacetGroupStatistics grpcFacetGroupStatistics
    )
    {
        List<FacetStatistics> facetStatistics = grpcFacetGroupStatistics.FacetStatistics
            .Select(x => ToFacetStatistics(entitySchemaFetcher, entityFetch, x)).ToList();
        return new FacetGroupStatistics(
            grpcFacetGroupStatistics.ReferenceName,
            grpcFacetGroupStatistics.GroupEntity is not null
                ? EntityConverter.ToSealedEntity(entitySchemaFetcher, grpcFacetGroupStatistics.GroupEntity)
                : EntityConverter.ToEntityReference(grpcFacetGroupStatistics.GroupEntityReference),
            grpcFacetGroupStatistics.Count,
            facetStatistics
                .Where(x => x.FacetEntity.PrimaryKey.HasValue)
                .ToDictionary(x => x.FacetEntity.PrimaryKey!.Value, x => x),
            facetStatistics
                .Where(x => !x.FacetEntity.PrimaryKey.HasValue)
                .ToList()
        );
    }

    private static FacetStatistics ToFacetStatistics(
        Func<GrpcSealedEntity, EntitySchema> entitySchemaFetcher,
        EntityFetch? entityFetch,
        GrpcFacetStatistics grpcFacetStatistics
    )
    {
        return new FacetStatistics(
            grpcFacetStatistics.FacetEntity is not null
                ? EntityConverter.ToSealedEntity(
                    entitySchemaFetcher,
                    grpcFacetStatistics.FacetEntity
                )
                : EntityConverter.ToEntityReference(grpcFacetStatistics.FacetEntityReference),
            grpcFacetStatistics.Requested,
            grpcFacetStatistics.Count,
            grpcFacetStatistics is {Impact: not null, MatchCount: not null}
                ? new RequestImpact(
                    grpcFacetStatistics.Impact.Value,
                    grpcFacetStatistics.MatchCount.Value
                )
                : null
        );
    }

    private static Dictionary<string, List<LevelInfo>> ToHierarchy(
        Func<GrpcSealedEntity, EntitySchema> entitySchemaFetcher,
        IRootHierarchyConstraint rootHierarchyConstraint,
        GrpcHierarchy grpcHierarchy
    )
    {
        return grpcHierarchy
            .Hierarchy
            .ToDictionary(x => x.Key, x =>
            {
                IConstraint? hierarchyConstraint = QueryUtils.FindConstraint<IRootHierarchyConstraint>(
                    rootHierarchyConstraint,
                    cnt => cnt is IHierarchyRequireConstraint hrc && x.Key == hrc.OutputName
                );
                EntityFetch? entityFetch = QueryUtils.FindConstraint<EntityFetch>(hierarchyConstraint);
                return x.Value.LevelInfos.Select(y => ToLevelInfo(entitySchemaFetcher, entityFetch, y)).ToList();
            });
    }

    private static LevelInfo ToLevelInfo(
        Func<GrpcSealedEntity, EntitySchema> entitySchemaFetcher,
        EntityFetch? entityFetch,
        GrpcLevelInfo grpcLevelInfo
    )
    {
        return new LevelInfo(
            grpcLevelInfo.Entity is not null
                ? EntityConverter.ToSealedEntity(entitySchemaFetcher, grpcLevelInfo.Entity)
                : EntityConverter.ToEntityReference(grpcLevelInfo.EntityReference),
            grpcLevelInfo.QueriedEntityCount,
            grpcLevelInfo.ChildrenCount,
            grpcLevelInfo.Items.Select(it => ToLevelInfo(entitySchemaFetcher, entityFetch, it)).ToList()
        );
    }

    private static QueryTelemetry ToQueryTelemetry(GrpcQueryTelemetry grpcQueryTelemetry)
    {
        List<QueryTelemetry> queryTelemetrySteps = new List<QueryTelemetry>();

        foreach (var step in grpcQueryTelemetry.Steps)
        {
            queryTelemetrySteps.AddRange(ToQueryTelemetrySteps(step));
        }

        return new QueryTelemetry(Enum.Parse<QueryTelemetry.QueryPhase>(grpcQueryTelemetry.Operation.ToString()),
            grpcQueryTelemetry.Start, grpcQueryTelemetry.SpentTime, queryTelemetrySteps,
            grpcQueryTelemetry.Arguments.Select(x => x.ToString()).ToArray());
    }

    private static List<QueryTelemetry> ToQueryTelemetrySteps(GrpcQueryTelemetry grpcQueryTelemetry)
    {
        List<QueryTelemetry> children = new List<QueryTelemetry>();
        List<QueryTelemetry> steps = new List<QueryTelemetry>();
        if (grpcQueryTelemetry.Steps.Any())
        {
            foreach (var step in grpcQueryTelemetry.Steps)
            {
                children.AddRange(ToQueryTelemetrySteps(step));
            }
        }

        steps.Add(new QueryTelemetry(Enum.Parse<QueryTelemetry.QueryPhase>(grpcQueryTelemetry.Operation.ToString()),
            grpcQueryTelemetry.Start, grpcQueryTelemetry.SpentTime, children,
            grpcQueryTelemetry.Arguments.Select(x => x.ToString()).ToArray()));
        return steps;
    }

    private static IHistogramContract ToHistogram(GrpcHistogram grpcHistogram)
    {
        return new Histogram(
            grpcHistogram.Buckets
                .Where(x => x is not null)
                .Select(ToBucket)
                .ToArray(),
            EvitaDataTypesConverter.ToDecimal(grpcHistogram.Max)
        );
    }

    private static Bucket ToBucket(GrpcHistogram.Types.GrpcBucket grpcBucket)
    {
        return new Bucket(
            grpcBucket.Index,
            EvitaDataTypesConverter.ToDecimal(grpcBucket.Threshold),
            grpcBucket.Occurrences
        );
    }
}