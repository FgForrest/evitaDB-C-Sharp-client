using Client.Exceptions;
using Client.Models.Data;
using Client.Models.Data.Mutations;
using Client.Models.Schemas;
using Client.Queries.Filter;
using Client.Queries.Order;
using Client.Queries.Requires;
using Client.Session;
using EvitaDB;
using static Client.Models.ExtraResults.QueryTelemetry;

namespace Client.Converters.Models;

public class EvitaEnumConverter
{
    public static CatalogState ToCatalogState(GrpcCatalogState grpcCatalogState)
    {
        return grpcCatalogState switch
        {
            GrpcCatalogState.WarmingUp => CatalogState.WarmingUp,
            GrpcCatalogState.Alive => CatalogState.Alive,
            _ => throw new EvitaInternalError("Unrecognized remote catalog state: " + grpcCatalogState)
        };
    }


    public static GrpcCatalogState ToGrpcCatalogState(CatalogState catalogState)
    {
        return catalogState switch
        {
            CatalogState.WarmingUp => GrpcCatalogState.WarmingUp,
            CatalogState.Alive => GrpcCatalogState.Alive,
            _ => throw new EvitaInternalError("Unrecognized local catalog state: " + catalogState)
        };
    }

    public static QueryPriceMode ToQueryPriceMode(GrpcQueryPriceMode grpcQueryPriceMode)
    {
        return grpcQueryPriceMode switch
        {
            GrpcQueryPriceMode.WithTax => QueryPriceMode.WithTax,
            GrpcQueryPriceMode.WithoutTax => QueryPriceMode.WithoutTax,
            _ => throw new EvitaInternalError("Unrecognized remote query price mode: " + grpcQueryPriceMode)
        };
    }

    public static GrpcQueryPriceMode ToGrpcQueryPriceMode(QueryPriceMode queryPriceMode)
    {
        return queryPriceMode switch
        {
            QueryPriceMode.WithTax => GrpcQueryPriceMode.WithTax,
            QueryPriceMode.WithoutTax => GrpcQueryPriceMode.WithoutTax,
            _ => throw new EvitaInternalError("Unrecognized local query price mode: " + queryPriceMode)
        };
    }

    public static PriceContentMode ToPriceContentMode(GrpcPriceContentMode grpcPriceContentMode)
    {
        return grpcPriceContentMode switch
        {
            GrpcPriceContentMode.FetchNone => PriceContentMode.None,
            GrpcPriceContentMode.RespectingFilter => PriceContentMode.RespectingFilter,
            GrpcPriceContentMode.All => PriceContentMode.All,
            _ => throw new EvitaInternalError("Unrecognized remote price content mode: " + grpcPriceContentMode)
        };
    }

    public static GrpcPriceContentMode ToGrpcPriceContentMode(PriceContentMode priceContentMode)
    {
        return priceContentMode switch
        {
            PriceContentMode.None => GrpcPriceContentMode.FetchNone,
            PriceContentMode.RespectingFilter => GrpcPriceContentMode.RespectingFilter,
            PriceContentMode.All => GrpcPriceContentMode.All,
            _ => throw new EvitaInternalError("Unrecognized local price content mode: " + priceContentMode)
        };
    }

    public static OrderDirection ToOrderDirection(GrpcOrderDirection grpcOrderDirection)
    {
        return grpcOrderDirection switch
        {
            GrpcOrderDirection.Asc => OrderDirection.Asc,
            GrpcOrderDirection.Desc => OrderDirection.Desc,
            _ => throw new EvitaInternalError("Unrecognized remote order direction: " + grpcOrderDirection)
        };
    }

    public static GrpcOrderDirection ToGrpcOrderDirection(OrderDirection orderDirection)
    {
        return orderDirection switch
        {
            OrderDirection.Asc => GrpcOrderDirection.Asc,
            OrderDirection.Desc => GrpcOrderDirection.Desc,
            _ => throw new EvitaInternalError("Unrecognized order direction: " + orderDirection)
        };
    }

