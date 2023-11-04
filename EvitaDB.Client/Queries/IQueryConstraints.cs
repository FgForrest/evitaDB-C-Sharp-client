using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Head;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;
using Random = EvitaDB.Client.Queries.Order.Random;

namespace EvitaDB.Client.Queries;

public interface IQueryConstraints
{
    /// <inheritdoc cref="Client.Queries.Head.Collection"/>
    static Collection Collection(string entityType) => new(entityType);

    static FilterBy? FilterBy(params IFilterConstraint?[]? constraint) => constraint == null ? null : new FilterBy(constraint);

    static FilterGroupBy? FilterGroupBy(params IFilterConstraint?[]? constraints) =>
        constraints == null ? null : new FilterGroupBy(constraints);

    static And? And(params IFilterConstraint?[]? constraints) => constraints is null ? null : new And(constraints);

    static Or? Or(params IFilterConstraint?[]? constraints) => constraints is null ? null : new Or(constraints);

    static Not? Not(IFilterConstraint? constraint) => constraint is null ? null : new Not(constraint);

    static ReferenceHaving? ReferenceHaving(string? referenceName, params IFilterConstraint?[]? constraints) =>
        referenceName is null ? null : new ReferenceHaving(referenceName, constraints!);

    static UserFilter? UserFilter(params IFilterConstraint?[]? constraints) =>
        constraints is null ? null : new UserFilter(constraints);

    static AttributeBetween<T>? AttributeBetween<T>(string attributeName, T? from, T? to) where T : IComparable =>
        from is null && to is null ? null : new AttributeBetween<T>(attributeName, from, to);

