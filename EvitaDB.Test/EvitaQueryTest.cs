using System.Globalization;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Test.Utils;
using NUnit.Framework;
using static EvitaDB.Client.Queries.IQueryConstraints;
using static NUnit.Framework.Assert;

namespace EvitaDB.Test;

public class EvitaQueryTest : IDisposable
{
    private static EvitaClient? _client;
    private static EvitaClientConfiguration? EvitaClientConfiguration { get; set; }

    private const string ExistingCatalogWithData = "evita";

    [OneTimeSetUp]
    public static void Setup()
    {
        EvitaClientConfiguration = new EvitaClientConfiguration.Builder()
            .SetHost("demo.evitadb.io")
            .SetPort(5556)
            .SetUseGeneratedCertificate(false)
            .SetUsingTrustedRootCaCertificate(true)
            .Build();
        
    }
    
    [SetUp]
    public void BeforeEachTest()
    {
        // create a new evita client with the specified configuration
        _client = new EvitaClient(EvitaClientConfiguration!);
    }
    
    [Test]
    public void ShouldBeAbleToQueryCatalogWithDataAndGetDataChunkOfEntityReferences()
    {
        EvitaEntityReferenceResponse referenceResponse = _client!.QueryCatalog(ExistingCatalogWithData,
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

        That(referenceResponse.RecordPage.Data!.Count, Is.EqualTo(20));
        That(referenceResponse.RecordPage.Data.All(x => x is {Type: "Product", PrimaryKey: > 0}), Is.True);
        That(referenceResponse.ExtraResults.Count, Is.EqualTo(1));
        That(referenceResponse.ExtraResults.Values.ToList()[0].GetType(), Is.EqualTo(typeof(Client.Models.ExtraResults.QueryTelemetry)));
    }

    [Test]
    public void ShouldBeAbleToQueryCatalogWithDataAndGetDataChunkOfSealedEntities()
    {
        EvitaEntityResponse entityResponse = _client!.QueryCatalog(ExistingCatalogWithData, session =>
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

        That(entityResponse.RecordPage.Data!.Count, Is.EqualTo(20));
        That(entityResponse.RecordPage.Data.Any(x => x.GetAttributeValues().Any()), Is.True);
        That(entityResponse.RecordPage.Data.Any(x => x.GetReferences().Any()), Is.True);
        That(entityResponse.RecordPage.Data.Any(x => x.GetPrices().Any()), Is.True);

        That(entityResponse.ExtraResults.Count, Is.EqualTo(1));
        That(entityResponse.ExtraResults.Values.ToList()[0].GetType(), Is.EqualTo(typeof(Client.Models.ExtraResults.QueryTelemetry)));
    }

    [Test]
    public void ShouldBeAbleToExecuteComplexQueryAndGetResults()
    {
        EvitaEntityResponse evitaEntityResponse = _client!.QueryCatalog(ExistingCatalogWithData,
            session => session.Query<EvitaEntityResponse, ISealedEntity>(
                Query(
                    Collection("Product"),
                    FilterBy(
                        Or(
                            AttributeInSet("visibility", "yes", "no"),
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
                                PriceValidNow()
                            ),
                            AttributeBetween("frekvence-od", 20m, 95.3m),
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
        That(evitaEntityResponse.RecordData.Count, Is.GreaterThan(0));
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}