    public static AttributeSpecialValue ToAttributeSpecialValue(GrpcAttributeSpecialValue grpcAttributeSpecialValue)
    {
        return grpcAttributeSpecialValue switch
        {
            GrpcAttributeSpecialValue.Null => AttributeSpecialValue.Null,
            GrpcAttributeSpecialValue.NotNull => AttributeSpecialValue.NotNull,
            _ => throw new EvitaInternalError("Unrecognized remote attribute special value: " +
                                              grpcAttributeSpecialValue)
        };
    }

    public static GrpcAttributeSpecialValue ToGrpcAttributeSpecialValue(AttributeSpecialValue attributeSpecialValue)
    {
        return attributeSpecialValue switch
        {
            AttributeSpecialValue.Null => GrpcAttributeSpecialValue.Null,
            AttributeSpecialValue.NotNull => GrpcAttributeSpecialValue.NotNull,
            _ => throw new EvitaInternalError("Unrecognized attribute special value: " + attributeSpecialValue)
        };
    }

    public static FacetStatisticsDepth ToFacetStatisticsDepth(GrpcFacetStatisticsDepth grpcFacetStatisticsDepth)
    {
        return grpcFacetStatisticsDepth switch
        {
            GrpcFacetStatisticsDepth.Counts => FacetStatisticsDepth.Counts,
            GrpcFacetStatisticsDepth.Impact => FacetStatisticsDepth.Impact,
            _ => throw new EvitaInternalError("Unrecognized remote facet statistics depth: " + grpcFacetStatisticsDepth)
        };
    }

    public static GrpcFacetStatisticsDepth ToGrpcFacetStatisticsDepth(FacetStatisticsDepth facetStatisticsDepth)
    {
        return facetStatisticsDepth switch
        {
            FacetStatisticsDepth.Counts => GrpcFacetStatisticsDepth.Counts,
            FacetStatisticsDepth.Impact => GrpcFacetStatisticsDepth.Impact,
            _ => throw new EvitaInternalError("Unrecognized facet statistics depth: " + facetStatisticsDepth)
        };
    }

    public static PriceInnerRecordHandling ToPriceInnerRecordHandling(
        GrpcPriceInnerRecordHandling grpcPriceInnerRecordHandling)
    {
        return grpcPriceInnerRecordHandling switch
        {
            GrpcPriceInnerRecordHandling.None => PriceInnerRecordHandling.None,
            GrpcPriceInnerRecordHandling.FirstOccurrence => PriceInnerRecordHandling.FirstOccurrence,
            GrpcPriceInnerRecordHandling.Sum => PriceInnerRecordHandling.Sum,
            GrpcPriceInnerRecordHandling.Unknown => PriceInnerRecordHandling.Unknown,
            _ => throw new EvitaInternalError(
                $"Unrecognized remote price inner record handling: {grpcPriceInnerRecordHandling}")
        };
    }

    public static GrpcPriceInnerRecordHandling ToGrpcPriceInnerRecordHandling(
        PriceInnerRecordHandling priceInnerRecordHandling)
    {
        return priceInnerRecordHandling switch
        {
            PriceInnerRecordHandling.None => GrpcPriceInnerRecordHandling.None,
            PriceInnerRecordHandling.FirstOccurrence => GrpcPriceInnerRecordHandling.FirstOccurrence,
            PriceInnerRecordHandling.Sum => GrpcPriceInnerRecordHandling.Sum,
            PriceInnerRecordHandling.Unknown => GrpcPriceInnerRecordHandling.Unknown,
            _ => throw new ArgumentOutOfRangeException(nameof(priceInnerRecordHandling), priceInnerRecordHandling, null)
        };
    }

    public static Cardinality? ToCardinality(GrpcCardinality grpcCardinality)
    {
        return grpcCardinality switch
        {
            GrpcCardinality.NotSpecified => null,
            GrpcCardinality.ZeroOrOne => Cardinality.ZeroOrOne,
            GrpcCardinality.ExactlyOne => Cardinality.ExactlyOne,
            GrpcCardinality.ZeroOrMore => Cardinality.ZeroOrMore,
            GrpcCardinality.OneOrMore => Cardinality.OneOrMore,
            _ => throw new EvitaInternalError("Unrecognized remote cardinality: " + grpcCardinality)
        };
    }

