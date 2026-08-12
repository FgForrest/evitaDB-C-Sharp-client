using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations.Conflicts;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using static EvitaDB.Client.Models.ExtraResults.QueryTelemetry;

namespace EvitaDB.Client.Converters.Models;

public static class EvitaEnumConverter
{
    public static CatalogState ToCatalogState(GrpcCatalogState grpcCatalogState)
    {
        return grpcCatalogState switch
        {
            GrpcCatalogState.WarmingUp => CatalogState.WarmingUp,
            GrpcCatalogState.Alive => CatalogState.Alive,
            GrpcCatalogState.UnknownCatalogState => CatalogState.UnknownCatalogState,
            GrpcCatalogState.Corrupted => CatalogState.Corrupted,
            GrpcCatalogState.Inactive => CatalogState.Inactive,
            GrpcCatalogState.GoingAlive => CatalogState.GoingAlive,
            GrpcCatalogState.BeingActivated => CatalogState.BeingActivated,
            GrpcCatalogState.BeingDeactivated => CatalogState.BeingDeactivated,
            GrpcCatalogState.BeingCreated => CatalogState.BeingCreated,
            GrpcCatalogState.BeingDeleted => CatalogState.BeingDeleted,
            GrpcCatalogState.Missing => CatalogState.Missing,
            GrpcCatalogState.OutOfDate => CatalogState.OutOfDate,
            GrpcCatalogState.BeingUpgraded => CatalogState.BeingUpgraded,
            _ => throw new EvitaInternalError("Unrecognized remote catalog state: " + grpcCatalogState)
        };
    }


    public static GrpcCatalogState ToGrpcCatalogState(CatalogState catalogState)
    {
        return catalogState switch
        {
            CatalogState.WarmingUp => GrpcCatalogState.WarmingUp,
            CatalogState.Alive => GrpcCatalogState.Alive,
            CatalogState.UnknownCatalogState => GrpcCatalogState.UnknownCatalogState,
            CatalogState.Corrupted => GrpcCatalogState.Corrupted,
            CatalogState.Inactive => GrpcCatalogState.Inactive,
            CatalogState.GoingAlive => GrpcCatalogState.GoingAlive,
            CatalogState.BeingActivated => GrpcCatalogState.BeingActivated,
            CatalogState.BeingDeactivated => GrpcCatalogState.BeingDeactivated,
            CatalogState.BeingCreated => GrpcCatalogState.BeingCreated,
            CatalogState.BeingDeleted => GrpcCatalogState.BeingDeleted,
            CatalogState.Missing => GrpcCatalogState.Missing,
            CatalogState.OutOfDate => GrpcCatalogState.OutOfDate,
            CatalogState.BeingUpgraded => GrpcCatalogState.BeingUpgraded,
            _ => throw new EvitaInternalError("Unrecognized local catalog state: " + catalogState)
        };
    }

    /// <summary>
    /// Converts the gRPC entity scope to the client model scope.
    /// </summary>
    public static Scope ToScope(GrpcEntityScope grpcScope)
    {
        return grpcScope switch
        {
            GrpcEntityScope.ScopeLive => Scope.Live,
            GrpcEntityScope.ScopeArchived => Scope.Archived,
            _ => throw new EvitaInternalError("Unrecognized remote scope: " + grpcScope)
        };
    }

    /// <summary>
    /// Converts the client model scope to the gRPC entity scope.
    /// </summary>
    public static GrpcEntityScope ToGrpcScope(Scope scope)
    {
        return scope switch
        {
            Scope.Live => GrpcEntityScope.ScopeLive,
            Scope.Archived => GrpcEntityScope.ScopeArchived,
            _ => throw new EvitaInternalError("Unrecognized local scope: " + scope)
        };
    }

