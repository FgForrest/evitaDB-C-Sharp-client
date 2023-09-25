using System.Globalization;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Test.Utils;
using NUnit.Framework;
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
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort).UntilPortIsAvailable(SystemApiPort))
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
            ISealedEntitySchema? entitySchema = rwSession.GetEntitySchema(TestCollection);
            That(entitySchema, Is.Not.Null);
            That(entitySchema!.Attributes.Count, Is.EqualTo(2));
            That(entitySchema.Version, Is.EqualTo(3));
            That(entitySchema.Attributes.ContainsKey(AttributeDateTime), Is.True);
            That(entitySchema.Attributes[AttributeDateTime].Type, Is.EqualTo(typeof(DateTimeOffset)));
            That(entitySchema.Attributes.ContainsKey(AttributeDecimalRange), Is.True);
            That(entitySchema.Attributes[AttributeDecimalRange].Type, Is.EqualTo(typeof(DecimalNumberRange)));

            // close the session and switch catalog to the alive state
            rwSession.GoLiveAndClose();
        }

        using EvitaClientSession newSession = _client.CreateReadWriteSession(TestCatalog);

        // insert a new entity with one of attributes defined in the entity schema
        DateTimeOffset dateTimeNow = DateTimeOffset.Now;
        ISealedEntity newEntity = newSession.UpsertAndFetchEntity(
            new EntityUpsertMutation(
                TestCollection,
                null,
                EntityExistence.MayExist,
                new UpsertAttributeMutation(AttributeDateTime, dateTimeNow)
            ),
            AttributeContent()
        );

        string dateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";

        That(newEntity.Schema.Attributes.Count, Is.EqualTo(2));
        That(newEntity.GetAttributeNames().Contains(AttributeDateTime), Is.True);
        That(
            (newEntity.GetAttribute(AttributeDateTime) as DateTimeOffset?)?.ToString(dateTimeFormat),
            Is.EqualTo(dateTimeNow.ToString(dateTimeFormat))
        );

        // insert a new entity with attribute that is not defined in the entity schema
        ISealedEntity notInAttributeSchemaEntity = newSession.UpsertAndFetchEntity(
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
    public void ShouldAllowCreatingCatalogAlongWithTheSchema()
    {
        string someCatalogName = "differentCatalog";
        try
        {
            _client!.DefineCatalog(someCatalogName)
                .WithDescription("Some description.")
                .UpdateVia(_client.CreateReadWriteSession(someCatalogName));
            That(_client.GetCatalogNames().Contains(someCatalogName), Is.True);
        }
        finally
        {
            _client!.DeleteCatalogIfExists(someCatalogName);
        }
    }

    [Test]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsInPrototypingMode()
    {
        string someCatalogName = "differentCatalog";
        CultureInfo locale = Data.EnglishLocale;
        try
        {
            _client!.DefineCatalog(someCatalogName)
                .WithDescription("This is a tutorial catalog.")
                .UpdateViaNewSession(_client);
            That(_client.GetCatalogNames().Contains(someCatalogName), Is.True);
            _client.UpdateCatalog(
                someCatalogName,
                session =>
                {
                    session.CreateNewEntity("Brand", 1)
                        .SetAttribute("name", locale, "Lenovo")
                        .UpsertVia(session);

                    ISealedEntitySchema? brand = session.GetEntitySchema("Brand");
                    That(brand is not null, Is.True);

                    IAttributeSchema? nameAttribute = brand?.GetAttribute("name");
                    That(nameAttribute is not null, Is.True);
                    That(nameAttribute?.Localized, Is.True);

                    // now create an example category tree
                    session.CreateNewEntity("Category", 10)
                        .SetAttribute("name", locale, "Electronics")
                        .UpsertVia(session);

                    session.CreateNewEntity("Category", 11)
                        .SetAttribute("name", locale, "Laptops")
                        // laptops will be a child category of electronics
                        .SetParent(10)
                        .UpsertVia(session);

                    // finally, create a product
                    session.CreateNewEntity("Product")
                        // with a few attributes
                        .SetAttribute("name", locale, "ThinkPad P15 Gen 1")
                        .SetAttribute("cores", 8)
                        .SetAttribute("graphics", "NVIDIA Quadro RTX 4000 with Max-Q Design")
                        // and price for sale
                        .SetPrice(
                            1, "basic",
                            new Currency("USD"),
                            1420m, 20m, 1704m,
                            true
                        )
                        // link it to the manufacturer
                        .SetReference(
                            "brand", "Brand",
                            Cardinality.ExactlyOne,
                            1
                        )
                        // and to the laptop category
                        .SetReference(
                            "categories", "Category",
                            Cardinality.ZeroOrMore,
                            11
                        )
                        .UpsertVia(session);

                    ISealedEntitySchema? product = session.GetEntitySchema("Product");
                    That(product is not null, Is.True);

                    IAttributeSchema? productNameAttribute = product?.GetAttribute("name");
                    That(productNameAttribute is not null, Is.True);
                    That(productNameAttribute?.Localized, Is.True);
                }
            );
        }
        finally
        {
            _client?.DeleteCatalogIfExists(someCatalogName);
        }
    }

    [Test]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsSchemas()
    {
        string someCatalogName = "differentCatalog";
        try
        {
            _client!.DefineCatalog(someCatalogName)
                .WithDescription("This is a tutorial catalog.")
                // define brand schema
                .WithEntitySchema(
                    "Brand",
                    whichIs => whichIs.WithDescription("A manufacturer of products.")
                        .WithAttribute<string>(
                            "name",
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                )
                // define category schema
                .WithEntitySchema(
                    "Category",
                    whichIs => whichIs.WithDescription("A category of products.")
                        .WithAttribute<string>(
                            "name",
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                        .WithHierarchy()
                )
                // define product schema
                .WithEntitySchema(
                    "Product",
                    whichIs => whichIs.WithDescription("A product in inventory.")
                        .WithAttribute<string>(
                            "name",
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                        .WithAttribute<int>(
                            "cores",
                            thatIs => thatIs.WithDescription("Number of CPU cores.")
                                .Filterable()
                        )
                        .WithAttribute<string>(
                            "graphics",
                            thatIs => thatIs.WithDescription("Graphics card.")
                                .Filterable()
                        )
                        .WithPrice()
                        .WithReferenceToEntity(
                            "brand", "Brand", Cardinality.ExactlyOne,
                            thatIs => thatIs.Indexed()
                        )
                        .WithReferenceToEntity(
                            "categories", "Category", Cardinality.ZeroOrMore,
                            thatIs => thatIs.Indexed()
                        )
                )
                // and now push all the definitions (mutations) to the server
                .UpdateViaNewSession(_client);
            That(_client.GetCatalogNames().Contains(someCatalogName), Is.True);
            _client.QueryCatalog(someCatalogName, session =>
            {
                ISet<string> allEntityTypes = session.GetAllEntityTypes();
                That(allEntityTypes.Contains("Brand"), Is.True);
                That(allEntityTypes.Contains("Category"), Is.True);
                That(allEntityTypes.Contains("Product"), Is.True);
            });
        }
        finally
        {
            _client!.DeleteCatalogIfExists(someCatalogName);
        }
    }

    [Test]
    public void ShouldBeAbleToRunParallelClients()
    {
        EvitaClient anotherParallelClient = new EvitaClient(_client!.Configuration);
        ListCatalogNames(anotherParallelClient);
        ListCatalogNames(_client);
    }

    [Test]
    public void ShouldAbleToFetchNonCachedEntitySchemaFromCatalogSchema()
    {
        EvitaClient clientWithEmptyCache = new EvitaClient(_client!.Configuration);
        clientWithEmptyCache.QueryCatalog(
            TestCatalog,
            session =>
            {
                IEntitySchema? productSchema = session.GetCatalogSchema().GetEntitySchema(Entities.Product);
                That(productSchema, Is.Not.Null);
            }
        );
    }

    [Test]
    public void ShouldListCatalogNames()
    {
        ISet<string> catalogNames = ListCatalogNames(_client!);
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(TestCatalog), Is.True);
    }

    private static ISet<string> ListCatalogNames(EvitaClient evitaClient)
    {
        return evitaClient.GetCatalogNames();
    }

    [Test]
    public void ShouldCreateCatalog()
    {
        string newCatalogName = "newCatalog";
        try
        {
            ICatalogSchema newCatalog = _client!.DefineCatalog(newCatalogName);
            ISet<String> catalogNames = _client.GetCatalogNames();

            That(catalogNames.Count, Is.EqualTo(2));
            That(catalogNames.Contains(TestCatalog), Is.True);
            That(catalogNames.Contains(newCatalogName), Is.True);
        }
        finally
        {
            _client!.DeleteCatalogIfExists(newCatalogName);
        }
    }

    [Test]
    public void ShouldRemoveCatalog()
    {
        string newCatalogName = "newCatalog";
        _client!.DefineCatalog(newCatalogName).UpdateViaNewSession(_client);
        bool removed = _client.DeleteCatalogIfExists(newCatalogName);
        That(removed, Is.True);

        ISet<String> catalogNames = _client.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(TestCatalog), Is.True);
    }

    [Test]
    public void ShouldReplaceCatalog()
    {
        string newCatalog = "newCatalog";
        _client!.DefineCatalog(newCatalog);

        ISet<string> catalogNames = _client.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(2));
        That(catalogNames.Contains(newCatalog), Is.True);
        That(catalogNames.Contains(TestCatalog), Is.True);
        That(
            _client.QueryCatalog(TestCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version),
            Is.EqualTo(10));

        _client.ReplaceCatalog(TestCatalog, newCatalog);

        ISet<string> catalogNamesAgain = _client.GetCatalogNames();
        That(catalogNamesAgain.Count, Is.EqualTo(1));
        That(catalogNamesAgain.Contains(newCatalog), Is.True);
        That(
            _client.QueryCatalog(newCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version)
            , Is.EqualTo(11));
    }

    [Test]
    public void ShouldRenameCatalog()
    {
        string newCatalog = "newCatalog";

        ISet<string> catalogNames = _client!.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(TestCatalog), Is.True);
        That(
            _client.QueryCatalog(TestCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version),
            Is.EqualTo(10));

        _client.RenameCatalog(TestCatalog, newCatalog);

        ISet<string> catalogNamesAgain = _client.GetCatalogNames();
        That(catalogNamesAgain.Count, Is.EqualTo(1));
        That(catalogNamesAgain.Contains(newCatalog), Is.True);
        That(
            _client.QueryCatalog(newCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version), 
            Is.EqualTo(11));
    }

    [Test]
    public void ShouldReplaceCollection()
    {
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        _client!.UpdateCatalog(
            TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.True);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.False);
                session.DefineEntitySchema(newCollection)
                    .WithGlobalAttribute(Data.AttributeCode)
                    .UpdateVia(session);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.True);
                productSchemaVersion = session.GetEntitySchemaOrThrow(Entities.Product).Version;
                productCount = session.GetEntityCollectionSize(Entities.Product);
            }
        );
        _client.UpdateCatalog(
            TestCatalog,
            session => session.ReplaceCollection(
                newCollection,
                Entities.Product
            ));
        _client.QueryCatalog(
            TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.False);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.True);
                That(session.GetEntitySchemaOrThrow(newCollection).Version,
                    Is.EqualTo(productSchemaVersion!.Value + 1));
                That(session.GetEntityCollectionSize(newCollection), Is.EqualTo(productCount!));
            }
        );
    }


    [Test]
    public void ShouldRenameCollection()
    {
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        _client!.QueryCatalog(
            TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.True);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.False);
                productSchemaVersion = session.GetEntitySchemaOrThrow(Entities.Product).Version;
                productCount = session.GetEntityCollectionSize(Entities.Product);
            }
        );
        _client.UpdateCatalog(
            TestCatalog,
            session => session.RenameCollection(
                Entities.Product,
                newCollection
            ));
        _client.QueryCatalog(
            TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.False);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.True);
                That(session.GetEntitySchemaOrThrow(newCollection).Version,
                    Is.EqualTo(productSchemaVersion!.Value + 1));
                That(session.GetEntityCollectionSize(newCollection), Is.EqualTo(productCount!.Value));
            }
        );
    }

    [Test]
    public void ShouldQueryCatalog()
    {
        ICatalogSchema catalogSchema = _client!.QueryCatalog(
            TestCatalog,
            x => x.GetCatalogSchema()
        );
        That(catalogSchema, Is.Not.Null);
        That(catalogSchema.Name, Is.EqualTo(TestCatalog));
    }

    [Test]
    public void ShouldQueryOneEntityReference()
    {
        EntityReference entityReference = _client!.QueryCatalog(
            TestCatalog,
            session => session.QueryOneEntityReference(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(1)
                    )
                )
            ) ?? throw new Exception("Entity reference not found!"));
        That(entityReference, Is.Not.Null);
        That(entityReference.Type, Is.EqualTo(Entities.Product));
        That(entityReference.PrimaryKey!, Is.EqualTo(1));
    }

    [Test]
    public void ShouldNotQueryOneMissingEntity()
    {
        EntityReference? entityReference = _client!.QueryCatalog(
            TestCatalog,
            session => session.QueryOneEntityReference(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(-100)
                    )
                )
            ));
        That(entityReference, Is.Null);
    }
}