    public static GrpcCardinality ToGrpcCardinality(Cardinality? cardinality)
    {
        if (cardinality == null)
        {
            return GrpcCardinality.NotSpecified;
        }

        return cardinality switch
        {
            Cardinality.ZeroOrOne => GrpcCardinality.ZeroOrOne,
            Cardinality.ExactlyOne => GrpcCardinality.ExactlyOne,
            Cardinality.ZeroOrMore => GrpcCardinality.ZeroOrMore,
            Cardinality.OneOrMore => GrpcCardinality.OneOrMore,
            _ => throw new ArgumentOutOfRangeException(nameof(cardinality), cardinality, null)
        };
    }

    public static EvolutionMode ToEvolutionMode(GrpcEvolutionMode grpcEvolutionMode)
    {
        return grpcEvolutionMode switch
        {
            GrpcEvolutionMode.AdaptPrimaryKeyGeneration => EvolutionMode.AdaptPrimaryKeyGeneration,
            GrpcEvolutionMode.AddingAttributes => EvolutionMode.AddingAttributes,
            GrpcEvolutionMode.AddingAssociatedData => EvolutionMode.AddingAssociatedData,
            GrpcEvolutionMode.AddingReferences => EvolutionMode.AddingReferences,
            GrpcEvolutionMode.AddingPrices => EvolutionMode.AddingPrices,
            GrpcEvolutionMode.AddingLocales => EvolutionMode.AddingLocales,
            GrpcEvolutionMode.AddingCurrencies => EvolutionMode.AddingCurrencies,
            GrpcEvolutionMode.AddingHierarchy => EvolutionMode.AddingHierarchy,
            _ => throw new EvitaInternalError("Unrecognized remote evolution mode: " + grpcEvolutionMode)
        };
    }

    public static GrpcEvolutionMode ToGrpcEvolutionMode(EvolutionMode evolutionMode)
    {
        return evolutionMode switch
        {
            EvolutionMode.AdaptPrimaryKeyGeneration => GrpcEvolutionMode.AdaptPrimaryKeyGeneration,
            EvolutionMode.AddingAttributes => GrpcEvolutionMode.AddingAttributes,
            EvolutionMode.AddingAssociatedData => GrpcEvolutionMode.AddingAssociatedData,
            EvolutionMode.AddingReferences => GrpcEvolutionMode.AddingReferences,
            EvolutionMode.AddingPrices => GrpcEvolutionMode.AddingPrices,
            EvolutionMode.AddingLocales => GrpcEvolutionMode.AddingLocales,
            EvolutionMode.AddingCurrencies => GrpcEvolutionMode.AddingCurrencies,
            EvolutionMode.AddingHierarchy => GrpcEvolutionMode.AddingHierarchy,
            _ => throw new ArgumentOutOfRangeException(nameof(evolutionMode), evolutionMode, null)
        };
    }

    public static QueryPhase ToQueryPhase(GrpcQueryPhase grpcQueryPhase)
    {
        return grpcQueryPhase switch
        {
            GrpcQueryPhase.Overall => QueryPhase.Overall,
            GrpcQueryPhase.Planning => QueryPhase.Planning,
            GrpcQueryPhase.PlanningNestedQuery => QueryPhase.PlanningNestedQuery,
            GrpcQueryPhase.PlanningIndexUsage => QueryPhase.PlanningIndexUsage,
            GrpcQueryPhase.PlanningFilter => QueryPhase.PlanningFilter,
            GrpcQueryPhase.PlanningFilterNestedQuery => QueryPhase.PlanningFilterNestedQuery,
            GrpcQueryPhase.PlanningFilterAlternative => QueryPhase.PlanningFilterAlternative,
            GrpcQueryPhase.PlanningSort => QueryPhase.PlanningSort,
            GrpcQueryPhase.PlanningSortAlternative => QueryPhase.PlanningSortAlternative,
            GrpcQueryPhase.PlanningExtraResultFabrication => QueryPhase.PlanningExtraResultFabrication,
            GrpcQueryPhase.PlanningExtraResultFabricationAlternative => QueryPhase
                .PlanningExtraResultFabricationAlternative,
            GrpcQueryPhase.Execution => QueryPhase.Execution,
            GrpcQueryPhase.ExecutionPrefetch => QueryPhase.ExecutionPrefetch,
            GrpcQueryPhase.ExecutionFilter => QueryPhase.ExecutionFilter,
            GrpcQueryPhase.ExecutionSortAndSlice => QueryPhase.ExecutionSortAndSlice,
            GrpcQueryPhase.ExtraResultsFabrication => QueryPhase.ExtraResultsFabrication,
            GrpcQueryPhase.ExtraResultItemFabrication => QueryPhase.ExtraResultItemFabrication,
            GrpcQueryPhase.Fetching => QueryPhase.Fetching,
            GrpcQueryPhase.FetchingReferences => QueryPhase.FetchingReferences,
            _ => throw new EvitaInternalError("Unrecognized remote query phase: " + grpcQueryPhase)
        };
    }

