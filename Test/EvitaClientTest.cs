using System.Globalization;
using NUnit.Framework;
using Client;
using Client.Config;
using Client.DataTypes;
using Client.Models;
using Client.Models.Data.Mutations;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Data.Structure;
using Client.Models.ExtraResults;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations.Attributes;
using Client.Models.Schemas.Mutations.Catalog;
using Client.Queries.Order;
using static Client.Queries.IQueryConstraints;

namespace Test;

public class EvitaClientTest
{
    private static EvitaClient? _client;
    private static EvitaClientConfiguration EvitaClientConfiguration { get; }

    private const string ExistingCatalogWithData = "moda";

    private const string TestCatalog = "testingCatalog";
    private const string TestCollection = "testingCollection";
    private const string AttributeDateTime = "attrDateTime";
    private const string AttributeDecimalRange = "attrDecimalRange";
    private const string NonExistingAttribute = "nonExistingAttribute";

    static EvitaClientTest()
    {
        // create a evita client configuration the the running instance of evita server
        EvitaClientConfiguration = new EvitaClientConfiguration.Builder()
            .SetHost("localhost")
            .SetPort(5556)
            .SetSystemApiPort(5557)
            .Build();
    }

    [SetUp]
    public static void Setup()
    {
        // create a new evita client with the specified configuration
        _client = new EvitaClient(EvitaClientConfiguration);
    }

    [Test]
    public async Task ShouldBeAbleTo_CreateCatalog_And_EntitySchema_AndInsertNewEntity_WithAttribute()
    {
        // delete test catalog if it exists
        _client!.DeleteCatalogIfExists(TestCatalog);

        // define new catalog
        CatalogSchema catalogSchema = await _client.DefineCatalogAsync(TestCatalog);
        Assert.AreEqual(TestCatalog, catalogSchema.Name);

        using (var rwSession = _client.CreateReadWriteSession(TestCatalog))
        {
            // create a new entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(new CreateEntitySchemaMutation(TestCollection));

            Assert.IsNotNull(catalogSchema.GetEntitySchema(TestCollection));
            Assert.AreEqual(1, catalogSchema.GetEntitySchema(TestCollection)!.Version);
            Assert.AreEqual(2, catalogSchema.Version);
            
            // create two attributes schema mutations
            CreateAttributeSchemaMutation createAttributeDateTime = new CreateAttributeSchemaMutation(
                AttributeDateTime, nameof(AttributeDateTime), null, false, true, true, false, true,
                typeof(DateTimeOffset), null, 0
            );
            CreateAttributeSchemaMutation createAttributeDecimalRange = new CreateAttributeSchemaMutation(
                AttributeDecimalRange, nameof(AttributeDecimalRange), null, false, true, true, false, true,
                typeof(DecimalNumberRange), null, 2
            );

            // add the two attributes to the entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(new ModifyEntitySchemaMutation(TestCollection,
                createAttributeDateTime, createAttributeDecimalRange));
            Assert.AreEqual(2, catalogSchema.Version);
            Assert.AreEqual(3, catalogSchema.GetEntitySchema(TestCollection)!.Version);

            // check if the entity schema has the two attributes
            var entitySchema = rwSession.GetEntitySchema(TestCollection);
            Assert.IsNotNull(entitySchema);
            Assert.AreEqual(2, entitySchema!.Attributes.Count);
            Assert.AreEqual(3, entitySchema.Version);
            Assert.IsTrue(entitySchema.Attributes.ContainsKey(AttributeDateTime));
            Assert.AreEqual(typeof(DateTimeOffset), entitySchema.Attributes[AttributeDateTime].Type);
            Assert.IsTrue(entitySchema.Attributes.ContainsKey(AttributeDecimalRange));
            Assert.AreEqual(typeof(DecimalNumberRange), entitySchema.Attributes[AttributeDecimalRange].Type);

            // close the session and switch catalog to the alive state
            rwSession.GoLiveAndClose();
        }

        using var newSession = _client.CreateReadWriteSession(TestCatalog);

        // insert a new entity with one of attributes defined in the entity schema
        var dateTimeNow = DateTimeOffset.Now;
        var newEntity = newSession.UpsertAndFetchEntity(
            new EntityUpsertMutation(
                TestCollection,
                null,
                EntityExistence.MayExist,
                new UpsertAttributeMutation(AttributeDateTime, dateTimeNow)
            ),
            AttributeContent()
        );

        Assert.AreEqual(2, newEntity.Schema.Attributes.Count);
        Assert.IsTrue(newEntity.Attributes.GetAttributeNames().Contains(AttributeDateTime));
        Assert.AreEqual(
            dateTimeNow.ToString("h:mm:ss tt zz"), 
            (newEntity.GetAttribute(AttributeDateTime) as DateTimeOffset?)?.ToString("h:mm:ss tt zz")
            );

        // insert a new entity with attribute that is not defined in the entity schema
        var notInAttributeSchemaEntity = newSession.UpsertAndFetchEntity(
            new EntityUpsertMutation(
                TestCollection,
                null,
                EntityExistence.MayExist,
                new UpsertAttributeMutation(NonExistingAttribute, true)
            ),
            AttributeContent()
        );

        // schema of the entity should have 3 attributes
        Assert.AreEqual(3, notInAttributeSchemaEntity.Schema.Attributes.Count);
        Assert.IsTrue(notInAttributeSchemaEntity.Attributes.GetAttributeNames().Contains(NonExistingAttribute));
        Assert.AreEqual(true, notInAttributeSchemaEntity.GetAttribute(NonExistingAttribute));
    }

    [Test]
    public async Task ShouldBeAbleTo_QueryCatalog_WithData_AndGet_DataChunkOf_EntityReferences()
    {
        EvitaEntityReferenceResponse referenceResponse = await _client!.QueryCatalogAsync(ExistingCatalogWithData,
            session =>
                session.Query<EvitaEntityReferenceResponse, EntityReference>(
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

        Assert.AreEqual(20, referenceResponse.RecordPage.Data!.Count);
        Assert.IsTrue(referenceResponse.RecordPage.Data.All(x => x.EntityType == "Product" && x.PrimaryKey > 0));
        Assert.AreEqual(1, referenceResponse.ExtraResults.Count);
        Assert.AreEqual(typeof(QueryTelemetry), referenceResponse.ExtraResults.Values.ToList()[0].GetType());
    }

    [Test]
    public async Task ShouldBeAbleTo_QueryCatalog_WithData_AndGet_DataChunkOf_SealedEntities()
    {
        EvitaEntityResponse entityResponse = await _client!.QueryCatalogAsync(ExistingCatalogWithData, session =>
            session.Query<EvitaEntityResponse, SealedEntity>(
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
                        EntityFetch(AttributeContent(), ReferenceContent(), PriceContent()),
                        DataInLocales(new CultureInfo("en-US"), new CultureInfo("cs-CZ")),
                        QueryTelemetry()
                    )
                )
            ));

        Assert.AreEqual(20, entityResponse.RecordPage.Data!.Count);
        Assert.IsTrue(entityResponse.RecordPage.Data.Any(x => x.GetAttributeValues().Any()));
        Assert.IsTrue(entityResponse.RecordPage.Data.Any(x => x.GetReferences().Any()));
        Assert.IsTrue(entityResponse.RecordPage.Data.Any(x => x.GetPrices().Any()));

        Assert.AreEqual(1, entityResponse.ExtraResults.Count);
        Assert.AreEqual(typeof(QueryTelemetry), entityResponse.ExtraResults.Values.ToList()[0].GetType());
    }
}