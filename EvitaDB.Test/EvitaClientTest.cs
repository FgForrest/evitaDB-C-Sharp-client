using System.Globalization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using NUnit.Framework;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Queries.Order;
using static EvitaDB.Client.Queries.IQueryConstraints;
using static NUnit.Framework.Assert;

namespace EvitaDB.Test;

public class EvitaClientTest
{
    private static EvitaClient? _client;

    private const string ExistingCatalogWithData = "evita";

    private const string TestCatalog = "testingCatalog";
    private const string TestCollection = "testingCollection";
    private const string AttributeDateTime = "attrDateTime";
    private const string AttributeDecimalRange = "attrDecimalRange";
    private const string NonExistingAttribute = "nonExistingAttribute";

    private const int GrpcPort = 5556;
    private const int SystemApiPort = 5557;

    [OneTimeSetUp]
    public static async Task Setup()
    {
        IContainer container = new ContainerBuilder()
            // Set the image for the container to "evitadb/evitadb".
            .WithImage("evitadb/evitadb:canary")
            // Bind ports of the container.
            .WithPortBinding(GrpcPort)
            .WithPortBinding(SystemApiPort)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort).UntilPortIsAvailable(SystemApiPort))
            // Build the container configuration.
            .Build();

        // Start the container.
        await container.StartAsync()
            .ConfigureAwait(false);
        
        // create a evita client configuration the the running instance of evita server
        EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
            .SetHost("localhost")
            .SetPort(GrpcPort)
            .SetSystemApiPort(SystemApiPort)
            .Build();
        
        // create a new evita client with the specified configuration
        _client = new EvitaClient(configuration);
    }

    [Test]
    public void ShouldBeAbleTo_CreateCatalog_And_EntitySchema_AndInsertNewEntity_WithAttribute()
    {
        // delete test catalog if it exists
        _client!.DeleteCatalogIfExists(TestCatalog);

        // define new catalog
        ICatalogSchema catalogSchema = _client.DefineCatalog(TestCatalog).ToInstance();
        That(catalogSchema.Name, Is.EqualTo(TestCatalog));

        using (EvitaClientSession rwSession = _client.CreateReadWriteSession(TestCatalog))
        {
            // create a new entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(new CreateEntitySchemaMutation(TestCollection));

            That(catalogSchema.GetEntitySchema(TestCollection), Is.Not.Null);
            That(catalogSchema.GetEntitySchema(TestCollection)!.Version, Is.EqualTo(1));
            That(catalogSchema.Version, Is.EqualTo(2));

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
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(
                new ModifyEntitySchemaMutation(TestCollection,
                createAttributeDateTime, createAttributeDecimalRange)
            );
            That(catalogSchema.Version, Is.EqualTo(2));
            That(catalogSchema.GetEntitySchema(TestCollection)!.Version, Is.EqualTo(3));

            // check if the entity schema has the two attributes
            var entitySchema = rwSession.GetEntitySchema(TestCollection);
            That(entitySchema, Is.Not.Null);
            That(entitySchema!.Attributes.Count, Is.EqualTo(2));
            That(entitySchema.Version, Is.EqualTo(3));
            IsTrue(entitySchema.Attributes.ContainsKey(AttributeDateTime));
            That(entitySchema.Attributes[AttributeDateTime].Type, Is.EqualTo(typeof(DateTimeOffset)));
            IsTrue(entitySchema.Attributes.ContainsKey(AttributeDecimalRange));
            That(entitySchema.Attributes[AttributeDecimalRange].Type, Is.EqualTo(typeof(DecimalNumberRange)));

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

        That(newEntity.Schema.Attributes.Count, Is.EqualTo(2));
        That(newEntity.GetAttributeNames().Contains(AttributeDateTime), Is.True);
        That(
            (newEntity.GetAttribute(AttributeDateTime) as DateTimeOffset?)?.ToString("h:mm:ss tt zz"), 
            Is.EqualTo(dateTimeNow.ToString("h:mm:ss tt zz"))
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
        That(notInAttributeSchemaEntity.Schema.Attributes.Count, Is.EqualTo(3));
        That(notInAttributeSchemaEntity.GetAttributeNames().Contains(NonExistingAttribute), Is.True);
        That(notInAttributeSchemaEntity.GetAttribute(NonExistingAttribute), Is.EqualTo(true));
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
}