    public static GrpcQueryPhase ToGrpcQueryPhase(QueryPhase queryPhase)
    {
        return queryPhase switch
        {
            QueryPhase.Overall => GrpcQueryPhase.Overall,
            QueryPhase.Planning => GrpcQueryPhase.Planning,
            QueryPhase.PlanningNestedQuery => GrpcQueryPhase.PlanningNestedQuery,
            QueryPhase.PlanningIndexUsage => GrpcQueryPhase.PlanningIndexUsage,
            QueryPhase.PlanningFilter => GrpcQueryPhase.PlanningFilter,
            QueryPhase.PlanningFilterNestedQuery => GrpcQueryPhase.PlanningFilterNestedQuery,
            QueryPhase.PlanningFilterAlternative => GrpcQueryPhase.PlanningFilterAlternative,
            QueryPhase.PlanningSort => GrpcQueryPhase.PlanningSort,
            QueryPhase.PlanningSortAlternative => GrpcQueryPhase.PlanningSortAlternative,
            QueryPhase.PlanningExtraResultFabrication => GrpcQueryPhase.PlanningExtraResultFabrication,
            QueryPhase.PlanningExtraResultFabricationAlternative => GrpcQueryPhase
                .PlanningExtraResultFabricationAlternative,
            QueryPhase.Execution => GrpcQueryPhase.Execution,
            QueryPhase.ExecutionPrefetch => GrpcQueryPhase.ExecutionPrefetch,
            QueryPhase.ExecutionFilter => GrpcQueryPhase.ExecutionFilter,
            QueryPhase.ExecutionSortAndSlice => GrpcQueryPhase.ExecutionSortAndSlice,
            QueryPhase.ExtraResultsFabrication => GrpcQueryPhase.ExtraResultsFabrication,
            QueryPhase.ExtraResultItemFabrication => GrpcQueryPhase.ExtraResultItemFabrication,
            QueryPhase.Fetching => GrpcQueryPhase.Fetching,
            QueryPhase.FetchingReferences => GrpcQueryPhase.FetchingReferences,
            _ => throw new EvitaInternalError("Unrecognized local query phase: " + queryPhase)
        };
    }

    public static EntityExistence ToEntityExistence(GrpcEntityExistence grpcEntityExistence)
    {
        return grpcEntityExistence switch
        {
            GrpcEntityExistence.MayExist => EntityExistence.MayExist,
            GrpcEntityExistence.MustNotExist => EntityExistence.MustNotExist,
            GrpcEntityExistence.MustExist => EntityExistence.MustExist,
            _ => throw new EvitaInternalError("Unrecognized remote entity existence: " + grpcEntityExistence)
        };
    }

    public static GrpcEntityExistence ToGrpcEntityExistence(EntityExistence entityExistence)
    {
        return entityExistence switch
        {
            EntityExistence.MayExist => GrpcEntityExistence.MayExist,
            EntityExistence.MustNotExist => GrpcEntityExistence.MustNotExist,
            EntityExistence.MustExist => GrpcEntityExistence.MustExist,
            _ => throw new EvitaInternalError("Unrecognized local entity existence: " + entityExistence)
        };
    }
}