    static AttributeContains? AttributeContains(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeContains(attributeName, textToSearch);

    static AttributeStartsWith? AttributeStartsWith(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeStartsWith(attributeName, textToSearch);

    static AttributeEndsWith? AttributeEndsWith(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeEndsWith(attributeName, textToSearch);

    static AttributeEquals<T>? AttributeEquals<T>(string attributeName, T? attributeValue) =>
        attributeValue is null ? null : new AttributeEquals<T>(attributeName, attributeValue);

    static AttributeLessThan<T>? AttributeLessThan<T>(string attributeName, T? attributeValue) where T : IComparable =>
        attributeValue is null ? null : new AttributeLessThan<T>(attributeName, attributeValue);

    static AttributeLessThanEquals<T>? AttributeLessThanEquals<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeLessThanEquals<T>(attributeName, attributeValue);

    static AttributeGreaterThan<T>? AttributeGreaterThan<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeGreaterThan<T>(attributeName, attributeValue);

    static AttributeGreaterThanEquals<T>? AttributeGreaterThanEquals<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeGreaterThanEquals<T>(attributeName, attributeValue);

    static PriceInPriceLists? PriceInPriceLists(params string[]? priceListNames) =>
        priceListNames is null ? null : new PriceInPriceLists(priceListNames);

    static PriceInCurrency? PriceInCurrency(string? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    static PriceInCurrency? PriceInCurrency(Currency? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    static HierarchyWithin? HierarchyWithinSelf(IFilterConstraint? ofParent,
        params IHierarchySpecificationFilterConstraint[]? with)
    {
        if (ofParent is null)
        {
            return null;
        }

        if (with is null)
        {
            return new HierarchyWithin(ofParent);
        }

        return new HierarchyWithin(ofParent, with);
    }

    static HierarchyWithin? HierarchyWithin(string referenceName, IFilterConstraint? ofParent,
        params IHierarchySpecificationFilterConstraint?[]? with)
    {
        if (ofParent is null)
        {
            return null;
        }
        if (with is null)
        {
            return new HierarchyWithin(referenceName, ofParent);
        }

        return new HierarchyWithin(referenceName, ofParent, with!);
    }

    static HierarchyWithinRoot HierarchyWithinRootSelf(params IHierarchySpecificationFilterConstraint?[]? with) =>
        with is null ? new HierarchyWithinRoot() : new HierarchyWithinRoot(with);

    static HierarchyWithinRoot HierarchyWithinRoot(string referenceName,
        params IHierarchySpecificationFilterConstraint?[]? with) =>
        with is null ? new HierarchyWithinRoot() : new HierarchyWithinRoot(referenceName, with);


    static HierarchyHaving? Having(params IFilterConstraint?[]? includeChildTreeConstraints) =>
        ArrayUtils.IsEmpty(includeChildTreeConstraints) ? null : new HierarchyHaving(includeChildTreeConstraints!);

    static HierarchyExcluding? Excluding(params IFilterConstraint[]? excludeChildTreeConstraints) =>
        ArrayUtils.IsEmpty(excludeChildTreeConstraints) ? null : new HierarchyExcluding(excludeChildTreeConstraints!);

    static HierarchyDirectRelation DirectRelation() => new HierarchyDirectRelation();

    static HierarchyExcludingRoot ExcludingRoot() => new HierarchyExcludingRoot();

    static EntityLocaleEquals? EntityLocaleEquals(CultureInfo? locale) =>
        locale is null ? null : new EntityLocaleEquals(locale);

    static EntityHaving? EntityHaving(IFilterConstraint? filterConstraint) =>
        filterConstraint is null ? null : new EntityHaving(filterConstraint);

    static AttributeInRange<DateTimeOffset>?
        AttributeInRange(string attributeName, DateTimeOffset? dateTimeOffsetValue) =>
        dateTimeOffsetValue is null
            ? null
            : new AttributeInRange<DateTimeOffset>(attributeName, dateTimeOffsetValue.Value);

    static AttributeInRange<int>? AttributeInRange(string attributeName, int? intValue) =>
        intValue is null ? null : new AttributeInRange<int>(attributeName, intValue.Value);

    static AttributeInRange<long>? AttributeInRange(string attributeName, long? longValue) =>
        longValue is null ? null : new AttributeInRange<long>(attributeName, longValue.Value);

    static AttributeInRange<short>? AttributeInRange(string attributeName, short? shortValue) =>
        shortValue is null ? null : new AttributeInRange<short>(attributeName, shortValue.Value);

    static AttributeInRange<byte>? AttributeInRange(string attributeName, byte? byteValue) =>
        byteValue is null ? null : new AttributeInRange<byte>(attributeName, byteValue.Value);

    static AttributeInRange<decimal>? AttributeInRange(string attributeName, decimal? decimalValue) =>
        decimalValue is null ? null : new AttributeInRange<decimal>(attributeName, decimalValue.Value);

    static AttributeInRange<DateTimeOffset> AttributeInRangeNow(string attributeName) => new(attributeName);

    static AttributeInSet<T>? AttributeInSet<T>(string attributeName, params T[]? set)
    {
        if (set is null)
        {
            return null;
        }

        List<T> args = set.Where(x => x is not null).ToList();
        if (args.Count == 0)
        {
            return null;
        }

        if (args.Count == set.Length)
        {
            return new AttributeInSet<T>(attributeName, set);
        }

        T[] limitedSet = (T[]) Array.CreateInstance(set.GetType().GetElementType()!, args.Count);
        for (int i = 0; i < args.Count; i++)
        {
            limitedSet[i] = args[i];
        }

        return new AttributeInSet<T>(attributeName, limitedSet);
    }

    static AttributeEquals<bool> AttributeEqualsFalse(string attributeName) =>
        new AttributeEquals<bool>(attributeName, false);

    static AttributeEquals<bool> AttributeEqualsTrue(string attributeName) =>
        new AttributeEquals<bool>(attributeName, true);

    static AttributeIs? AttributeIs(string attributeName, AttributeSpecialValue? specialValue) =>
        specialValue is null ? null : new AttributeIs(attributeName, specialValue.Value);

    static AttributeIs AttributeIsNull(string attributeName) => new(attributeName, AttributeSpecialValue.Null);

    static AttributeIs AttributeIsNotNull(string attributeName) => new(attributeName, AttributeSpecialValue.NotNull);

    static PriceBetween? PriceBetween(decimal? minPrice, decimal? maxPrice) =>
        minPrice is null && maxPrice is null ? null : new PriceBetween(minPrice, maxPrice);

    static PriceValidIn? PriceValidIn(DateTimeOffset? theMoment) =>
        theMoment is null ? null : new PriceValidIn(theMoment.Value);

    static PriceValidIn PriceValidNow() => new PriceValidIn();

    static FacetHaving? FacetHaving(string referenceName, params IFilterConstraint?[]? constraints) =>
        ArrayUtils.IsEmpty(constraints) ? null : new FacetHaving(referenceName, constraints!);


    static EntityPrimaryKeyInSet? EntityPrimaryKeyInSet(params int[]? primaryKeys) =>
        primaryKeys == null ? null : new EntityPrimaryKeyInSet(primaryKeys);

    static OrderBy? OrderBy(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new OrderBy(constraints);

    static OrderGroupBy? OrderGroupBy(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new OrderGroupBy(constraints);

    static EntityPrimaryKeyInFilter EntityPrimaryKeyInFilter() => new EntityPrimaryKeyInFilter();

    static EntityPrimaryKeyExact? EntityPrimaryKeyExact(params int[]? primaryKeys) =>
        ArrayUtils.IsEmpty(primaryKeys) ? null : new EntityPrimaryKeyExact(primaryKeys!);

    static AttributeSetInFilter? AttributeSetInFilter(string? attributeName) =>
        string.IsNullOrEmpty(attributeName) ? null : new AttributeSetInFilter(attributeName);

    static AttributeSetExact? AttributeSetExact(string? attributeName, params object[]? attributeValues) =>
        ArrayUtils.IsEmpty(attributeValues) || string.IsNullOrEmpty(attributeName)
            ? null
            : new AttributeSetExact(attributeName, attributeValues!);

    static ReferenceProperty? ReferenceProperty(string propertyName, params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new ReferenceProperty(propertyName, constraints);

    static EntityProperty? EntityProperty(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new EntityProperty(constraints);

    static EntityGroupProperty? EntityGroupProperty(params IOrderConstraint?[]? constraints) =>
        constraints == null ? null : new EntityGroupProperty(constraints);

    static AttributeNatural AttributeNatural(string attributeName) => new(attributeName);

    static AttributeNatural AttributeNatural(string attributeName, OrderDirection orderDirection) =>
        new AttributeNatural(attributeName, orderDirection);

    static PriceNatural PriceNatural() => new();

    static PriceNatural PriceNatural(OrderDirection orderDirection) => new(orderDirection);

    static Random Random() => new();

    static Require? Require(params IRequireConstraint?[]? constraints) =>
        constraints is null ? null : new Require(constraints);

    static AttributeHistogram? AttributeHistogram(int requestedBucketCount, params string[]? attributeNames) =>
        ArrayUtils.IsEmpty(attributeNames) ? null : new AttributeHistogram(requestedBucketCount, attributeNames!);

    static PriceHistogram PriceHistogram(int requestedBucketCount) => new(requestedBucketCount);

    static FacetGroupsConjunction? FacetGroupsConjunction(string referenceName, FilterBy? filterBy) =>
        filterBy is null || !filterBy.Applicable ? null : new FacetGroupsConjunction(referenceName, filterBy);

    static FacetGroupsDisjunction? FacetGroupsDisjunction(string referenceName, FilterBy? filterBy) =>
        filterBy is null || !filterBy.Applicable ? null : new FacetGroupsDisjunction(referenceName, filterBy);

    static FacetGroupsNegation? FacetGroupsNegation(string referenceName, FilterBy? filterBy) =>
        filterBy is null || !filterBy.Applicable ? null : new FacetGroupsNegation(referenceName, filterBy);

    static HierarchyOfSelf? HierarchyOfSelf(params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(requirements) ? null : new HierarchyOfSelf(null, requirements!);

    static HierarchyOfSelf? HierarchyOfSelf(OrderBy? orderBy, params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(requirements) ? null : new HierarchyOfSelf(orderBy, requirements!);

    static HierarchyOfReference? HierarchyOfReference(string referenceName,
        params IHierarchyRequireConstraint?[]? requirements) =>
        HierarchyOfReference(referenceName, null, null, requirements!);

    static HierarchyOfReference? HierarchyOfReference(string referenceName, OrderBy orderBy,
        params IHierarchyRequireConstraint?[]? requirements) =>
        HierarchyOfReference(referenceName, null, orderBy, requirements!);

    static HierarchyOfReference? HierarchyOfReference(string? referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[]? requirements) =>
        referenceName is null || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceName,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, requirements!);

    static HierarchyOfReference? HierarchyOfReference(string? referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        OrderBy? orderBy,
        params IHierarchyRequireConstraint?[]? requirements) =>
        referenceName is null || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(
                referenceName,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty,
                orderBy,
                requirements!
            );

    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        params IHierarchyRequireConstraint[] requirements) =>
        HierarchyOfReference(referenceNames, null, null, requirements);

    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames, OrderBy? orderBy,
        params IHierarchyRequireConstraint[] requirements) =>
        HierarchyOfReference(referenceNames, null, orderBy, requirements);

    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(referenceNames) || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceNames!,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, requirements!);

    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour, OrderBy? orderBy,
        params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(referenceNames) || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceNames!,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, orderBy,
                requirements!);

    static HierarchyFromRoot? FromRoot(string? outputName, params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null :
        requirements is null ? new HierarchyFromRoot(outputName) : new HierarchyFromRoot(outputName, requirements);

    static HierarchyFromRoot? FromRoot(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null :
        requirements is null ? new HierarchyFromRoot(outputName, entityFetch) :
        new HierarchyFromRoot(outputName, entityFetch, requirements);

    static HierarchyFromNode? FromNode(string? outputName, HierarchyNode? node,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null || node is null ? null :
        requirements is null ? new HierarchyFromNode(outputName, node) :
        new HierarchyFromNode(outputName, node, requirements!);

    static HierarchyFromNode? FromNode(string? outputName, HierarchyNode? node, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null || node is null ? null :
        entityFetch is null ? new HierarchyFromNode(outputName, node) :
        new HierarchyFromNode(outputName, node, entityFetch, requirements!);

    static HierarchyChildren? Children(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyChildren(outputName, entityFetch, requirements!);

    static HierarchyChildren? Children(string? outputName, params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null 
            : new HierarchyChildren(outputName, requirements!);

    static HierarchySiblings? Siblings(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchySiblings(outputName, entityFetch, requirements!);

    static HierarchySiblings? Siblings(string? outputName,
        params IHierarchyOutputRequireConstraint[]? requirements) => outputName is null
        ? null
        : new HierarchySiblings(outputName, requirements!);

    static HierarchySiblings Siblings(EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) =>
        new(null, entityFetch, requirements!);

    static HierarchySiblings? Siblings(params IHierarchyOutputRequireConstraint?[]? requirements) =>
        new(null, requirements!);

    static HierarchyParents? Parents(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyParents(outputName, entityFetch, requirements!);

    static HierarchyParents? Parents(string? outputName, EntityFetch? entityFetch, HierarchySiblings? siblings,
        params IHierarchyOutputRequireConstraint?[]? requirements)
    {
        if (outputName is null)
        {
            return null;
        }

        if (siblings is null)
        {
            return entityFetch is null
                ? new HierarchyParents(outputName, requirements!)
                : new HierarchyParents(outputName, entityFetch, requirements!);
        }

        return entityFetch is null
            ? new HierarchyParents(outputName, siblings, requirements!)
            : new HierarchyParents(outputName, entityFetch, siblings, requirements!);
    }

    static HierarchyParents? Parents(string? outputName,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyParents(outputName, requirements!);

    static HierarchyParents? Parents(string? outputName, HierarchySiblings? siblings,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : siblings is null
            ? new HierarchyParents(outputName, requirements!)
            : new HierarchyParents(outputName, siblings, requirements!);

    static HierarchyStopAt? StopAt(IHierarchyStopAtRequireConstraint? stopConstraint) => stopConstraint is null
        ? null
        : new HierarchyStopAt(stopConstraint);

    static HierarchyNode? Node(FilterBy? filterBy) => filterBy is null ? null : new HierarchyNode(filterBy);

    static HierarchyLevel? Level(int? level) => level is null ? null : new HierarchyLevel(level.Value);

    static HierarchyDistance? Distance(int? distance) =>
        distance is null ? null : new HierarchyDistance(distance.Value);

    static HierarchyStatistics? Statistics(params StatisticsType[]? types) => types is null
        ? new HierarchyStatistics(StatisticsBase.WithoutUserFilter)
        : new HierarchyStatistics(StatisticsBase.WithoutUserFilter, types);

    static HierarchyStatistics? Statistics(StatisticsBase? statisticsBase, params StatisticsType[]? types) =>
        statisticsBase is null
            ? null
            : types is null
                ? new HierarchyStatistics(statisticsBase.Value)
                : new HierarchyStatistics(statisticsBase.Value, types);

    static EntityFetch EntityFetch(params IEntityContentRequire?[]? requirements) =>
        requirements is null ? new EntityFetch() : new EntityFetch(requirements);

    static EntityGroupFetch EntityGroupFetch(params IEntityContentRequire?[]? requirements) =>
        requirements is null ? new EntityGroupFetch() : new EntityGroupFetch(requirements);

    static AttributeContent AttributeContentAll() => new();

    static AttributeContent AttributeContent(params string[]? attributeNames) =>
        attributeNames is null ? new AttributeContent() : new AttributeContent(attributeNames);

    static AssociatedDataContent AssociatedDataContentAll() => new();

    static AssociatedDataContent AssociatedDataContent(params string[]? associatedDataNames) =>
        associatedDataNames is null ? new AssociatedDataContent() : new AssociatedDataContent(associatedDataNames);

    static DataInLocales DataInLocalesAll() => new();
    
    static DataInLocales DataInLocales(params CultureInfo[]? locales) => locales is null
        ? new DataInLocales()
        : new DataInLocales(locales);

    static ReferenceContent ReferenceContentAll()
    {
        return new ReferenceContent();
    }

    static ReferenceContent ReferenceContentAllWithAttributes()
    {
        return new ReferenceContent((AttributeContent?) null);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent)
    {
        return new ReferenceContent(attributeContent);
    }

    static ReferenceContent ReferenceContent(string? referencedEntityType)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent();
        }

        return new ReferenceContent(referencedEntityType);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType, params string[]? attributeNames)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            AttributeContent(attributeNames), null, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType)
    {
        return new ReferenceContent(referencedEntityType, (FilterBy?) null, null, null, null, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, null, null
        );
    }

    static ReferenceContent ReferenceContent(string[]? referencedEntityType)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent();
        }

        return new ReferenceContent(referencedEntityType);
    }

    static ReferenceContent ReferenceContent(string? referencedEntityType, EntityFetch? entityRequirement)
    {
        if (referencedEntityType == null && entityRequirement == null)
        {
            return new ReferenceContent();
        }

        if (referencedEntityType == null)
        {
            return new ReferenceContent(entityRequirement, null);
        }

        return new ReferenceContent(
            referencedEntityType, null, null, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            null, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContent(string? referencedEntityType, EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityType == null && groupEntityRequirement == null)
        {
            return new ReferenceContent();
        }

        if (referencedEntityType == null)
        {
            return new ReferenceContent(null, groupEntityRequirement);
        }

        return new ReferenceContent(referencedEntityType, null, null, null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            null, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string? referencedEntityType, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent(entityRequirement, groupEntityRequirement);
        }

        return new ReferenceContent(referencedEntityType, null, null, entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(
        string referencedEntityType, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement
    )
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(
        string referencedEntityType, AttributeContent? attributeContent,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement
    )
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string[]? referencedEntityTypes, EntityFetch? entityRequirement)
    {
        if (referencedEntityTypes == null && entityRequirement == null)
        {
            return new ReferenceContent();
        }

        if (referencedEntityTypes == null)
        {
            return new ReferenceContent(entityRequirement, null);
        }

        return new ReferenceContent(
            referencedEntityTypes,
            entityRequirement,
            null
        );
    }

    static ReferenceContent ReferenceContent(string[]? referencedEntityTypes, EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityTypes == null && groupEntityRequirement == null)
        {
            return new ReferenceContent();
        }

        if (referencedEntityTypes == null)
        {
            return new ReferenceContent(null, groupEntityRequirement);
        }

        return new ReferenceContent(
            referencedEntityTypes,
            null,
            groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string[]? referencedEntityTypes, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityTypes != null)
        {
            return new ReferenceContent(referencedEntityTypes, entityRequirement, groupEntityRequirement);
        }

        return new ReferenceContent(entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, null, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(referenceName, filterBy, null, attributeContent, null, null);
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, entityRequirement, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy)
    {
        return new ReferenceContent(referenceName, null, orderBy, null, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, null, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, null, null
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, entityRequirement, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, null, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, null, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, null, null
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, entityRequirement, null);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, entityRequirement, null
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, null, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    static ReferenceContent ReferenceContentAll(EntityFetch? entityRequirement)
    {
        return new ReferenceContent(entityRequirement, null);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(EntityFetch? entityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, entityRequirement, null);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(attributeContent, entityRequirement, null);
    }

    static ReferenceContent ReferenceContentAll(EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, null, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(attributeContent, null, groupEntityRequirement);
    }


    static ReferenceContent ReferenceContentAll(EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, entityRequirement, groupEntityRequirement);
    }

    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(attributeContent, entityRequirement, groupEntityRequirement);
    }

    static HierarchyContent HierarchyContent() => new HierarchyContent();

    static HierarchyContent HierarchyContent(HierarchyStopAt? stopAt) =>
        stopAt is null ? new HierarchyContent() : new HierarchyContent(stopAt);

    static HierarchyContent HierarchyContent(EntityFetch? entityFetch) =>
        entityFetch is null ? new HierarchyContent() : new HierarchyContent(entityFetch);

    static HierarchyContent HierarchyContent(HierarchyStopAt? stopAt, EntityFetch? entityFetch)
    {
        if (stopAt is null && entityFetch is null)
        {
            return new HierarchyContent();
        }

        if (entityFetch is not null)
        {
            return stopAt is null ? new HierarchyContent(entityFetch) : new HierarchyContent(stopAt, entityFetch);
        }

        return new HierarchyContent(stopAt!);
    }

    static PriceContent? PriceContent(PriceContentMode? contentMode, params string[]? priceLists) => contentMode is null
        ? null
        : ArrayUtils.IsEmpty(priceLists)
            ? new PriceContent(contentMode.Value)
            : new PriceContent(contentMode.Value, priceLists!);

    static PriceContent PriceContentAll() => Requires.PriceContent.All();

    static PriceContent PriceContentRespectingFilter(params string[] priceLists) =>
        Requires.PriceContent.RespectingFilter(priceLists);

    static PriceType PriceType(QueryPriceMode priceMode) => new(priceMode);

    static Page Page(int? pageNumber, int? pageSize) => new(pageNumber, pageSize);

    static Strip Strip(int? offset, int? limit) => new(offset, limit);

    static FacetSummary FacetSummary() => new();

    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth) => statisticsDepth is null
        ? new FacetSummary(FacetStatisticsDepth.Counts)
        : new FacetSummary(statisticsDepth.Value);

    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterBy? facetFilterBy,
        OrderBy? facetOrderBy, params IEntityRequire?[]? requirements) => FacetSummary(statisticsDepth, facetFilterBy,
        null, facetOrderBy, null, requirements!);

    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterGroupBy? facetFilterGroupBy,
        OrderGroupBy? facetOrderGroupBy, params IEntityRequire?[]? requirements) => FacetSummary(statisticsDepth, null,
        facetFilterGroupBy, null, facetOrderGroupBy, requirements!);

    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterBy? facetFilterBy,
        FilterGroupBy? facetGroupFilterBy, OrderBy? facetOrderBy, OrderGroupBy? facetGroupOrderBy,
        params IEntityRequire?[]? requirements)
    {
        if (statisticsDepth is null)
        {
            statisticsDepth = FacetStatisticsDepth.Counts;
        }

        if (ArrayUtils.IsEmpty(requirements))
        {
            return new FacetSummary(statisticsDepth.Value, facetFilterBy, facetGroupFilterBy, facetOrderBy,
                facetGroupOrderBy);
        }

        return new FacetSummary(statisticsDepth.Value, facetFilterBy, facetGroupFilterBy, facetOrderBy,
            facetGroupOrderBy, requirements!);
    }

    static FacetSummaryOfReference
        FacetSummaryOfReference(string referenceName, params IEntityRequire[] requirements) =>
        new(referenceName, FacetStatisticsDepth.Counts, requirements);

    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        params IEntityRequire[] requirements) => statisticsDepth is null
        ? new FacetSummaryOfReference(referenceName, FacetStatisticsDepth.Counts, requirements)
        : new FacetSummaryOfReference(referenceName, statisticsDepth.Value, requirements);

    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterBy? facetFilterBy, OrderBy? facetOrderBy, params IEntityRequire[]? requirements) =>
        FacetSummaryOfReference(referenceName, statisticsDepth, facetFilterBy, null, facetOrderBy, null, requirements);

    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterGroupBy? facetGroupFilterBy, OrderGroupBy? facetGroupOrderBy, params IEntityRequire[]? requirements) =>
        FacetSummaryOfReference(referenceName, statisticsDepth, null, facetGroupFilterBy, null, facetGroupOrderBy,
            requirements);

    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterBy? facetFilterBy, FilterGroupBy? facetGroupFilterBy, OrderBy? facetOrderBy,
        OrderGroupBy? facetGroupOrderBy, params IEntityRequire[]? requirements)
    {
        if (statisticsDepth is null)
        {
            statisticsDepth = FacetStatisticsDepth.Counts;
        }

        if (ArrayUtils.IsEmpty(requirements))
        {
            return new FacetSummaryOfReference(referenceName, statisticsDepth.Value, facetFilterBy, facetGroupFilterBy,
                facetOrderBy, facetGroupOrderBy);
        }

        return new FacetSummaryOfReference(referenceName, statisticsDepth.Value, facetFilterBy, facetGroupFilterBy,
            facetOrderBy, facetGroupOrderBy, requirements!);
    }

    static QueryTelemetry QueryTelemetry() => new();
    
    static EntityFetch EntityFetchAll() => EntityFetch(AttributeContentAll(), AssociatedDataContentAll(),
        PriceContentAll(),
        ReferenceContentAllWithAttributes(), DataInLocales());

    static IRequireConstraint?[] EntityFetchAllAnd(params IRequireConstraint?[] combineWith)
    {
        if (ArrayUtils.IsEmpty(combineWith))
        {
            return new IRequireConstraint[] {EntityFetchAll()};
        }

        return EntityFetchAll().Concat(combineWith).ToArray();
    }

    static IEntityContentRequire[] EntityFetchAllContent() => new IEntityContentRequire[]
    {
        AttributeContent(), AssociatedDataContent(), PriceContentAll(), ReferenceContentAllWithAttributes(),
        DataInLocales()
    };

    static Query Query(FilterBy? filter) => new(null, filter, null, null);

    static Query Query(FilterBy? filter, OrderBy? order) => new(null, filter, order, null);

    static Query Query(FilterBy? filter, OrderBy? order, Require? require) => new(null, filter, order, require);

    static Query Query(FilterBy? filter, Require? require) => new(null, filter, null, require);

    static Query Query(Collection? entityType) => new(entityType, null, null, null);
    static Query Query(Collection? entityType, FilterBy? filter) => new(entityType, filter, null, null);

    static Query Query(Collection? entityType, FilterBy? filter, OrderBy? order) =>
        new(entityType, filter, order, null);

    static Query Query(Collection? entityType, FilterBy? filter, OrderBy? order, Require? require) =>
        new(entityType, filter, order, require);

    static Query Query(Collection? entityType, FilterBy? filter, Require? require, OrderBy? order) =>
        new(entityType, filter, order, require);

    static Query Query(Collection? entityType, OrderBy? order) => new(entityType, null, order, null);

    static Query Query(Collection? entityType, OrderBy? order, Require? require) =>
        new(entityType, null, order, require);

    static Query Query(Collection? entityType, FilterBy? filter, Require? require) =>
        new(entityType, filter, null, require);

    static Query Query(Collection? entityType, Require? require) => new(entityType, null, null, require);
}