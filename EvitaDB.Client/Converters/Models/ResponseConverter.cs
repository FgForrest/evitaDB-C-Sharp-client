using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Converters.Models.Data;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;
using AttributeHistogram = EvitaDB.Client.Models.ExtraResults.AttributeHistogram;
using FacetSummary = EvitaDB.Client.Queries.Requires.FacetSummary;
using PriceHistogram = EvitaDB.Client.Models.ExtraResults.PriceHistogram;
using QueryTelemetry = EvitaDB.Client.Models.ExtraResults.QueryTelemetry;

namespace EvitaDB.Client.Converters.Models;

public static class ResponseConverter
{
    public static IDataChunk<T> ConvertToDataChunk<T>(GrpcQueryResponse grpcResponse,
        Func<GrpcDataChunk, IList<T>> converter)
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

    public static IEvitaResponseExtraResult[] ToExtraResults(
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaFetcher,
        EvitaRequest evitaRequest, 
        GrpcExtraResults? extraResults)
    {
        Query query = evitaRequest.Query;
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
            extraResultList.Add(
                new Hierarchy(
                    extraResults.SelfHierarchy is not null
                        ? ToHierarchy(
                            entitySchemaFetcher,
                            evitaRequest,
                            hierarchyConstraints[0],
                            extraResults.SelfHierarchy
                        )
                        : null,
                    extraResults.Hierarchy.ToDictionary(
                        x => x.Key,
                        x => ToHierarchy(
                            entitySchemaFetcher,
                            evitaRequest,
                            hierarchyConstraints
                                .OfType<HierarchyOfReference>()
                                .First(it => it.ReferenceNames.Any(name => name == x.Key)), 
                            x.Value)
                    )
                )
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

                    return ToFacetGroupStatistics(entitySchemaFetcher, evitaRequest, entityFetch, entityGroupFetch, it);
                }).ToList()
            ));
        }

        return extraResultList.ToArray();
    }

    private static FacetGroupStatistics ToFacetGroupStatistics(
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaFetcher,
        EvitaRequest evitaRequest,
        EntityFetch? entityFetch,
        EntityGroupFetch? entityGroupFetch,
        GrpcFacetGroupStatistics grpcFacetGroupStatistics
    )
    {
        return new FacetGroupStatistics(
            grpcFacetGroupStatistics.ReferenceName,
            grpcFacetGroupStatistics.GroupEntity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(entitySchemaFetcher, grpcFacetGroupStatistics.GroupEntity, evitaRequest)
                : EntityConverter.ToEntityReference(grpcFacetGroupStatistics.GroupEntityReference),
            grpcFacetGroupStatistics.Count,
            grpcFacetGroupStatistics.FacetStatistics
                .Select(x => ToFacetStatistics(entitySchemaFetcher, evitaRequest, entityFetch, x))
                .ToDictionary(x => x.FacetEntity.PrimaryKey!.Value, x => x)
        );
    }

    private static FacetStatistics ToFacetStatistics(
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaFetcher,
        EvitaRequest evitaRequest,
        EntityFetch? entityFetch,
        GrpcFacetStatistics grpcFacetStatistics
    )
    {
        return new FacetStatistics(
            grpcFacetStatistics.FacetEntity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(
                    entitySchemaFetcher,
                    grpcFacetStatistics.FacetEntity,
                    evitaRequest
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
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaFetcher,
        EvitaRequest evitaRequest,
        IRootHierarchyConstraint rootHierarchyConstraint,
        GrpcHierarchy grpcHierarchy
    )
    {
        return grpcHierarchy
            .Hierarchy
            .ToDictionary(x => x.Key, x =>
            {
                IConstraint? hierarchyConstraint = QueryUtils.FindConstraint<IConstraint>(
                    rootHierarchyConstraint,
                    cnt => cnt is IHierarchyRequireConstraint hrc && x.Key == hrc.OutputName
                );
                EntityFetch? entityFetch = QueryUtils.FindConstraint<EntityFetch>(hierarchyConstraint);
                return x.Value.LevelInfos.Select(y => ToLevelInfo(entitySchemaFetcher, evitaRequest, entityFetch, y)).ToList();
            });
    }

    private static LevelInfo ToLevelInfo(
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaFetcher,
        EvitaRequest evitaRequest,
        EntityFetch? entityFetch,
        GrpcLevelInfo grpcLevelInfo
    )
    {
        return new LevelInfo(
            grpcLevelInfo.Entity is not null
                ? EntityConverter.ToEntity<ISealedEntity>(entitySchemaFetcher, grpcLevelInfo.Entity, evitaRequest)
                : EntityConverter.ToEntityReference(grpcLevelInfo.EntityReference),
            grpcLevelInfo.QueriedEntityCount,
            grpcLevelInfo.ChildrenCount,
            grpcLevelInfo.Items.Select(it => ToLevelInfo(entitySchemaFetcher, evitaRequest, entityFetch, it)).ToList()
        );
    }

    private static QueryTelemetry ToQueryTelemetry(GrpcQueryTelemetry grpcQueryTelemetry)
    {
        return new QueryTelemetry(
            EvitaEnumConverter.ToQueryPhase(grpcQueryTelemetry.Operation),
            grpcQueryTelemetry.Start,
            grpcQueryTelemetry.SpentTime,
            grpcQueryTelemetry.Steps.Select(ToQueryTelemetry).ToList(),
            grpcQueryTelemetry.Arguments.Select(x => x.ToString()).ToArray()
        );
    }

    private static IHistogram ToHistogram(GrpcHistogram grpcHistogram)
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