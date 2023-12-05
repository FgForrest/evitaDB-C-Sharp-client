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

    /// <inheritdoc cref="Client.Queries.Filter.FilterBy"/>
    static FilterBy? FilterBy(params IFilterConstraint?[]? constraint) => constraint == null ? null : new FilterBy(constraint);

    /// <inheritdoc cref="Client.Queries.Filter.FilterGroupBy"/>
    static FilterGroupBy? FilterGroupBy(params IFilterConstraint?[]? constraints) =>
        constraints == null ? null : new FilterGroupBy(constraints);

    /// <inheritdoc cref="Client.Queries.Filter.And"/>
    static And? And(params IFilterConstraint?[]? constraints) => constraints is null ? null : new And(constraints);

    /// <inheritdoc cref="Client.Queries.Filter.Or"/>
    static Or? Or(params IFilterConstraint?[]? constraints) => constraints is null ? null : new Or(constraints);

    /// <inheritdoc cref="Client.Queries.Filter.Not"/>
    static Not? Not(IFilterConstraint? constraint) => constraint is null ? null : new Not(constraint);

    /// <inheritdoc cref="Client.Queries.Filter.ReferenceHaving"/>
    static ReferenceHaving? ReferenceHaving(string? referenceName, params IFilterConstraint?[]? constraints) =>
        referenceName is null ? null : new ReferenceHaving(referenceName, constraints!);

    /// <inheritdoc cref="Client.Queries.Filter.UserFilter"/>
    static UserFilter? UserFilter(params IFilterConstraint?[]? constraints) =>
        constraints is null ? null : new UserFilter(constraints);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeBetween{T}"/>
    static AttributeBetween<T>? AttributeBetween<T>(string attributeName, T? from, T? to) where T : IComparable =>
        from is null && to is null ? null : new AttributeBetween<T>(attributeName, from, to);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeContains"/>
    static AttributeContains? AttributeContains(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeContains(attributeName, textToSearch);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeStartsWith"/>
    static AttributeStartsWith? AttributeStartsWith(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeStartsWith(attributeName, textToSearch);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeEndsWith"/>
    static AttributeEndsWith? AttributeEndsWith(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeEndsWith(attributeName, textToSearch);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeEquals{T}"/>
    static AttributeEquals<T>? AttributeEquals<T>(string attributeName, T? attributeValue) =>
        attributeValue is null ? null : new AttributeEquals<T>(attributeName, attributeValue);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeLessThan{T}"/>
    static AttributeLessThan<T>? AttributeLessThan<T>(string attributeName, T? attributeValue) where T : IComparable =>
        attributeValue is null ? null : new AttributeLessThan<T>(attributeName, attributeValue);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeLessThanEquals{T}"/>
    static AttributeLessThanEquals<T>? AttributeLessThanEquals<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeLessThanEquals<T>(attributeName, attributeValue);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeGreaterThan{T}"/>
    static AttributeGreaterThan<T>? AttributeGreaterThan<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeGreaterThan<T>(attributeName, attributeValue);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeGreaterThanEquals{T}"/>
    static AttributeGreaterThanEquals<T>? AttributeGreaterThanEquals<T>(string attributeName, T? attributeValue)
        where T : IComparable =>
        attributeValue is null ? null : new AttributeGreaterThanEquals<T>(attributeName, attributeValue);

    
    /// <inheritdoc cref="Client.Queries.Filter.PriceInPriceLists"/>
    static PriceInPriceLists? PriceInPriceLists(params string[]? priceListNames) =>
        priceListNames is null ? null : new PriceInPriceLists(priceListNames);

    /// <inheritdoc cref="Client.Queries.Filter.PriceInCurrency"/>
    static PriceInCurrency? PriceInCurrency(string? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    /// <inheritdoc cref="Client.Queries.Filter.PriceInCurrency"/>
    static PriceInCurrency? PriceInCurrency(Currency? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyWithin"/>
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

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyWithin"/>
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

    
    /// <inheritdoc cref="Client.Queries.Filter.HierarchyWithinRoot"/>
    static HierarchyWithinRoot HierarchyWithinRootSelf(params IHierarchySpecificationFilterConstraint?[]? with) =>
        with is null ? new HierarchyWithinRoot() : new HierarchyWithinRoot(with);

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyWithinRoot"/>
    static HierarchyWithinRoot HierarchyWithinRoot(string referenceName,
        params IHierarchySpecificationFilterConstraint?[]? with) =>
        with is null ? new HierarchyWithinRoot() : new HierarchyWithinRoot(referenceName, with);


    /// <inheritdoc cref="Client.Queries.Filter.HierarchyHaving"/>
    static HierarchyHaving? Having(params IFilterConstraint?[]? includeChildTreeConstraints) =>
        ArrayUtils.IsEmpty(includeChildTreeConstraints) ? null : new HierarchyHaving(includeChildTreeConstraints!);

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyExcluding"/>
    static HierarchyExcluding? Excluding(params IFilterConstraint[]? excludeChildTreeConstraints) =>
        ArrayUtils.IsEmpty(excludeChildTreeConstraints) ? null : new HierarchyExcluding(excludeChildTreeConstraints!);

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyDirectRelation"/>
    static HierarchyDirectRelation DirectRelation() => new HierarchyDirectRelation();

    /// <inheritdoc cref="Client.Queries.Filter.HierarchyExcludingRoot"/>
    static HierarchyExcludingRoot ExcludingRoot() => new HierarchyExcludingRoot();

    /// <inheritdoc cref="Client.Queries.Filter.EntityLocaleEquals"/>
    static EntityLocaleEquals? EntityLocaleEquals(CultureInfo? locale) =>
        locale is null ? null : new EntityLocaleEquals(locale);

    /// <inheritdoc cref="Client.Queries.Filter.EntityHaving"/>
    static EntityHaving? EntityHaving(IFilterConstraint? filterConstraint) =>
        filterConstraint is null ? null : new EntityHaving(filterConstraint);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{DateTimeOffset}"/>
    static AttributeInRange<DateTimeOffset>?
        AttributeInRange(string attributeName, DateTimeOffset? dateTimeOffsetValue) =>
        dateTimeOffsetValue is null
            ? null
            : new AttributeInRange<DateTimeOffset>(attributeName, dateTimeOffsetValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{Int32}"/>
    static AttributeInRange<int>? AttributeInRange(string attributeName, int? intValue) =>
        intValue is null ? null : new AttributeInRange<int>(attributeName, intValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{Int64}"/>
    static AttributeInRange<long>? AttributeInRange(string attributeName, long? longValue) =>
        longValue is null ? null : new AttributeInRange<long>(attributeName, longValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{Int16}"/>
    static AttributeInRange<short>? AttributeInRange(string attributeName, short? shortValue) =>
        shortValue is null ? null : new AttributeInRange<short>(attributeName, shortValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{Byte}"/>
    static AttributeInRange<byte>? AttributeInRange(string attributeName, byte? byteValue) =>
        byteValue is null ? null : new AttributeInRange<byte>(attributeName, byteValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{Decimal}"/>
    static AttributeInRange<decimal>? AttributeInRange(string attributeName, decimal? decimalValue) =>
        decimalValue is null ? null : new AttributeInRange<decimal>(attributeName, decimalValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInRange{DateTimeOffset}"/>
    static AttributeInRange<DateTimeOffset> AttributeInRangeNow(string attributeName) => new(attributeName);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeInSet{T}"/>
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

    /// <inheritdoc cref="Client.Queries.Filter.AttributeEquals{Bool}"/>
    static AttributeEquals<bool> AttributeEqualsFalse(string attributeName) =>
        new AttributeEquals<bool>(attributeName, false);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeEquals{Bool}"/>
    static AttributeEquals<bool> AttributeEqualsTrue(string attributeName) =>
        new AttributeEquals<bool>(attributeName, true);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeIs"/>
    static AttributeIs? AttributeIs(string attributeName, AttributeSpecialValue? specialValue) =>
        specialValue is null ? null : new AttributeIs(attributeName, specialValue.Value);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeIs"/>
    static AttributeIs AttributeIsNull(string attributeName) => new(attributeName, AttributeSpecialValue.Null);

    /// <inheritdoc cref="Client.Queries.Filter.AttributeIs"/>
    static AttributeIs AttributeIsNotNull(string attributeName) => new(attributeName, AttributeSpecialValue.NotNull);

    /// <inheritdoc cref="Client.Queries.Filter.PriceBetween"/>
    static PriceBetween? PriceBetween(decimal? minPrice, decimal? maxPrice) =>
        minPrice is null && maxPrice is null ? null : new PriceBetween(minPrice, maxPrice);
    
    /// <inheritdoc cref="Client.Queries.Filter.PriceValidIn"/>
    static PriceValidIn? PriceValidIn(DateTimeOffset? theMoment) =>
        theMoment is null ? null : new PriceValidIn(theMoment.Value);

    /// <inheritdoc cref="Client.Queries.Filter.PriceValidIn"/>
    static PriceValidIn PriceValidInNow() => new();
    
    /// <inheritdoc cref="Client.Queries.Filter.FacetHaving"/>
    static FacetHaving? FacetHaving(string referenceName, params IFilterConstraint?[]? constraints) =>
        ArrayUtils.IsEmpty(constraints) ? null : new FacetHaving(referenceName, constraints!);


    /// <inheritdoc cref="Client.Queries.Filter.EntityPrimaryKeyInSet"/>
    static EntityPrimaryKeyInSet? EntityPrimaryKeyInSet(params int[]? primaryKeys) =>
        primaryKeys == null ? null : new EntityPrimaryKeyInSet(primaryKeys);

    /// <inheritdoc cref="Client.Queries.Order.OrderBy"/>
    static OrderBy? OrderBy(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new OrderBy(constraints);

    /// <inheritdoc cref="Client.Queries.Order.OrderGroupBy"/>
    static OrderGroupBy? OrderGroupBy(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new OrderGroupBy(constraints);
    
    /// <inheritdoc cref="Client.Queries.Order.EntityPrimaryKeyNatural"/>
    static EntityPrimaryKeyNatural EntityPrimaryKeyNatural(OrderDirection? direction) {
        return new EntityPrimaryKeyNatural(direction ?? OrderDirection.Asc);
    }
    
    /// <inheritdoc cref="Client.Queries.Order.EntityPrimaryKeyInFilter"/>
    static EntityPrimaryKeyInFilter EntityPrimaryKeyInFilter() => new EntityPrimaryKeyInFilter();

    /// <inheritdoc cref="Client.Queries.Order.EntityPrimaryKeyExact"/>
    static EntityPrimaryKeyExact? EntityPrimaryKeyExact(params int[]? primaryKeys) =>
        ArrayUtils.IsEmpty(primaryKeys) ? null : new EntityPrimaryKeyExact(primaryKeys!);

    /// <inheritdoc cref="Client.Queries.Order.AttributeSetInFilter"/>
    static AttributeSetInFilter? AttributeSetInFilter(string? attributeName) =>
        string.IsNullOrEmpty(attributeName) ? null : new AttributeSetInFilter(attributeName);

    /// <inheritdoc cref="Client.Queries.Order.AttributeSetExact"/>
    static AttributeSetExact? AttributeSetExact(string? attributeName, params object[]? attributeValues) =>
        ArrayUtils.IsEmpty(attributeValues) || string.IsNullOrEmpty(attributeName)
            ? null
            : new AttributeSetExact(attributeName, attributeValues!);
    
    /// <inheritdoc cref="Client.Queries.Order.ReferenceProperty"/>
    static ReferenceProperty? ReferenceProperty(string propertyName, params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new ReferenceProperty(propertyName, constraints);

    /// <inheritdoc cref="Client.Queries.Order.EntityProperty"/>
    static EntityProperty? EntityProperty(params IOrderConstraint?[]? constraints) =>
        constraints is null ? null : new EntityProperty(constraints);

    /// <inheritdoc cref="Client.Queries.Order.EntityGroupProperty"/>
    static EntityGroupProperty? EntityGroupProperty(params IOrderConstraint?[]? constraints) =>
        constraints == null ? null : new EntityGroupProperty(constraints);

    /// <inheritdoc cref="Client.Queries.Order.AttributeNatural"/>
    static AttributeNatural AttributeNatural(string attributeName) => new(attributeName);

    /// <inheritdoc cref="Client.Queries.Order.AttributeNatural"/>
    static AttributeNatural AttributeNatural(string attributeName, OrderDirection orderDirection) =>
        new AttributeNatural(attributeName, orderDirection);

    /// <inheritdoc cref="Client.Queries.Order.PriceNatural"/>
    static PriceNatural PriceNatural() => new();

    /// <inheritdoc cref="Client.Queries.Order.PriceNatural"/>
    static PriceNatural PriceNatural(OrderDirection orderDirection) => new(orderDirection);

    /// <inheritdoc cref="Client.Queries.Order.Random"/>
    static Random Random() => new();

    /// <inheritdoc cref="Client.Queries.Requires.Require"/>
    static Require? Require(params IRequireConstraint?[]? constraints) =>
        constraints is null ? null : new Require(constraints);

    /// <inheritdoc cref="Client.Queries.Requires.AttributeHistogram"/>
    static AttributeHistogram? AttributeHistogram(int requestedBucketCount, params string[]? attributeNames) =>
        ArrayUtils.IsEmpty(attributeNames) ? null : new AttributeHistogram(requestedBucketCount, attributeNames!);

    /// <inheritdoc cref="Client.Queries.Requires.PriceHistogram"/>
    static PriceHistogram PriceHistogram(int requestedBucketCount) => new(requestedBucketCount);

    /// <inheritdoc cref="Client.Queries.Requires.FacetGroupsConjunction"/>
    static FacetGroupsConjunction? FacetGroupsConjunction(string? referenceName, FilterBy? filterBy = null) =>
        referenceName is null ? null : new FacetGroupsConjunction(referenceName, filterBy);

    /// <inheritdoc cref="Client.Queries.Requires.FacetGroupsDisjunction"/>
    static FacetGroupsDisjunction? FacetGroupsDisjunction(string? referenceName, FilterBy? filterBy = null) =>
        referenceName is null ? null : new FacetGroupsDisjunction(referenceName, filterBy);

    /// <inheritdoc cref="Client.Queries.Requires.FacetGroupsNegation"/>
    static FacetGroupsNegation? FacetGroupsNegation(string? referenceName, FilterBy? filterBy = null) =>
        referenceName is null ? null : new FacetGroupsNegation(referenceName, filterBy);
    
    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfSelf"/>
    static HierarchyOfSelf? HierarchyOfSelf(params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(requirements) ? null : new HierarchyOfSelf(null, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfSelf"/>
    static HierarchyOfSelf? HierarchyOfSelf(OrderBy? orderBy, params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(requirements) ? null : new HierarchyOfSelf(orderBy, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string referenceName,
        params IHierarchyRequireConstraint?[]? requirements) =>
        HierarchyOfReference(referenceName, null, null, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string referenceName, OrderBy orderBy,
        params IHierarchyRequireConstraint?[]? requirements) =>
        HierarchyOfReference(referenceName, null, orderBy, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string? referenceName,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[]? requirements) =>
        referenceName is null || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceName,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        params IHierarchyRequireConstraint[] requirements) =>
        HierarchyOfReference(referenceNames, null, null, requirements);
    
    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames, OrderBy? orderBy,
        params IHierarchyRequireConstraint[] requirements) =>
        HierarchyOfReference(referenceNames, null, orderBy, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour,
        params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(referenceNames) || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceNames!,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyOfReference"/>
    static HierarchyOfReference? HierarchyOfReference(string[]? referenceNames,
        EmptyHierarchicalEntityBehaviour? emptyHierarchicalEntityBehaviour, OrderBy? orderBy,
        params IHierarchyRequireConstraint?[]? requirements) =>
        ArrayUtils.IsEmpty(referenceNames) || ArrayUtils.IsEmpty(requirements)
            ? null
            : new HierarchyOfReference(referenceNames!,
                emptyHierarchicalEntityBehaviour ?? EmptyHierarchicalEntityBehaviour.RemoveEmpty, orderBy,
                requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyFromRoot"/>
    static HierarchyFromRoot? FromRoot(string? outputName, params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null :
        requirements is null ? new HierarchyFromRoot(outputName) : new HierarchyFromRoot(outputName, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyFromRoot"/>
    static HierarchyFromRoot? FromRoot(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null :
        requirements is null ? new HierarchyFromRoot(outputName, entityFetch) :
        new HierarchyFromRoot(outputName, entityFetch, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyFromNode"/>
    static HierarchyFromNode? FromNode(string? outputName, HierarchyNode? node,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null || node is null ? null :
        requirements is null ? new HierarchyFromNode(outputName, node) :
        new HierarchyFromNode(outputName, node, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyFromNode"/>
    static HierarchyFromNode? FromNode(string? outputName, HierarchyNode? node, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null || node is null ? null :
        entityFetch is null ? new HierarchyFromNode(outputName, node) :
        new HierarchyFromNode(outputName, node, entityFetch, requirements!);
    
    /// <inheritdoc cref="Client.Queries.Requires.HierarchyChildren"/>
    static HierarchyChildren? Children(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyChildren(outputName, entityFetch, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyChildren"/>
    static HierarchyChildren? Children(string? outputName, params IHierarchyOutputRequireConstraint?[]? requirements) =>
        outputName is null ? null 
            : new HierarchyChildren(outputName, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchySiblings"/>
    static HierarchySiblings? Siblings(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchySiblings(outputName, entityFetch, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchySiblings"/>
    static HierarchySiblings? Siblings(string? outputName,
        params IHierarchyOutputRequireConstraint[]? requirements) => outputName is null
        ? null
        : new HierarchySiblings(outputName, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchySiblings"/>
    static HierarchySiblings Siblings(EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) =>
        new(null, entityFetch, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchySiblings"/>
    static HierarchySiblings Siblings(params IHierarchyOutputRequireConstraint?[]? requirements) =>
        new(null, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyParents"/>
    static HierarchyParents? Parents(string? outputName, EntityFetch? entityFetch,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyParents(outputName, entityFetch, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyParents"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyParents"/>
    static HierarchyParents? Parents(string? outputName,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : new HierarchyParents(outputName, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyParents"/>
    static HierarchyParents? Parents(string? outputName, HierarchySiblings? siblings,
        params IHierarchyOutputRequireConstraint?[]? requirements) => outputName is null
        ? null
        : siblings is null
            ? new HierarchyParents(outputName, requirements!)
            : new HierarchyParents(outputName, siblings, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyStopAt"/>
    static HierarchyStopAt? StopAt(IHierarchyStopAtRequireConstraint? stopConstraint) => stopConstraint is null
        ? null
        : new HierarchyStopAt(stopConstraint);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyNode"/>
    static HierarchyNode? Node(FilterBy? filterBy) => filterBy is null ? null : new HierarchyNode(filterBy);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyLevel"/>
    static HierarchyLevel? Level(int? level) => level is null ? null : new HierarchyLevel(level.Value);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyDistance"/>
    static HierarchyDistance? Distance(int? distance) =>
        distance is null ? null : new HierarchyDistance(distance.Value);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyStatistics"/>
    static HierarchyStatistics Statistics(params StatisticsType[]? types) => types is null
        ? new HierarchyStatistics(StatisticsBase.WithoutUserFilter)
        : new HierarchyStatistics(StatisticsBase.WithoutUserFilter, types);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyStatistics"/>
    static HierarchyStatistics? Statistics(StatisticsBase? statisticsBase, params StatisticsType[]? types) =>
        statisticsBase is null
            ? null
            : types is null
                ? new HierarchyStatistics(statisticsBase.Value)
                : new HierarchyStatistics(statisticsBase.Value, types);

    /// <inheritdoc cref="Client.Queries.Requires.EntityFetch"/>
    static EntityFetch EntityFetch(params IEntityContentRequire?[]? requirements) =>
        requirements is null ? new EntityFetch() : new EntityFetch(requirements);

    /// <inheritdoc cref="Client.Queries.Requires.EntityGroupFetch"/>
    static EntityGroupFetch EntityGroupFetch(params IEntityContentRequire?[]? requirements) =>
        requirements is null ? new EntityGroupFetch() : new EntityGroupFetch(requirements);

    /// <inheritdoc cref="Client.Queries.Requires.AttributeContent"/>
    static AttributeContent AttributeContentAll() => new();

    /// <inheritdoc cref="Client.Queries.Requires.AttributeContent"/>
    static AttributeContent AttributeContent(params string[]? attributeNames) =>
        attributeNames is null ? new AttributeContent() : new AttributeContent(attributeNames);

    /// <inheritdoc cref="Client.Queries.Requires.AssociatedDataContent"/>
    static AssociatedDataContent AssociatedDataContentAll() => new();

    /// <inheritdoc cref="Client.Queries.Requires.AssociatedDataContent"/>
    static AssociatedDataContent AssociatedDataContent(params string[]? associatedDataNames) =>
        associatedDataNames is null ? new AssociatedDataContent() : new AssociatedDataContent(associatedDataNames);

    /// <inheritdoc cref="Client.Queries.Requires.DataInLocales"/>
    static DataInLocales DataInLocalesAll() => new();
    
    /// <inheritdoc cref="Client.Queries.Requires.DataInLocales"/>
    static DataInLocales DataInLocales(params CultureInfo[]? locales) => locales is null
        ? new DataInLocales()
        : new DataInLocales(locales);
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAll()
    {
        return new ReferenceContent();
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes()
    {
        return new ReferenceContent((AttributeContent?) null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent)
    {
        return new ReferenceContent(attributeContent);
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string? referencedEntityType)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent();
        }

        return new ReferenceContent(referencedEntityType);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType, params string[]? attributeNames)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            AttributeContent(attributeNames), null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType)
    {
        return new ReferenceContent(referencedEntityType, (FilterBy?) null, null, null, null, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string[]? referencedEntityType)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent();
        }

        return new ReferenceContent(referencedEntityType);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            null, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            null, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referencedEntityType,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            attributeContent, null, groupEntityRequirement
        );
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string? referencedEntityType, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityType == null)
        {
            return new ReferenceContent(entityRequirement, groupEntityRequirement);
        }

        return new ReferenceContent(referencedEntityType, null, null, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(
        string referencedEntityType, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement
    )
    {
        return new ReferenceContent(
            referencedEntityType, null, null,
            entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string[]? referencedEntityTypes, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        if (referencedEntityTypes != null)
        {
            return new ReferenceContent(referencedEntityTypes, entityRequirement, groupEntityRequirement);
        }

        return new ReferenceContent(entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, null);
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, null, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(referenceName, filterBy, null, attributeContent, null, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, entityRequirement, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, null, groupEntityRequirement);
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, null, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            null, entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, null,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy)
    {
        return new ReferenceContent(referenceName, null, orderBy, null, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, entityRequirement, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, null, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, OrderBy? orderBy, EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, null, orderBy, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            null, entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, null, orderBy,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, null, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, null, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, entityRequirement, null);
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, entityRequirement, null
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, null, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, null, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            null, entityRequirement, groupEntityRequirement
        );
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentWithAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(
            referenceName, filterBy, orderBy,
            attributeContent, entityRequirement, groupEntityRequirement
        );
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAll(EntityFetch? entityRequirement)
    {
        return new ReferenceContent(entityRequirement, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(EntityFetch? entityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, entityRequirement, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityFetch? entityRequirement)
    {
        return new ReferenceContent(attributeContent, entityRequirement, null);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAll(EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(null, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, null, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(attributeContent, null, groupEntityRequirement);
    }
    
    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAll(EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(EntityFetch? entityRequirement,
        EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent((AttributeContent?) null, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.ReferenceContent"/>
    static ReferenceContent ReferenceContentAllWithAttributes(AttributeContent? attributeContent,
        EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement)
    {
        return new ReferenceContent(attributeContent, entityRequirement, groupEntityRequirement);
    }

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyContent"/>
    static HierarchyContent HierarchyContent() => new HierarchyContent();
    
    /// <inheritdoc cref="Client.Queries.Requires.HierarchyContent"/>
    static HierarchyContent HierarchyContent(HierarchyStopAt? stopAt) =>
        stopAt is null ? new HierarchyContent() : new HierarchyContent(stopAt);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyContent"/>
    static HierarchyContent HierarchyContent(EntityFetch? entityFetch) =>
        entityFetch is null ? new HierarchyContent() : new HierarchyContent(entityFetch);

    /// <inheritdoc cref="Client.Queries.Requires.HierarchyContent"/>
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

    /// <inheritdoc cref="Client.Queries.Requires.PriceContent"/>
    static PriceContent? PriceContent(PriceContentMode? contentMode, params string[]? priceLists) => contentMode is null
        ? null
        : ArrayUtils.IsEmpty(priceLists)
            ? new PriceContent(contentMode.Value)
            : new PriceContent(contentMode.Value, priceLists!);
    
    /// <inheritdoc cref="Client.Queries.Requires.PriceContent"/>
    static PriceContent PriceContentAll() => Requires.PriceContent.All();

    /// <inheritdoc cref="Client.Queries.Requires.PriceContent"/>
    static PriceContent PriceContentRespectingFilter(params string[] priceLists) =>
        Requires.PriceContent.RespectingFilter(priceLists);

    /// <inheritdoc cref="Client.Queries.Requires.PriceType"/>
    static PriceType PriceType(QueryPriceMode priceMode) => new(priceMode);
    
    /// <inheritdoc cref="Client.Queries.Requires.Page"/>
    static Page Page(int? pageNumber, int? pageSize) => new(pageNumber, pageSize);

    /// <inheritdoc cref="Client.Queries.Requires.Strip"/>
    static Strip Strip(int? offset, int? limit) => new(offset, limit);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary() => new();
    
    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth) => statisticsDepth is null
        ? new FacetSummary(FacetStatisticsDepth.Counts)
        : new FacetSummary(statisticsDepth.Value);
    
    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, params IEntityRequire?[]? requirements) => 
        statisticsDepth is null
        ? new FacetSummary(FacetStatisticsDepth.Counts, requirements!)
        : new FacetSummary(statisticsDepth.Value, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterBy? facetFilterBy,
        OrderBy? facetOrderBy, params IEntityRequire?[]? requirements) => FacetSummary(statisticsDepth, facetFilterBy,
        null, facetOrderBy, null, requirements!);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterGroupBy? facetFilterGroupBy,
        OrderGroupBy? facetOrderGroupBy, params IEntityRequire?[]? requirements) => FacetSummary(statisticsDepth, null,
        facetFilterGroupBy, null, facetOrderGroupBy, requirements!);
    
    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		FilterGroupBy? facetGroupFilterBy,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, filterBy, facetGroupFilterBy, orderBy, null, requirements);
	}
    
    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		FilterGroupBy? facetGroupFilterBy,
		params IEntityRequire[] requirements
	) {
		return FacetSummary(statisticsDepth, filterBy, facetGroupFilterBy, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		OrderBy? orderBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, null, null, orderBy, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, filterBy, null, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, null, null, orderBy, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterGroupBy? facetGroupFilterBy,
		params IEntityRequire[] requirements
	) {
		return FacetSummary(statisticsDepth, null, facetGroupFilterBy, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, null, null, null, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterGroupBy? facetGroupFilterBy,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return FacetSummary(statisticsDepth, null, facetGroupFilterBy, orderBy, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
	static FacetSummary FacetSummary(
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
        return FacetSummary(statisticsDepth, filterBy, null, null, facetGroupOrderBy, requirements);
    }

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummary"/>
    static FacetSummary FacetSummary(FacetStatisticsDepth? statisticsDepth, FilterBy? facetFilterBy,
        FilterGroupBy? facetGroupFilterBy, OrderBy? facetOrderBy, OrderGroupBy? facetGroupOrderBy,
        params IEntityRequire?[]? requirements)
    {
        statisticsDepth ??= FacetStatisticsDepth.Counts;

        if (ArrayUtils.IsEmpty(requirements))
        {
            return new FacetSummary(statisticsDepth.Value, facetFilterBy, facetGroupFilterBy, facetOrderBy,
                facetGroupOrderBy);
        }

        return new FacetSummary(statisticsDepth.Value, facetFilterBy, facetGroupFilterBy, facetOrderBy,
            facetGroupOrderBy, requirements!);
    }

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, params IEntityRequire[] requirements) =>
        new(referenceName, FacetStatisticsDepth.Counts, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        params IEntityRequire[] requirements) => statisticsDepth is null
        ? new FacetSummaryOfReference(referenceName, FacetStatisticsDepth.Counts, requirements)
        : new FacetSummaryOfReference(referenceName, statisticsDepth.Value, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterBy? facetFilterBy, OrderBy? facetOrderBy, params IEntityRequire[]? requirements) =>
        FacetSummaryOfReference(referenceName, statisticsDepth, facetFilterBy, null, facetOrderBy, null, requirements);

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterGroupBy? facetGroupFilterBy, OrderGroupBy? facetGroupOrderBy, params IEntityRequire[]? requirements) =>
        FacetSummaryOfReference(referenceName, statisticsDepth, null, facetGroupFilterBy, null, facetGroupOrderBy,
            requirements);
    
    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
        string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		FilterGroupBy? facetGroupFilterBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, facetGroupFilterBy, null, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterGroupBy? facetGroupFilterBy,
		OrderBy? orderBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, facetGroupFilterBy, orderBy, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		OrderBy? orderBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, null, orderBy, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		FilterGroupBy? facetGroupFilterBy,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, facetGroupFilterBy, orderBy, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		FilterGroupBy? facetGroupFilterBy,
		params IEntityRequire[] requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, facetGroupFilterBy, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		OrderBy? orderBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, null, orderBy, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, null, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, null, orderBy, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterGroupBy? facetGroupFilterBy,
		params IEntityRequire[] requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, facetGroupFilterBy, null, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, null, null, facetGroupOrderBy, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterGroupBy? facetGroupFilterBy,
		OrderBy? orderBy,
		params IEntityRequire[]? requirements
	) {
		return referenceName == null ? null :
			FacetSummaryOfReference(referenceName, statisticsDepth, null, facetGroupFilterBy, orderBy, null, requirements);
	}

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
	static FacetSummaryOfReference? FacetSummaryOfReference(
		string? referenceName,
		FacetStatisticsDepth? statisticsDepth,
		FilterBy? filterBy,
		OrderGroupBy? facetGroupOrderBy,
		params IEntityRequire[]? requirements
	) {
        return referenceName == null
            ? null
            : FacetSummaryOfReference(referenceName, statisticsDepth, filterBy, null, null, facetGroupOrderBy,
                requirements);
    }

    /// <inheritdoc cref="Client.Queries.Requires.FacetSummaryOfReference"/>
    static FacetSummaryOfReference FacetSummaryOfReference(string referenceName, FacetStatisticsDepth? statisticsDepth,
        FilterBy? facetFilterBy, FilterGroupBy? facetGroupFilterBy, OrderBy? facetOrderBy,
        OrderGroupBy? facetGroupOrderBy, params IEntityRequire[]? requirements)
    {
        statisticsDepth ??= FacetStatisticsDepth.Counts;

        if (ArrayUtils.IsEmpty(requirements))
        {
            return new FacetSummaryOfReference(referenceName, statisticsDepth.Value, facetFilterBy, facetGroupFilterBy,
                facetOrderBy, facetGroupOrderBy);
        }

        return new FacetSummaryOfReference(referenceName, statisticsDepth.Value, facetFilterBy, facetGroupFilterBy,
            facetOrderBy, facetGroupOrderBy, requirements!);
    }

    /// <inheritdoc cref="Client.Queries.Requires.QueryTelemetry"/>
    static QueryTelemetry QueryTelemetry() => new();
    
    /// <inheritdoc cref="Client.Queries.Requires.EntityFetch"/>
    static EntityFetch EntityFetchAll() => EntityFetch(AttributeContentAll(), AssociatedDataContentAll(),
        PriceContentAll(),
        ReferenceContentAllWithAttributes(), DataInLocales());

    /// <summary>
    /// This method returns array of all requirements that are necessary to load full content of the entity including
    /// all language specific attributes, all prices, all references and all associated data.
    /// </summary>
    static IRequireConstraint?[] EntityFetchAllAnd(params IRequireConstraint?[] combineWith)
    {
        if (ArrayUtils.IsEmpty(combineWith))
        {
            return new IRequireConstraint[] {EntityFetchAll()};
        }

        return EntityFetchAll().Concat(combineWith).ToArray();
    }

    /// <summary>
    /// This interface marks all requirements that can be used for loading additional data to existing entity.
    /// </summary>
    static IEntityContentRequire[] EntityFetchAllContent() => new IEntityContentRequire[]
    {
        AttributeContent(), AssociatedDataContent(), PriceContentAll(), ReferenceContentAllWithAttributes(),
        DataInLocales()
    };

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(FilterBy? filter) => new(null, filter, null, null);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(FilterBy? filter, OrderBy? order) => new(null, filter, order, null);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(FilterBy? filter, OrderBy? order, Require? require) => new(null, filter, order, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(FilterBy? filter, Require? require) => new(null, filter, null, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType) => new(entityType, null, null, null);
    
    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, FilterBy? filter) => new(entityType, filter, null, null);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, FilterBy? filter, OrderBy? order) =>
        new(entityType, filter, order, null);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, FilterBy? filter, OrderBy? order, Require? require) =>
        new(entityType, filter, order, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, FilterBy? filter, Require? require, OrderBy? order) =>
        new(entityType, filter, order, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, OrderBy? order) => new(entityType, null, order, null);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, OrderBy? order, Require? require) =>
        new(entityType, null, order, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, FilterBy? filter, Require? require) =>
        new(entityType, filter, null, require);

    /// <inheritdoc cref="Client.Queries.Query"/>
    static Query Query(Collection? entityType, Require? require) => new(entityType, null, null, require);
}
