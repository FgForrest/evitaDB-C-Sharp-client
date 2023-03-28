using System.Globalization;
using Client.DataTypes;
using Client.Queries.Filter;
using Client.Queries.Head;
using Client.Queries.Order;
using Client.Queries.Requires;
using Random = Client.Queries.Order.Random;

namespace Client.Queries;

public interface IQueryConstraints
{
    static Collection Collection(string entityType) => new(entityType);

    static FilterBy? FilterBy(IFilterConstraint? constraint) => constraint == null ? null : new FilterBy(constraint);

    static EntityPrimaryKeyInSet? EntityPrimaryKeyInSet(params int[]? primaryKeys) =>
        primaryKeys == null ? null : new EntityPrimaryKeyInSet(primaryKeys);

    static And? And(params IFilterConstraint?[]? constraints) => constraints is null ? null : new And(constraints);

    static Or? Or(params IFilterConstraint?[]? constraints) => constraints is null ? null : new Or(constraints);

    static Not? Not(IFilterConstraint? constraint) => constraint is null ? null : new Not(constraint);

    static AttributeContains? AttributeContains(string attributeName, string? textToSearch) =>
        textToSearch is null ? null : new AttributeContains(attributeName, textToSearch);

    static PriceInPriceLists? PriceInPriceLists(params string[]? priceListNames) =>
        priceListNames is null ? null : new PriceInPriceLists(priceListNames);

    static PriceBetween? PriceBetween(decimal? minPrice, decimal? maxPrice) =>
        minPrice is null && maxPrice is null ? null : new PriceBetween(minPrice, maxPrice);

    static PriceValidIn? PriceValidIn(DateTimeOffset? theMoment) =>
        theMoment is null ? null : new PriceValidIn(theMoment.Value);

    static PriceValidIn PriceValidNow() => new PriceValidIn();

    static PriceInCurrency? PriceInCurrency(string? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    static PriceInCurrency? PriceInCurrency(Currency? currency) =>
        currency is null ? null : new PriceInCurrency(currency);

    static OrderBy? OrderBy(params IOrderConstraint[]? constraints) =>
        constraints is null ? null : new OrderBy(constraints);

    static ReferenceProperty? ReferenceProperty(string propertyName, params IOrderConstraint[]? constraints) =>
        constraints is null ? null : new ReferenceProperty(propertyName);

    static EntityProperty? EntityProperty(params IOrderConstraint[]? constraints) =>
        constraints is null ? null : new EntityProperty(constraints);

    static AttributeNatural AttributeNatural(string attributeName) => new AttributeNatural(attributeName);

    static AttributeNatural AttributeNatural(string attributeName, OrderDirection orderDirection) =>
        new AttributeNatural(attributeName, orderDirection);

    static PriceNatural PriceNatural() => new PriceNatural();

    static PriceNatural PriceNatural(OrderDirection orderDirection) => new PriceNatural(orderDirection);

    static Random Random() => new Random();

    static Require? Require(params IRequireConstraint[]? constraints) =>
        constraints is null ? null : new Require(constraints);

    static EntityFetch EntityFetch(params IEntityContentRequire[]? requirements) =>
        requirements is null ? new EntityFetch() : new EntityFetch(requirements);

    static AttributeContent AttributeContent(params string[]? attributeNames) =>
        attributeNames is null ? new AttributeContent() : new AttributeContent(attributeNames);

    static AssociatedDataContent AssociatedDataContent(params string[]? associatedDataNames) =>
        associatedDataNames is null ? new AssociatedDataContent() : new AssociatedDataContent(associatedDataNames);

    static DataInLocales DataInLocales(params CultureInfo[]? locales) => locales is null
        ? new DataInLocales()
        : new DataInLocales(locales);

    static ReferenceContent ReferenceContent() => new ReferenceContent();

    static ReferenceContent ReferenceContent(string? referencedEntityType) => referencedEntityType is null
        ? new ReferenceContent()
        : new ReferenceContent(referencedEntityType);

    static PriceContent PriceContent(params string[]? priceLists) => priceLists is null || priceLists.Length == 0
        ? new PriceContent(PriceContentMode.RespectingFilter)
        : new PriceContent(PriceContentMode.RespectingFilter, priceLists);

    static PriceContent PriceContentAll() => new PriceContent(PriceContentMode.All);

    static Page Page(int? pageNumber, int? pageSize) => new Page(pageNumber, pageSize);

    static Strip Strip(int? offset, int? limit) => new Strip(offset, limit);

    static QueryTelemetry QueryTelemetry() => new QueryTelemetry();

    static EntityFetch EntityFetchAll() => EntityFetch(AttributeContent(), AssociatedDataContent(), PriceContentAll(),
        ReferenceContent(), DataInLocales());

    static Query Query(FilterBy? filter) => new Query(null, filter, null, null);

    static Query Query(FilterBy? filter, OrderBy? order) => new Query(null, filter, order, null);

    static Query Query(FilterBy? filter, OrderBy? order, Require? require) => new Query(null, filter, order, require);

    static Query Query(FilterBy? filter, Require? require) => new Query(null, filter, null, require);

    static Query Query(Collection entityType) => new Query(entityType, null, null, null);
    static Query Query(Collection entityType, FilterBy? filter) => new Query(entityType, filter, null, null);

    static Query Query(Collection entityType, FilterBy? filter, OrderBy? order) =>
        new Query(entityType, filter, order, null);

    static Query Query(Collection entityType, FilterBy? filter, OrderBy? order, Require? require) =>
        new Query(entityType, filter, order, require);

    static Query Query(Collection entityType, FilterBy? filter, Require? require, OrderBy? order) =>
        new Query(entityType, filter, order, require);

    static Query Query(Collection entityType, OrderBy? order) => new Query(entityType, null, order, null);

    static Query Query(Collection entityType, OrderBy? order, Require? require) =>
        new Query(entityType, null, order, require);

    static Query Query(Collection entityType, FilterBy? filter, Require? require) =>
        new Query(entityType, filter, null, require);

    static Query Query(Collection entityType, Require? require) => new Query(entityType, null, null, require);
}