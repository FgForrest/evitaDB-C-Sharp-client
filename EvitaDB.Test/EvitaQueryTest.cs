﻿using System.Globalization;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using NUnit.Framework;
using static EvitaDB.Client.Queries.IQueryConstraints;
using static NUnit.Framework.Assert;

namespace EvitaDB.Test;

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

        /*EvitaClientConfiguration = new EvitaClientConfiguration.Builder()
            .SetHost("localhost")
            .SetPort(5556)
            .SetUseGeneratedCertificate(true)
            .SetUsingTrustedRootCaCertificate(false)
            .Build();*/
    }
    
    [Test]
    public void ShouldBeAbleTo_QueryCatalog_WithData_AndGet_DataChunkOf_EntityReferences()
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
        That(referenceResponse.ExtraResults.Values.ToList()[0].GetType(), Is.EqualTo(typeof(QueryTelemetry)));
    }

    [Test]
    public void ShouldBeAbleTo_QueryCatalog_WithData_AndGet_DataChunkOf_SealedEntities()
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
        That(entityResponse.ExtraResults.Values.ToList()[0].GetType(), Is.EqualTo(typeof(QueryTelemetry)));
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
            session => session.Query<EvitaEntityResponse, ISealedEntity>(
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
        EvitaResponse<ISealedEntity> evitaEntityResponse = _client!.QueryCatalog(ExistingCatalogWithData,
            session => session.QuerySealedEntity(
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
            ));
    }

    [Test]
    public void ShouldBe_Tmp()
    {
        EvitaResponse<ISealedEntity> entities = _client!.QueryCatalog("evita",
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

    [Test]
    public void ShouldTestCdc()
    {
        IObservable<ChangeSystemCapture> captures =
            _client!.RegisterSystemChangeCapture(new ChangeSystemCaptureRequest(CaptureContent.Header));
        var subscription = captures.Subscribe(c => { Console.WriteLine(c.Operation); });
        subscription.Dispose();
    }

    [Test]
    public void ShouldIdk()
    {
        EvitaResponse<ISealedEntity> entities = _client!.QueryCatalog(
            "evita",
            session => session.QuerySealedEntity(
                Query(
                    Collection("Product"),
                    FilterBy(
                        HierarchyWithin(
                            "categories",
                            AttributeEquals("code", "vouchers-for-shareholders")
                        ),
                        EntityLocaleEquals(CultureInfo.GetCultureInfo("cs"))
                    ),
                    Require(
                        EntityFetch(
                            AttributeContent("code", "name")
                        )
                    )
                )
            )
        );
        Console.WriteLine();
    }

    [Test]
    public void ShouldTestCdo()
    {
        EvitaResponse<ISealedEntity> entities = _client!.QueryCatalog(
            "evita",
            session => session.QuerySealedEntity(
                Query(
                    Collection("Product"),
                    Require(
                        EntityFetch(
                            AssociatedDataContentAll()
                        )
                    )
                )
            )
        );

        var session = _client.CreateReadWriteSession("evita");

        var x = ComplexDataObjectConverter.ConvertFromComplexDataObject<TestAsDataObj[]>(
            (entities.RecordData[0].GetAssociatedData("allActiveUrls") as ComplexDataObject)!);

        Console.WriteLine(x);

        var asData = new TestAsDataObj[]
        {
            new("cs", "/cs/macbook-pro-13-2022"),
            new("en", "/en/macbook-pro-13-2022")
        };
        var initialBuilder = session.CreateNewEntity("Product", 6666666)
            .SetAssociatedData("allActiveUrls", asData);
        session.UpsertEntity(initialBuilder);
        Console.WriteLine();
    }

    private record TestAsDataObj(string Locale, string Url)
    {
        public TestAsDataObj() : this("", "")
        {
        }
    }
}