    public static GrpcAttributeSchemaType ToGrpcAttributeSchemaType<T>() where T : IAttributeSchema
    {
        if (typeof(IGlobalAttributeSchema).IsAssignableFrom(typeof(T)))
        {
            return GrpcAttributeSchemaType.GlobalSchema;
        }

        if (typeof(IEntityAttributeSchema).IsAssignableFrom(typeof(T)))
        {
            return GrpcAttributeSchemaType.EntitySchema;
        }

        if (typeof(IAttributeSchema).IsAssignableFrom(typeof(T)))
        {
            return GrpcAttributeSchemaType.ReferenceSchema;
        }
        else
        {
            throw new EvitaInternalError("Unrecognized attribute schema type: " + typeof(T));
        }
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
            GrpcStatisticsBase.CompleteFilterExcludingSelfInUserFilter => StatisticsBase.CompleteFilterExcludingSelfInUserFilter,
            _ => throw new EvitaInternalError("Unrecognized remote statistics base: " + grpcStatisticsBase)
        };
    }

    public static GrpcStatisticsBase ToGrpcStatisticsBase(StatisticsBase statisticsBase)
    {
        return statisticsBase switch
        {
            StatisticsBase.CompleteFilter => GrpcStatisticsBase.CompleteFilter,
            StatisticsBase.WithoutUserFilter => GrpcStatisticsBase.WithoutUserFilter,
            StatisticsBase.CompleteFilterExcludingSelfInUserFilter => GrpcStatisticsBase.CompleteFilterExcludingSelfInUserFilter,
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
            GrpcFacetStatisticsDepth.StatisticsNone => FacetStatisticsDepth.None,
            _ => throw new EvitaInternalError("Unrecognized remote facet statistics depth: " + grpcFacetStatisticsDepth)
        };
    }

    public static GrpcFacetStatisticsDepth ToGrpcFacetStatisticsDepth(FacetStatisticsDepth facetStatisticsDepth)
    {
        return facetStatisticsDepth switch
        {
            FacetStatisticsDepth.Counts => GrpcFacetStatisticsDepth.Counts,
            FacetStatisticsDepth.Impact => GrpcFacetStatisticsDepth.Impact,
            FacetStatisticsDepth.None => GrpcFacetStatisticsDepth.StatisticsNone,
            _ => throw new EvitaInternalError("Unrecognized facet statistics depth: " + facetStatisticsDepth)
        };
    }

    public static PriceInnerRecordHandling ToPriceInnerRecordHandling(
        GrpcPriceInnerRecordHandling grpcPriceInnerRecordHandling)
    {
        return grpcPriceInnerRecordHandling switch
        {
            GrpcPriceInnerRecordHandling.None => PriceInnerRecordHandling.None,
            GrpcPriceInnerRecordHandling.LowestPrice => PriceInnerRecordHandling.LowestPrice,
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
            PriceInnerRecordHandling.LowestPrice => GrpcPriceInnerRecordHandling.LowestPrice,
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
            GrpcCardinality.ZeroOrMoreWithDuplicates => Cardinality.ZeroOrMoreWithDuplicates,
            GrpcCardinality.OneOrMoreWithDuplicates => Cardinality.OneOrMoreWithDuplicates,
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
            Cardinality.ZeroOrMoreWithDuplicates => GrpcCardinality.ZeroOrMoreWithDuplicates,
            Cardinality.OneOrMoreWithDuplicates => GrpcCardinality.OneOrMoreWithDuplicates,
            _ => throw new ArgumentOutOfRangeException(nameof(cardinality), cardinality, null)
        };
    }

    public static HistogramBehavior ToHistogramBehavior(GrpcHistogramBehavior grpcHistogramBehavior)
    {
        return grpcHistogramBehavior switch
        {
            GrpcHistogramBehavior.Standard => HistogramBehavior.Standard,
            GrpcHistogramBehavior.Optimized => HistogramBehavior.Optimized,
            GrpcHistogramBehavior.Equalized => HistogramBehavior.Equalized,
            GrpcHistogramBehavior.EqualizedOptimized => HistogramBehavior.EqualizedOptimized,
            _ => throw new EvitaInternalError("Unrecognized remote histogram behavior: " + grpcHistogramBehavior)
        };
    }

    public static TraversalMode ToTraversalMode(GrpcTraversalMode grpcTraversalMode)
    {
        return grpcTraversalMode switch
        {
            GrpcTraversalMode.DepthFirst => TraversalMode.DepthFirst,
            GrpcTraversalMode.BreadthFirst => TraversalMode.BreadthFirst,
            _ => throw new EvitaInternalError("Unrecognized remote traversal mode: " + grpcTraversalMode)
        };
    }

    public static GrpcTraversalMode ToGrpcTraversalMode(TraversalMode traversalMode)
    {
        return traversalMode switch
        {
            TraversalMode.DepthFirst => GrpcTraversalMode.DepthFirst,
            TraversalMode.BreadthFirst => GrpcTraversalMode.BreadthFirst,
            _ => throw new EvitaInternalError("Unrecognized local traversal mode: " + traversalMode)
        };
    }

    public static FacetRelationType ToFacetRelationType(GrpcFacetRelationType grpcFacetRelationType)
    {
        return grpcFacetRelationType switch
        {
            GrpcFacetRelationType.Disjunction => FacetRelationType.Disjunction,
            GrpcFacetRelationType.Conjunction => FacetRelationType.Conjunction,
            GrpcFacetRelationType.Negation => FacetRelationType.Negation,
            GrpcFacetRelationType.Exclusivity => FacetRelationType.Exclusivity,
            _ => throw new EvitaInternalError("Unrecognized remote facet relation type: " + grpcFacetRelationType)
        };
    }

    public static GrpcFacetRelationType ToGrpcFacetRelationType(FacetRelationType facetRelationType)
    {
        return facetRelationType switch
        {
            FacetRelationType.Disjunction => GrpcFacetRelationType.Disjunction,
            FacetRelationType.Conjunction => GrpcFacetRelationType.Conjunction,
            FacetRelationType.Negation => GrpcFacetRelationType.Negation,
            FacetRelationType.Exclusivity => GrpcFacetRelationType.Exclusivity,
            _ => throw new EvitaInternalError("Unrecognized local facet relation type: " + facetRelationType)
        };
    }

    public static FacetGroupRelationLevel ToFacetGroupRelationLevel(GrpcFacetGroupRelationLevel grpcLevel)
    {
        return grpcLevel switch
        {
            GrpcFacetGroupRelationLevel.WithDifferentFacetsInGroup => FacetGroupRelationLevel.WithDifferentFacetsInGroup,
            GrpcFacetGroupRelationLevel.WithDifferentGroups => FacetGroupRelationLevel.WithDifferentGroups,
            _ => throw new EvitaInternalError("Unrecognized remote facet group relation level: " + grpcLevel)
        };
    }

    public static GrpcFacetGroupRelationLevel ToGrpcFacetGroupRelationLevel(FacetGroupRelationLevel level)
    {
        return level switch
        {
            FacetGroupRelationLevel.WithDifferentFacetsInGroup => GrpcFacetGroupRelationLevel.WithDifferentFacetsInGroup,
            FacetGroupRelationLevel.WithDifferentGroups => GrpcFacetGroupRelationLevel.WithDifferentGroups,
            _ => throw new EvitaInternalError("Unrecognized local facet group relation level: " + level)
        };
    }

    public static ManagedReferencesBehaviour ToManagedReferencesBehaviour(GrpcManagedReferencesBehaviour grpcBehaviour)
    {
        return grpcBehaviour switch
        {
            GrpcManagedReferencesBehaviour.Any => ManagedReferencesBehaviour.Any,
            GrpcManagedReferencesBehaviour.Existing => ManagedReferencesBehaviour.Existing,
            _ => throw new EvitaInternalError("Unrecognized remote managed references behaviour: " + grpcBehaviour)
        };
    }

    public static GrpcManagedReferencesBehaviour ToGrpcManagedReferencesBehaviour(ManagedReferencesBehaviour behaviour)
    {
        return behaviour switch
        {
            ManagedReferencesBehaviour.Any => GrpcManagedReferencesBehaviour.Any,
            ManagedReferencesBehaviour.Existing => GrpcManagedReferencesBehaviour.Existing,
            _ => throw new EvitaInternalError("Unrecognized local managed references behaviour: " + behaviour)
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
            GrpcEvolutionMode.UpdatingReferenceCardinality => EvolutionMode.UpdatingReferenceCardinality,
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
            EvolutionMode.UpdatingReferenceCardinality => GrpcEvolutionMode.UpdatingReferenceCardinality,
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
            GrpcQueryPhase.ExecutionFilterNestedQuery => QueryPhase.ExecutionFilterNestedQuery,
            GrpcQueryPhase.ExecutionSortAndSlice => QueryPhase.ExecutionSortAndSlice,
            GrpcQueryPhase.ExtraResultsFabrication => QueryPhase.ExtraResultsFabrication,
            GrpcQueryPhase.ExtraResultItemFabrication => QueryPhase.ExtraResultItemFabrication,
            GrpcQueryPhase.Fetching => QueryPhase.Fetching,
            GrpcQueryPhase.FetchingReferences => QueryPhase.FetchingReferences,
            GrpcQueryPhase.FetchingParents => QueryPhase.FetchingParents,
            GrpcQueryPhase.FetchingReferenceBodies => QueryPhase.FetchingReferenceBodies,
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
            QueryPhase.ExecutionFilterNestedQuery => GrpcQueryPhase.ExecutionFilterNestedQuery,
            QueryPhase.ExecutionSortAndSlice => GrpcQueryPhase.ExecutionSortAndSlice,
            QueryPhase.ExtraResultsFabrication => GrpcQueryPhase.ExtraResultsFabrication,
            QueryPhase.ExtraResultItemFabrication => GrpcQueryPhase.ExtraResultItemFabrication,
            QueryPhase.Fetching => GrpcQueryPhase.Fetching,
            QueryPhase.FetchingReferences => GrpcQueryPhase.FetchingReferences,
            QueryPhase.FetchingParents => GrpcQueryPhase.FetchingParents,
            QueryPhase.FetchingReferenceBodies => GrpcQueryPhase.FetchingReferenceBodies,
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

    public static CaptureContent ToCaptureContent(GrpcChangeCaptureContent grpcCaptureContent)
    {
        return grpcCaptureContent switch
        {
            GrpcChangeCaptureContent.ChangeHeader => CaptureContent.Header,
            GrpcChangeCaptureContent.ChangeBody => CaptureContent.Body,
            _ => throw new EvitaInternalError("Unrecognized remote capture content: " + grpcCaptureContent)
        };
    }

    public static GrpcChangeCaptureContent ToGrpcCaptureContent(CaptureContent captureContent)
    {
        return captureContent switch
        {
            CaptureContent.Header => GrpcChangeCaptureContent.ChangeHeader,
            CaptureContent.Body => GrpcChangeCaptureContent.ChangeBody,
            _ => throw new ArgumentOutOfRangeException(nameof(captureContent), captureContent, null)
        };
    }

    public static AttributeUniquenessType ToAttributeUniquenessType(
        GrpcAttributeUniquenessType grpcAttributeUniquenessType)
    {
        return grpcAttributeUniquenessType switch
        {
            GrpcAttributeUniquenessType.NotUnique => AttributeUniquenessType.NotUnique,
            GrpcAttributeUniquenessType.UniqueWithinCollection => AttributeUniquenessType.UniqueWithinCollection,
            GrpcAttributeUniquenessType.UniqueWithinCollectionLocale =>
                AttributeUniquenessType.UniqueWithinCollectionLocale,
            _ => throw new EvitaInternalError("Unrecognized remote attribute uniqueness type: " +
                                              grpcAttributeUniquenessType)
        };
    }

    public static GrpcAttributeUniquenessType ToGrpcAttributeUniquenessType(
        AttributeUniquenessType attributeUniquenessType)
    {
        return attributeUniquenessType switch
        {
            AttributeUniquenessType.NotUnique => GrpcAttributeUniquenessType.NotUnique,
            AttributeUniquenessType.UniqueWithinCollection => GrpcAttributeUniquenessType.UniqueWithinCollection,
            AttributeUniquenessType.UniqueWithinCollectionLocale =>
                GrpcAttributeUniquenessType.UniqueWithinCollectionLocale,
            _ => throw new EvitaInternalError("Unrecognized attribute uniqueness type: " + attributeUniquenessType)
        };
    }

    public static GlobalAttributeUniquenessType ToGlobalAttributeUniquenessType(
        GrpcGlobalAttributeUniquenessType grpcGlobalAttributeUniquenessType)
    {
        return grpcGlobalAttributeUniquenessType switch
        {
            GrpcGlobalAttributeUniquenessType.NotGloballyUnique => GlobalAttributeUniquenessType.NotUnique,
            GrpcGlobalAttributeUniquenessType.UniqueWithinCatalog => GlobalAttributeUniquenessType.UniqueWithinCatalog,
            GrpcGlobalAttributeUniquenessType.UniqueWithinCatalogLocale => GlobalAttributeUniquenessType
                .UniqueWithinCatalogLocale,
            _ => throw new EvitaInternalError("Unrecognized remote global attribute uniqueness type: " +
                                              grpcGlobalAttributeUniquenessType)
        };
    }

    public static GrpcGlobalAttributeUniquenessType ToGrpcGlobalAttributeUniquenessType(
        GlobalAttributeUniquenessType globalAttributeUniquenessType)
    {
        return globalAttributeUniquenessType switch
        {
            GlobalAttributeUniquenessType.NotUnique => GrpcGlobalAttributeUniquenessType.NotGloballyUnique,
            GlobalAttributeUniquenessType.UniqueWithinCatalog => GrpcGlobalAttributeUniquenessType.UniqueWithinCatalog,
            GlobalAttributeUniquenessType.UniqueWithinCatalogLocale => GrpcGlobalAttributeUniquenessType
                .UniqueWithinCatalogLocale,
            _ => throw new EvitaInternalError("Unrecognized global attribute uniqueness type: " +
                                              globalAttributeUniquenessType)
        };
    }

    /// <summary>
    /// Extracts the attribute uniqueness effective in the live scope from the scoped list. When the scoped list
    /// is empty (messages produced by servers older than 2024.12), falls back to the passed legacy single-value
    /// field. Mirrors the Java `EvitaEnumConverter.toScopedAttributeUniquenessTypes` fallback chain, projected
    /// to the live scope which is the only scope the C# schema model currently tracks.
    /// </summary>
    public static AttributeUniquenessType ToAttributeUniquenessType(
        IList<GrpcScopedAttributeUniquenessType> uniqueInScopes,
        GrpcAttributeUniquenessType legacyUnique)
    {
        if (uniqueInScopes.Count == 0)
        {
            return ToAttributeUniquenessType(legacyUnique);
        }

        GrpcScopedAttributeUniquenessType? liveScope =
            uniqueInScopes.FirstOrDefault(x => x.Scope == GrpcEntityScope.ScopeLive);
        return liveScope is null
            ? AttributeUniquenessType.NotUnique
            : ToAttributeUniquenessType(liveScope.UniquenessType);
    }

    /// <summary>
    /// Extracts the global attribute uniqueness effective in the live scope from the scoped list. When the scoped
    /// list is empty (messages produced by servers older than 2024.12), falls back to the passed legacy
    /// single-value field.
    /// </summary>
    public static GlobalAttributeUniquenessType ToGlobalAttributeUniquenessType(
        IList<GrpcScopedGlobalAttributeUniquenessType> uniqueGloballyInScopes,
        GrpcGlobalAttributeUniquenessType legacyUniqueGlobally)
    {
        if (uniqueGloballyInScopes.Count == 0)
        {
            return ToGlobalAttributeUniquenessType(legacyUniqueGlobally);
        }

        GrpcScopedGlobalAttributeUniquenessType? liveScope =
            uniqueGloballyInScopes.FirstOrDefault(x => x.Scope == GrpcEntityScope.ScopeLive);
        return liveScope is null
            ? GlobalAttributeUniquenessType.NotUnique
            : ToGlobalAttributeUniquenessType(liveScope.UniquenessType);
    }

    /// <summary>
    /// Resolves a boolean schema flag (filterable / sortable / faceted) from its scoped representation. When the
    /// scope list is empty (messages produced by servers older than 2024.12), falls back to the passed legacy
    /// boolean field. Mirrors the Java `EvitaEnumConverter.toBooleanScopes` fallback chain, projected to the live
    /// scope which is the only scope the C# schema model currently tracks.
    /// </summary>
    public static bool ToScopedBooleanFlag(IList<GrpcEntityScope> scopes, bool legacyFlag)
    {
        return scopes.Count == 0 ? legacyFlag : scopes.Contains(GrpcEntityScope.ScopeLive);
    }

    /// <summary>
    /// Resolves the reference indexed flag from its three wire representations, newest first: `scopedIndexTypes`,
    /// then the deprecated `indexedInScopes` and finally the oldest boolean `indexed` field. Mirrors the Java
    /// `EntitySchemaConverter.getIndexedInScopes` fallback chain, projected to the live scope.
    /// </summary>
    public static bool ToReferenceIndexedFlag(
        IList<GrpcScopedReferenceIndexType> scopedIndexTypes,
        IList<GrpcEntityScope> indexedInScopes,
        bool legacyIndexed)
    {
        if (scopedIndexTypes.Count > 0)
        {
            return scopedIndexTypes.Any(x =>
                x.Scope == GrpcEntityScope.ScopeLive &&
                x.IndexType != GrpcReferenceIndexType.ReferenceIndexTypeNone);
        }

        return indexedInScopes.Count > 0
            ? indexedInScopes.Contains(GrpcEntityScope.ScopeLive)
            : legacyIndexed;
    }

    public static GrpcHistogramBehavior ToGrpcHistogramBehavior(HistogramBehavior histogramBehavior)
    {
        return histogramBehavior switch
        {
            HistogramBehavior.Standard => GrpcHistogramBehavior.Standard,
            HistogramBehavior.Optimized => GrpcHistogramBehavior.Optimized,
            HistogramBehavior.Equalized => GrpcHistogramBehavior.Equalized,
            HistogramBehavior.EqualizedOptimized => GrpcHistogramBehavior.EqualizedOptimized,
            _ => throw new EvitaInternalError("Unrecognized histogram behavior: " + histogramBehavior)
        };
    }

    public static GrpcCommitBehavior ToGrpcCommitBehavior(EvitaClientTransaction.CommitBehavior commitBehavior)
    {
        return commitBehavior switch
        {
            EvitaClientTransaction.CommitBehavior.WaitForConflictResolution => GrpcCommitBehavior
                .WaitForConflictResolution,
            EvitaClientTransaction.CommitBehavior.WaitForWalPersistence => GrpcCommitBehavior.WaitForLogPersistence,
            EvitaClientTransaction.CommitBehavior.WaitForChangesVisible => GrpcCommitBehavior.WaitForChangesVisible,
            _ => throw new ArgumentOutOfRangeException(nameof(commitBehavior), commitBehavior, null)
        };
    }

    public static EvitaClientTransaction.CommitBehavior ToCommitBehavior(GrpcCommitBehavior commitBehaviour)
    {
        return commitBehaviour switch
        {
            GrpcCommitBehavior.WaitForConflictResolution => EvitaClientTransaction.CommitBehavior
                .WaitForConflictResolution,
            GrpcCommitBehavior.WaitForLogPersistence => EvitaClientTransaction.CommitBehavior.WaitForWalPersistence,
            GrpcCommitBehavior.WaitForChangesVisible => EvitaClientTransaction.CommitBehavior.WaitForChangesVisible,
            _ => throw new ArgumentOutOfRangeException("Unrecognized remote commit behavior: " + commitBehaviour)
        };
    }

    public static GrpcNamingConvention ToGrpcNamingConvention(NamingConvention namingConvention)
    {
        return namingConvention switch
        {
            NamingConvention.SnakeCase => GrpcNamingConvention.SnakeCase,
            NamingConvention.CamelCase => GrpcNamingConvention.CamelCase,
            NamingConvention.UpperSnakeCase => GrpcNamingConvention.UpperSnakeCase,
            NamingConvention.PascalCase => GrpcNamingConvention.PascalCase,
            NamingConvention.KebabCase => GrpcNamingConvention.KebabCase,
            _ => throw new ArgumentOutOfRangeException(nameof(namingConvention), namingConvention, null)
        };
    }

    public static NamingConvention ToNamingConvention(GrpcNamingConvention namingConvention)
    {
        return namingConvention switch
        {
            GrpcNamingConvention.SnakeCase => NamingConvention.SnakeCase,
            GrpcNamingConvention.CamelCase => NamingConvention.CamelCase,
            GrpcNamingConvention.UpperSnakeCase => NamingConvention.UpperSnakeCase,
            GrpcNamingConvention.PascalCase => NamingConvention.PascalCase,
            GrpcNamingConvention.KebabCase => NamingConvention.KebabCase,
            _ => throw new ArgumentOutOfRangeException("Unrecognized naming convention: " + namingConvention)
        };
    }

    public static CaptureArea ToCaptureArea(GrpcChangeCaptureArea area)
    {
        return area switch
        {
            GrpcChangeCaptureArea.Data => CaptureArea.Data,
            GrpcChangeCaptureArea.Schema => CaptureArea.Schema,
            GrpcChangeCaptureArea.Infrastructure => CaptureArea.Infrastructure,
            _ => throw new ArgumentOutOfRangeException("Unrecognized capture area: " + area)
        };
    }

    public static GrpcChangeCaptureArea ToGrpcCaptureArea(CaptureArea? area)
    {
        return area switch
        {
            CaptureArea.Data => GrpcChangeCaptureArea.Data,
            CaptureArea.Schema => GrpcChangeCaptureArea.Schema,
            CaptureArea.Infrastructure => GrpcChangeCaptureArea.Infrastructure,
            _ => throw new ArgumentOutOfRangeException(nameof(area), area, null)
        };
    }

    public static Operation ToOperation(GrpcChangeCaptureOperation grpcOperation)
    {
        return grpcOperation switch
        {
            GrpcChangeCaptureOperation.Upsert => Operation.Upsert,
            GrpcChangeCaptureOperation.Remove => Operation.Remove,
            GrpcChangeCaptureOperation.Transaction => Operation.Transaction,
            _ => throw new ArgumentOutOfRangeException("Unrecognized operation: " + grpcOperation)
        };
    }

    public static GrpcChangeCaptureOperation ToGrpcOperation(Operation operation)
    {
        return operation switch
        {
            Operation.Upsert => GrpcChangeCaptureOperation.Upsert,
            Operation.Remove => GrpcChangeCaptureOperation.Remove,
            Operation.Transaction => GrpcChangeCaptureOperation.Transaction,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null)
        };
    }

    public static ContainerType ToContainerType(GrpcChangeCaptureContainerType grpcCaptureContainerType)
    {
        return grpcCaptureContainerType switch
            {
                GrpcChangeCaptureContainerType.ContainerCatalog => ContainerType.Catalog,
                GrpcChangeCaptureContainerType.ContainerEntity => ContainerType.Entity,
                GrpcChangeCaptureContainerType.ContainerAttribute => ContainerType.Attribute,
                GrpcChangeCaptureContainerType.ContainerAssociatedData => ContainerType.AssociatedData,
                GrpcChangeCaptureContainerType.ContainerReference => ContainerType.Reference,
                GrpcChangeCaptureContainerType.ContainerPrice => ContainerType.Price,
                _ => throw new ArgumentOutOfRangeException("Unrecognized container type: " +
                                                           grpcCaptureContainerType)
            };
    }

    public static GrpcChangeCaptureContainerType ToGrpcCaptureContainerType(ContainerType containerType)
    {
        return containerType switch
        {
            ContainerType.Catalog => GrpcChangeCaptureContainerType.ContainerCatalog,
            ContainerType.Entity => GrpcChangeCaptureContainerType.ContainerEntity,
            ContainerType.Attribute => GrpcChangeCaptureContainerType.ContainerAttribute,
            ContainerType.AssociatedData => GrpcChangeCaptureContainerType.ContainerAssociatedData,
            ContainerType.Reference => GrpcChangeCaptureContainerType.ContainerReference,
            ContainerType.Price => GrpcChangeCaptureContainerType.ContainerPrice,
            _ => throw new ArgumentOutOfRangeException(nameof(containerType), containerType, null)
        };
    }
    
    public static HealthProblem ToHealthProblem(GrpcHealthProblem grpcHealthProblem)
    {
        return grpcHealthProblem switch
        {
            GrpcHealthProblem.MemoryShortage => HealthProblem.MemoryShortage,
            GrpcHealthProblem.ExternalApiUnavailable => HealthProblem.ExternalApiUnavailable,
            GrpcHealthProblem.InputQueuesOverloaded => HealthProblem.InputQueuesOverloaded,
            GrpcHealthProblem.JavaInternalErrors => HealthProblem.JavaInternalErrors,
            _ => throw new EvitaInternalError("Unrecognized remote health problem: " + grpcHealthProblem)
        };
    }

    public static Readiness ToReadiness(GrpcReadiness grpcReadiness)
    {
        return grpcReadiness switch
        {
            GrpcReadiness.ApiStarting => Readiness.ApiStarting,
            GrpcReadiness.ApiReady => Readiness.ApiReady,
            GrpcReadiness.ApiStalling => Readiness.ApiStalling,
            GrpcReadiness.ApiShutdown => Readiness.ApiShutdown,
            GrpcReadiness.ApiUnknown => Readiness.ApiUnknown,
            _ => throw new EvitaInternalError("Unrecognized remote readiness: " + grpcReadiness)
        };
    }

    public static TaskSimplifiedState ToTaskSimplifiedState(GrpcTaskSimplifiedState grpcState)
    {
        return grpcState switch
        {
            GrpcTaskSimplifiedState.TaskQueued => TaskSimplifiedState.Queued,
            GrpcTaskSimplifiedState.TaskRunning => TaskSimplifiedState.Running,
            GrpcTaskSimplifiedState.TaskFinished => TaskSimplifiedState.Finished,
            GrpcTaskSimplifiedState.TaskFailed => TaskSimplifiedState.Failed,
            GrpcTaskSimplifiedState.TaskWaitingForPrecondition => TaskSimplifiedState.WaitingForPrecondition,
            _ => throw new EvitaInternalError("Unrecognized remote task state: " + grpcState)
        };
    }

    public static GrpcTaskSimplifiedState ToGrpcTaskSimplifiedState(TaskSimplifiedState state)
    {
        return state switch
        {
            TaskSimplifiedState.Queued => GrpcTaskSimplifiedState.TaskQueued,
            TaskSimplifiedState.Running => GrpcTaskSimplifiedState.TaskRunning,
            TaskSimplifiedState.Finished => GrpcTaskSimplifiedState.TaskFinished,
            TaskSimplifiedState.Failed => GrpcTaskSimplifiedState.TaskFailed,
            TaskSimplifiedState.WaitingForPrecondition => GrpcTaskSimplifiedState.TaskWaitingForPrecondition,
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
        };
    }

    public static ConflictResolutionOverride ToConflictResolutionOverride(GrpcConflictResolutionOverride grpcOverride)
    {
        return grpcOverride switch
        {
            GrpcConflictResolutionOverride.ConflictResolutionOverrideInherited => ConflictResolutionOverride.Inherited,
            GrpcConflictResolutionOverride.ConflictResolutionOverrideGranular => ConflictResolutionOverride.Granular,
            GrpcConflictResolutionOverride.ConflictResolutionOverrideEntity => ConflictResolutionOverride.Entity,
            _ => throw new EvitaInternalError("Unrecognized remote conflict resolution override: " + grpcOverride)
        };
    }

    public static GrpcConflictResolutionOverride ToGrpcConflictResolutionOverride(ConflictResolutionOverride @override)
    {
        return @override switch
        {
            ConflictResolutionOverride.Inherited => GrpcConflictResolutionOverride.ConflictResolutionOverrideInherited,
            ConflictResolutionOverride.Granular => GrpcConflictResolutionOverride.ConflictResolutionOverrideGranular,
            ConflictResolutionOverride.Entity => GrpcConflictResolutionOverride.ConflictResolutionOverrideEntity,
            _ => throw new ArgumentOutOfRangeException(nameof(@override), @override, null)
        };
    }

    public static ConflictPolicy ToConflictPolicy(GrpcConflictPolicy grpcPolicy)
    {
        return grpcPolicy switch
        {
            GrpcConflictPolicy.ConflictPolicyNone => ConflictPolicy.None,
            GrpcConflictPolicy.ConflictPolicyCatalog => ConflictPolicy.Catalog,
            GrpcConflictPolicy.ConflictPolicyCollection => ConflictPolicy.Collection,
            GrpcConflictPolicy.ConflictPolicyEntity => ConflictPolicy.Entity,
            _ => throw new EvitaInternalError("Unrecognized remote conflict policy: " + grpcPolicy)
        };
    }

    public static GrpcConflictPolicy ToGrpcConflictPolicy(ConflictPolicy policy)
    {
        return policy switch
        {
            ConflictPolicy.None => GrpcConflictPolicy.ConflictPolicyNone,
            ConflictPolicy.Catalog => GrpcConflictPolicy.ConflictPolicyCatalog,
            ConflictPolicy.Collection => GrpcConflictPolicy.ConflictPolicyCollection,
            ConflictPolicy.Entity => GrpcConflictPolicy.ConflictPolicyEntity,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        };
    }

    public static GranularConflictPolicy ToGranularConflictPolicy(GrpcGranularConflictPolicy grpcPolicy)
    {
        return grpcPolicy switch
        {
            GrpcGranularConflictPolicy.GranularConflictPolicyEntityAttribute => GranularConflictPolicy.EntityAttribute,
            GrpcGranularConflictPolicy.GranularConflictPolicyReference => GranularConflictPolicy.Reference,
            GrpcGranularConflictPolicy.GranularConflictPolicyReferenceAttribute => GranularConflictPolicy.ReferenceAttribute,
            GrpcGranularConflictPolicy.GranularConflictPolicyAssociatedData => GranularConflictPolicy.AssociatedData,
            GrpcGranularConflictPolicy.GranularConflictPolicyPrice => GranularConflictPolicy.Price,
            GrpcGranularConflictPolicy.GranularConflictPolicyHierarchy => GranularConflictPolicy.Hierarchy,
            _ => throw new EvitaInternalError("Unrecognized remote granular conflict policy: " + grpcPolicy)
        };
    }

    public static GrpcGranularConflictPolicy ToGrpcGranularConflictPolicy(GranularConflictPolicy policy)
    {
        return policy switch
        {
            GranularConflictPolicy.EntityAttribute => GrpcGranularConflictPolicy.GranularConflictPolicyEntityAttribute,
            GranularConflictPolicy.Reference => GrpcGranularConflictPolicy.GranularConflictPolicyReference,
            GranularConflictPolicy.ReferenceAttribute => GrpcGranularConflictPolicy.GranularConflictPolicyReferenceAttribute,
            GranularConflictPolicy.AssociatedData => GrpcGranularConflictPolicy.GranularConflictPolicyAssociatedData,
            GranularConflictPolicy.Price => GrpcGranularConflictPolicy.GranularConflictPolicyPrice,
            GranularConflictPolicy.Hierarchy => GrpcGranularConflictPolicy.GranularConflictPolicyHierarchy,
            _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
        };
    }

    public static ConflictResolution? ToConflictResolution(GrpcConflictResolution? grpcConflictResolution)
    {
        return grpcConflictResolution is null
            ? null
            : new ConflictResolution(
                ToConflictPolicy(grpcConflictResolution.Policy),
                grpcConflictResolution.Granularity.Select(ToGranularConflictPolicy).ToArray()
            );
    }

    public static GrpcConflictResolution? ToGrpcConflictResolution(ConflictResolution? conflictResolution)
    {
        if (conflictResolution is null)
        {
            return null;
        }
        GrpcConflictResolution grpcConflictResolution = new GrpcConflictResolution
        {
            Policy = ToGrpcConflictPolicy(conflictResolution.Policy)
        };
        grpcConflictResolution.Granularity.AddRange(
            conflictResolution.Granularity.Select(ToGrpcGranularConflictPolicy)
        );
        return grpcConflictResolution;
    }

    public static TaskTrait ToTaskTrait(GrpcTaskTrait grpcTaskTrait)
    {
        return grpcTaskTrait switch
        {
            GrpcTaskTrait.TaskCanBeStarted => TaskTrait.CanBeStarted,
            GrpcTaskTrait.TaskCanBeCancelled => TaskTrait.CanBeCancelled,
            GrpcTaskTrait.TaskNeedsToBeStopped => TaskTrait.NeedsToBeStopped,
            _ => throw new EvitaInternalError("Unrecognized remote task trait: " + grpcTaskTrait)
        };
    }

    public static GrpcClassifierType ToGrpcClassifierType(ClassifierType key)
    {
        return key switch
        {
            ClassifierType.ServerName => GrpcClassifierType.ClassifierTypeServerName,
            ClassifierType.Catalog => GrpcClassifierType.ClassifierTypeCatalog,
            ClassifierType.Entity => GrpcClassifierType.ClassifierTypeEntity,
            ClassifierType.Attribute => GrpcClassifierType.ClassifierTypeAttribute,
            ClassifierType.AssociatedData => GrpcClassifierType.ClassifierTypeAssociatedData,
            ClassifierType.Reference => GrpcClassifierType.ClassifierTypeReference,
            ClassifierType.ReferenceAttribute => GrpcClassifierType.ClassifierTypeReferenceAttribute,
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
        };
    }
}
