using System.Globalization;
using Client;
using Client.Config;
using Client.DataTypes;
using Client.Models;
using Client.Models.Data.Structure;
using Client.Queries.Filter;
using Client.Queries.Order;
using Client.Queries.Requires;
using static Client.Queries.IQueryConstraints;
using NUnit.Framework;

namespace Test;

public class EvitaQueryTest
{
    private static EvitaClient? _client;
    private static EvitaClientConfiguration EvitaClientConfiguration { get; }

    private const string ExistingCatalogWithData = "evita";

    static EvitaQueryTest()
    {
        // create a evita client configuration the the running instance of evita server
        EvitaClientConfiguration = new EvitaClientConfiguration.Builder()
            .SetHost("demo.evitadb.io")
            .SetPort(5556)
            .SetUseGeneratedCertificate(false)
            .SetUsingTrustedRootCaCertificate(true)
            .Build();
    }

    [SetUp]
    public static void Setup()
    {
        // create a new evita client with the specified configuration
        _client = new EvitaClient(EvitaClientConfiguration);
    }

    [Test]
    public void ShouldBe_WellSee()
    {
        EvitaEntityResponse evitaEntityResponse = _client!.QueryCatalog(ExistingCatalogWithData,
            session => session.Query<EvitaEntityResponse, SealedEntity>(
                Query(
                    Collection("Product"),
                    FilterBy(
                        Or(
                            AttributeInSet("visibility", "yes", "no"),
                            EntityLocaleEquals(new CultureInfo("cs-CZ")),
                            HierarchyWithin("categories", AttributeEquals("url", "A"), ExcludingRoot()),
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
                                FilterBy(AttributeContains("url", "w")),
                                OrderBy(EntityProperty(AttributeNatural("orderedQuantity", OrderDirection.Asc))),
                                EntityFetch(AttributeContentAll())
                            ),
                            HierarchyContent(StopAt(Distance(2)), EntityFetch(AttributeContentAll()))
                        ),
                        FacetSummary(
                            FacetStatisticsDepth.Counts,
                            FilterBy(AttributeContains("url", "test")),
                            OrderBy(Random())
                        ),
                        FacetSummaryOfReference(
                            "parameterValues",
                            FacetStatisticsDepth.Impact,
                            FilterBy(EntityPrimaryKeyInSet(1, 2)),
                            FilterGroupBy(EntityPrimaryKeyInSet(1, 2)),
                            OrderBy(Random()),
                            OrderGroupBy(Random())
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
        Console.WriteLine();
    }

    [Test]
    public void ShouldBe_WellSee1()
    {
        EvitaEntityResponse evitaEntityResponse = _client!.QueryCatalog(ExistingCatalogWithData,
            session =>
            {
                return session.Query<EvitaEntityResponse, SealedEntity>(
                    Query(
                        Collection("Category"),
                        Require(
                            HierarchyOfSelf(
                                OrderBy(Random()),
                                Children(
                                    "test",
                                    EntityFetch(ReferenceContentAllWithAttributes()),
                                    Statistics(StatisticsBase.CompleteFilter, StatisticsType.QueriedEntityCount)
                                )
                            )
                        )
                    )
                );
            });
    }

    [Test]
    public void ShouldBe_Tmp()
    {
        EvitaResponse<SealedEntity> entities = _client!.QueryCatalog("evita",
            session => session.QuerySealedEntity(
                Query(
                    Collection("Product"),
                    FilterBy(
                        AttributeEquals("code", "amazfit-gtr-3"),
                        EntityLocaleEquals(CultureInfo.GetCultureInfo("en"))
                    ),
                    Require(
                        EntityFetch(
                            ReferenceContent(
                                "categories", 
                                EntityFetch(AttributeContent("code", "name"),
                                    HierarchyContent(
                                        EntityFetch(AttributeContent("code", "name"))
                                    )
                                )
                            )
                        )
                    )
                )
            )
        );


        Console.WriteLine();
    }
}