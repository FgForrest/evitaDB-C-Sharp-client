using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Test.Utils;
using Xunit.Abstractions;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Test.Tests;

public class EvitaClientDemoQueryTest : BaseTest<DemoSetupFixture>
{
    public EvitaClientDemoQueryTest(ITestOutputHelper outputHelper, DemoSetupFixture setupFixture)
        : base(outputHelper, setupFixture)
    {
    }

    private const string ExistingCatalogWithData = "evita";
    
    [Fact]
    public void ShouldBeAbleToQueryCatalogWithDataAndGetDataChunkOfEntityReferences()
    {
        EvitaEntityReferenceResponse referenceResponse = Client!.QueryCatalog(ExistingCatalogWithData,
            session => session.Query<EvitaEntityReferenceResponse, EntityReference>(
                Query(
                    Collection("Product"),
                    FilterBy(
                        And(
                            Not(
                                AttributeContains("url", "bla")
                            ),
                            Or(
                                EntityPrimaryKeyInSet(677, 678, 679, 680),
                                And(
                                    PriceBetween(1.2m, 1000),
                                    PriceValidIn(DateTimeOffset.Now),
                                    PriceInPriceLists("basic", "vip"),
                                    PriceInCurrency(new Currency("CZK"))
                                )
                            )
                        )
                    ),
                    OrderBy(
                        PriceNatural(OrderDirection.Desc)
                    ),
                    Require(
                        Page(1, 20),
                        DataInLocales(new CultureInfo("en-US"), new CultureInfo("cs-CZ")),
                        QueryTelemetry()
                    )
                )
            ));
        
        Assert.Equal(20, referenceResponse.RecordPage.Data!.Count);
        Assert.True(referenceResponse.RecordPage.Data.All(x => x is {Type: "Product", PrimaryKey: > 0}));
        Assert.Equal(1, referenceResponse.ExtraResults.Count);
        Assert.Equal(typeof(Client.Models.ExtraResults.QueryTelemetry), referenceResponse.ExtraResults.Values.ToList()[0].GetType());
    }

    [Fact]
    public void ShouldBeAbleToQueryCatalogWithDataAndGetDataChunkOfSealedEntities()
    {
        EvitaEntityResponse entityResponse = Client!.QueryCatalog(ExistingCatalogWithData, session =>
            session.Query<EvitaEntityResponse, ISealedEntity>(
                Query(
                    Collection("Product"),
                    FilterBy(
                        And(
                            Not(
                                AttributeContains("url", "bla")
                            ),
                            Or(
                                EntityPrimaryKeyInSet(677, 678, 679, 680),
                                And(
                                    PriceBetween(102.2m, 10000),
                                    PriceValidIn(DateTimeOffset.Now),
                                    PriceInPriceLists("basic", "vip"),
                                    PriceInCurrency(new Currency("CZK"))
                                )
                            )
                        )
                    ),
                    OrderBy(
                        PriceNatural(OrderDirection.Desc)
                    ),
                    Require(
                        Page(1, 20),
                        EntityFetch(AttributeContentAll(), ReferenceContentAll(), PriceContentAll()),
                        DataInLocales(new CultureInfo("en-US"), new CultureInfo("cs-CZ")),
                        QueryTelemetry()
                    )
                )
            ));
        
        Assert.Equal(20, entityResponse.RecordPage.Data!.Count);
        Assert.Contains(entityResponse.RecordPage.Data, x => x.GetAttributeValues().Any());
        Assert.Contains(entityResponse.RecordPage.Data, x => x.GetReferences().Any());
        Assert.Contains(entityResponse.RecordPage.Data, x => x.GetPrices().Any());

        Assert.Equal(1, entityResponse.ExtraResults.Count);
        Assert.Equal(typeof(Client.Models.ExtraResults.QueryTelemetry), entityResponse.ExtraResults.Values.ToList()[0].GetType());
    }

    [Fact]
    public void ShouldBeAbleToExecuteComplexQueryAndGetResults()
    {
        EvitaEntityResponse evitaEntityResponse = Client!.QueryCatalog(ExistingCatalogWithData,
            session => session.Query<EvitaEntityResponse, ISealedEntity>(
                Query(
                    Collection("Product"),
                    FilterBy(
                        Or(
                            EntityLocaleEquals(new CultureInfo("cs-CZ")),
                            HierarchyWithin("categories", null, ExcludingRoot()),
                            Not(
                                ReferenceHaving(
                                    "groups",
                                    And(
                                        AttributeInRange("assignmentValidity", DateTimeOffset.Now),
                                        AttributeInRange("assignmentValidity", DateTimeOffset.Now.AddDays(7))
                                    )
                                )
                            ),
                            And(
                                PriceInPriceLists("vip", "basic"),
                                PriceInCurrency("CZK"),
                                PriceBetween(100m, 200.5m),
                                PriceValidInNow()
                            ),
                            UserFilter(
                                FacetHaving("variantParameters", EntityPrimaryKeyInSet(1, 2, 3))
                            )
                        )),
                    OrderBy(
                        AttributeNatural("orderedQuantity", OrderDirection.Desc),
                        PriceNatural(OrderDirection.Asc),
                        ReferenceProperty(
                            "groups", Random()
                        )
                    ),
                    Require(
                        EntityFetch(
                            AssociatedDataContentAll(),
                            AttributeContentAll(),
                            PriceContentAll(),
                            ReferenceContentWithAttributes(
                                "relatedProducts",
                                FilterBy(AttributeContains("category", "w")),
                                OrderBy(EntityProperty(AttributeNatural("orderedQuantity", OrderDirection.Asc))),
                                EntityFetch(AttributeContentAll())
                            ),
                            HierarchyContent(StopAt(Distance(2)), EntityFetch(AttributeContentAll()))
                        ),
                        FacetSummaryOfReference(
                            "parameterValues",
                            FacetStatisticsDepth.Impact,
                            FilterBy(AttributeContains("code", "a")),
                            OrderBy(AttributeNatural(Data.AttributeName, OrderDirection.Desc))
                        ),
                        FacetGroupsDisjunction("productSetItems", FilterBy(AttributeGreaterThanEquals("order", 5))),
                        AttributeHistogram(20, "response-time", "thickness"),
                        PriceHistogram(10),
                        QueryTelemetry(),
                        HierarchyOfReference(
                            "categories",
                            FromNode(
                                "x",
                                Node(FilterBy(AttributeEquals("code", "portables"))),
                                EntityFetch(AttributeContent("categoryType")),
                                StopAt(Distance(1)),
                                Statistics(StatisticsType.ChildrenCount, StatisticsType.QueriedEntityCount)
                            ),
                            FromNode(
                                "y",
                                Node(FilterBy(AttributeContains("code", "laptops"))),
                                EntityFetch(AttributeContent("status")),
                                StopAt(Distance(2)),
                                Statistics(StatisticsType.ChildrenCount, StatisticsType.QueriedEntityCount)
                            )
                        ),
                        PriceType(QueryPriceMode.WithoutTax),
                        Strip(0, 20)
                    )
                )
            )
        );
        
        Assert.True(evitaEntityResponse.RecordData.Count > 0);
    }
}
