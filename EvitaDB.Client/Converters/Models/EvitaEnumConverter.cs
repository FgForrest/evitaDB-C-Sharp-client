using EvitaDB;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Session;
using static EvitaDB.Client.Models.ExtraResults.QueryTelemetry;
using QueryTelemetry = EvitaDB.Client.Models.ExtraResults.QueryTelemetry;

namespace EvitaDB.Client.Converters.Models;

public static class EvitaEnumConverter
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

    public static OrderBehaviour ToOrderBehaviour(GrpcOrderBehaviour grpcOrderBehaviour)
    {
        return grpcOrderBehaviour switch
        {
            GrpcOrderBehaviour.NullsFirst => OrderBehaviour.NullsFirst,
            GrpcOrderBehaviour.NullsLast => OrderBehaviour.NullsLast,
            _ => throw new EvitaInternalError("Unrecognized remote order behaviour: " + grpcOrderBehaviour)
        };
    }

    public static GrpcOrderBehaviour ToGrpcOrderBehaviour(OrderBehaviour orderBehaviour)
    {
        return orderBehaviour switch
        {
            OrderBehaviour.NullsFirst => GrpcOrderBehaviour.NullsFirst,
            OrderBehaviour.NullsLast => GrpcOrderBehaviour.NullsLast,
            _ => throw new EvitaInternalError("Unrecognized order behaviour: " + orderBehaviour)
        };
    }

    public static EmptyHierarchicalEntityBehaviour ToEmptyHierarchicalEntityBehaviour(
        GrpcEmptyHierarchicalEntityBehaviour grpcEmptyHierarchicalEntityBehaviour)
    {
        return grpcEmptyHierarchicalEntityBehaviour switch
        {
            GrpcEmptyHierarchicalEntityBehaviour.LeaveEmpty => EmptyHierarchicalEntityBehaviour.LeaveEmpty,
            GrpcEmptyHierarchicalEntityBehaviour.RemoveEmpty => EmptyHierarchicalEntityBehaviour.RemoveEmpty,
            _ => throw new EvitaInternalError("Unrecognized remote empty hierarchical entity behaviour: " +
                                              grpcEmptyHierarchicalEntityBehaviour)
        };
    }

    public static GrpcEmptyHierarchicalEntityBehaviour ToGrpcEmptyHierarchicalEntityBehaviour(
        EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour)
    {
        return emptyHierarchicalEntityBehaviour switch
        {
            EmptyHierarchicalEntityBehaviour.LeaveEmpty => GrpcEmptyHierarchicalEntityBehaviour.LeaveEmpty,
            EmptyHierarchicalEntityBehaviour.RemoveEmpty => GrpcEmptyHierarchicalEntityBehaviour.RemoveEmpty,
            _ => throw new EvitaInternalError("Unrecognized empty hierarchical entity behaviour: " +
                                              emptyHierarchicalEntityBehaviour)
        };
    }

    public static StatisticsBase ToStatisticsBase(GrpcStatisticsBase grpcStatisticsBase)
    {
        return grpcStatisticsBase switch
        {
            GrpcStatisticsBase.CompleteFilter => StatisticsBase.CompleteFilter,
            GrpcStatisticsBase.WithoutUserFilter => StatisticsBase.WithoutUserFilter,
            _ => throw new EvitaInternalError("Unrecognized remote statistics base: " + grpcStatisticsBase)
        };
    }

    public static GrpcStatisticsBase ToGrpcStatisticsBase(StatisticsBase statisticsBase)
    {
        return statisticsBase switch
        {
            StatisticsBase.CompleteFilter => GrpcStatisticsBase.CompleteFilter,
            StatisticsBase.WithoutUserFilter => GrpcStatisticsBase.WithoutUserFilter,
            _ => throw new EvitaInternalError("Unrecognized statistics base: " + statisticsBase)
        };
    }

    public static StatisticsType ToStatisticsType(GrpcStatisticsType grpcStatisticsType)
    {
        return grpcStatisticsType switch
        {
            GrpcStatisticsType.ChildrenCount => StatisticsType.ChildrenCount,
            GrpcStatisticsType.QueriedEntityCount => StatisticsType.QueriedEntityCount,
            _ => throw new EvitaInternalError("Unrecognized remote statistics type: " + grpcStatisticsType)
        };
    }

    public static GrpcStatisticsType ToGrpcStatisticsType(StatisticsType statisticsType)
    {
        return statisticsType switch
        {
            StatisticsType.ChildrenCount => GrpcStatisticsType.ChildrenCount,
            StatisticsType.QueriedEntityCount => GrpcStatisticsType.QueriedEntityCount,
            _ => throw new EvitaInternalError("Unrecognized statistics type: " + statisticsType)
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

    public static CatalogEvolutionMode ToCatalogEvolutionMode(GrpcCatalogEvolutionMode grpcEvolutionMode)
    {
        return grpcEvolutionMode switch
        {
            GrpcCatalogEvolutionMode.AddingEntityTypes => CatalogEvolutionMode.AddingEntityTypes,
            _ => throw new EvitaInternalError("Unrecognized remote evolution mode: " + grpcEvolutionMode)
        };
    }
    
    public static GrpcCatalogEvolutionMode ToGrpcCatalogEvolutionMode(CatalogEvolutionMode evolutionMode)
    {
        return evolutionMode switch
        {
            CatalogEvolutionMode.AddingEntityTypes => GrpcCatalogEvolutionMode.AddingEntityTypes,
            _ => throw new ArgumentOutOfRangeException(nameof(evolutionMode), evolutionMode, null)
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

    public static QueryTelemetry.QueryPhase ToQueryPhase(GrpcQueryPhase grpcQueryPhase)
    {
        return grpcQueryPhase switch
        {
            GrpcQueryPhase.Overall => QueryTelemetry.QueryPhase.Overall,
            GrpcQueryPhase.Planning => QueryTelemetry.QueryPhase.Planning,
            GrpcQueryPhase.PlanningNestedQuery => QueryTelemetry.QueryPhase.PlanningNestedQuery,
            GrpcQueryPhase.PlanningIndexUsage => QueryTelemetry.QueryPhase.PlanningIndexUsage,
            GrpcQueryPhase.PlanningFilter => QueryTelemetry.QueryPhase.PlanningFilter,
            GrpcQueryPhase.PlanningFilterNestedQuery => QueryTelemetry.QueryPhase.PlanningFilterNestedQuery,
            GrpcQueryPhase.PlanningFilterAlternative => QueryTelemetry.QueryPhase.PlanningFilterAlternative,
            GrpcQueryPhase.PlanningSort => QueryTelemetry.QueryPhase.PlanningSort,
            GrpcQueryPhase.PlanningSortAlternative => QueryTelemetry.QueryPhase.PlanningSortAlternative,
            GrpcQueryPhase.PlanningExtraResultFabrication => QueryTelemetry.QueryPhase.PlanningExtraResultFabrication,
            GrpcQueryPhase.PlanningExtraResultFabricationAlternative => QueryTelemetry.QueryPhase
                .PlanningExtraResultFabricationAlternative,
            GrpcQueryPhase.Execution => QueryTelemetry.QueryPhase.Execution,
            GrpcQueryPhase.ExecutionPrefetch => QueryTelemetry.QueryPhase.ExecutionPrefetch,
            GrpcQueryPhase.ExecutionFilter => QueryTelemetry.QueryPhase.ExecutionFilter,
            GrpcQueryPhase.ExecutionFilterNestedQuery => QueryTelemetry.QueryPhase.ExecutionFilterNestedQuery,
            GrpcQueryPhase.ExecutionSortAndSlice => QueryTelemetry.QueryPhase.ExecutionSortAndSlice,
            GrpcQueryPhase.ExtraResultsFabrication => QueryTelemetry.QueryPhase.ExtraResultsFabrication,
            GrpcQueryPhase.ExtraResultItemFabrication => QueryTelemetry.QueryPhase.ExtraResultItemFabrication,
            GrpcQueryPhase.Fetching => QueryTelemetry.QueryPhase.Fetching,
            GrpcQueryPhase.FetchingReferences => QueryTelemetry.QueryPhase.FetchingReferences,
            GrpcQueryPhase.FetchingParents => QueryTelemetry.QueryPhase.FetchingParents,
            _ => throw new EvitaInternalError("Unrecognized remote query phase: " + grpcQueryPhase)
        };
    }

    public static GrpcQueryPhase ToGrpcQueryPhase(QueryTelemetry.QueryPhase queryPhase)
    {
        return queryPhase switch
        {
            QueryTelemetry.QueryPhase.Overall => GrpcQueryPhase.Overall,
            QueryTelemetry.QueryPhase.Planning => GrpcQueryPhase.Planning,
            QueryTelemetry.QueryPhase.PlanningNestedQuery => GrpcQueryPhase.PlanningNestedQuery,
            QueryTelemetry.QueryPhase.PlanningIndexUsage => GrpcQueryPhase.PlanningIndexUsage,
            QueryTelemetry.QueryPhase.PlanningFilter => GrpcQueryPhase.PlanningFilter,
            QueryTelemetry.QueryPhase.PlanningFilterNestedQuery => GrpcQueryPhase.PlanningFilterNestedQuery,
            QueryTelemetry.QueryPhase.PlanningFilterAlternative => GrpcQueryPhase.PlanningFilterAlternative,
            QueryTelemetry.QueryPhase.PlanningSort => GrpcQueryPhase.PlanningSort,
            QueryTelemetry.QueryPhase.PlanningSortAlternative => GrpcQueryPhase.PlanningSortAlternative,
            QueryTelemetry.QueryPhase.PlanningExtraResultFabrication => GrpcQueryPhase.PlanningExtraResultFabrication,
            QueryTelemetry.QueryPhase.PlanningExtraResultFabricationAlternative => GrpcQueryPhase
                .PlanningExtraResultFabricationAlternative,
            QueryTelemetry.QueryPhase.Execution => GrpcQueryPhase.Execution,
            QueryTelemetry.QueryPhase.ExecutionPrefetch => GrpcQueryPhase.ExecutionPrefetch,
            QueryTelemetry.QueryPhase.ExecutionFilter => GrpcQueryPhase.ExecutionFilter,
            QueryTelemetry.QueryPhase.ExecutionFilterNestedQuery => GrpcQueryPhase.ExecutionFilterNestedQuery,
            QueryTelemetry.QueryPhase.ExecutionSortAndSlice => GrpcQueryPhase.ExecutionSortAndSlice,
            QueryTelemetry.QueryPhase.ExtraResultsFabrication => GrpcQueryPhase.ExtraResultsFabrication,
            QueryTelemetry.QueryPhase.ExtraResultItemFabrication => GrpcQueryPhase.ExtraResultItemFabrication,
            QueryTelemetry.QueryPhase.Fetching => GrpcQueryPhase.Fetching,
            QueryTelemetry.QueryPhase.FetchingReferences => GrpcQueryPhase.FetchingReferences,
            QueryTelemetry.QueryPhase.FetchingParents => GrpcQueryPhase.FetchingParents,
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

    public static CaptureContent ToCaptureContent(GrpcCaptureContent grpcCaptureContent)
    {
        return grpcCaptureContent switch
        {
            GrpcCaptureContent.Header => CaptureContent.Header,
            GrpcCaptureContent.Body => CaptureContent.Body,
            _ => throw new EvitaInternalError("Unrecognized remote capture content: " + grpcCaptureContent)
        };
    }

    public static GrpcCaptureContent ToGrpcCaptureContent(CaptureContent captureContent)
    {
        return captureContent switch
        {
            CaptureContent.Header => GrpcCaptureContent.Header,
            CaptureContent.Body => GrpcCaptureContent.Body
        };
    }

    public static Operation ToOperation(GrpcOperation grpcOperation)
    {
        return grpcOperation switch
        {
            GrpcOperation.Create => Operation.Create,
            GrpcOperation.Update => Operation.Update,
            GrpcOperation.Remove => Operation.Remove,
            _ => throw new EvitaInternalError("Unrecognized remote operation: " + grpcOperation)
        };
    }

    public static GrpcOperation ToGrpcOperation(Operation operation)
    {
        return operation switch
        {
            Operation.Create => GrpcOperation.Create,
            Operation.Update => GrpcOperation.Update,
            Operation.Remove => GrpcOperation.Remove
        };
    }
}