using System.Collections.Concurrent;
using System.Globalization;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using EvitaDB.Client;
using EvitaDB.Client.Config;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using EvitaDB.Test.Utils;
using KellermanSoftware.CompareNetObjects;
using NUnit.DeepObjectCompare;
using NUnit.Framework;
using static EvitaDB.Client.Queries.IQueryConstraints;
using static NUnit.Framework.Assert;
using AttributeHistogram = EvitaDB.Client.Models.ExtraResults.AttributeHistogram;
using FacetSummary = EvitaDB.Client.Models.ExtraResults.FacetSummary;
using Is = NUnit.Framework.Is;
using PriceHistogram = EvitaDB.Client.Models.ExtraResults.PriceHistogram;
using QueryTelemetry = EvitaDB.Client.Models.ExtraResults.QueryTelemetry;
using IsExactlyTheSame = NUnit.DeepObjectCompare.Is;
using Random = System.Random;

namespace EvitaDB.Test;

[Parallelizable(ParallelScope.All)]
public class EvitaClientTest : IAsyncDisposable
{
    private const int RandomSeed = 42;
    private const int GrpcPort = 5556;
    private const int SystemApiPort = 5557;
    private const string Host = "localhost";
    private const string ImageName = "evitadb/evitadb:canary";
    
    private static readonly Random Random = new(RandomSeed);

    private static readonly IDictionary<string, IList<ISealedEntity>> CreatedEntities =
        new ConcurrentDictionary<string, IList<ISealedEntity>>();

    private static readonly IDictionary<string, IEntitySchema> CreatedSchemas = new ConcurrentDictionary<string, IEntitySchema>();
    
    private readonly ConcurrentQueue<EvitaClient> _clients = new();
    private readonly IDictionary<EvitaClient, IContainer> _clientContainers = new ConcurrentDictionary<EvitaClient, IContainer>();
    
    private readonly ComparisonConfig _entityComparisonConfig = new() 
    { 
        MembersToIgnore = new List<string>
        {
            "LocalePredicate", "HierarchyPredicate", "AttributePredicate", 
            "AssociatedDataPredicate", "ReferencePredicate", "PricePredicate", "Schema"
        } 
    };
    
    private EvitaClient GetClient()
    {
        if (_clients.TryDequeue(out EvitaClient? client))
        {
            DeleteCreateAndSetupCatalog(client, Data.TestCatalog, true);
            return client;
        }
        return InitializeEvitaClient().GetAwaiter().GetResult();
    }

    [OneTimeSetUp]
    public async Task Setup()
    {
        using DockerClient client = new DockerClientConfiguration().CreateClient();
        // Get information about the locally cached image (if it exists)
        var images = await client.Images.ListImagesAsync(
            new ImagesListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["reference"] = new Dictionary<string, bool>
                    {
                        [ImageName] = true,
                    },
                }
            });
        if (images.Count > 0)
        {
            var localImage = images[0];

            // Get information about the remote image from the Docker registry
            ImageInspectResponse remoteImage = await client.Images.InspectImageAsync(ImageName);

            // Compare image timestamps to determine if the remote image is newer
            if (remoteImage.Created > localImage.Created)
            {
                // Pull the new image
                await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = ImageName }, null,
                    new Progress<JSONMessage>());
            }
        }
        else
        {
            // If the image is not cached locally, simply pull it
            await client.Images.CreateImageAsync(new ImagesCreateParameters { FromImage = ImageName }, null,
                new Progress<JSONMessage>());
        }

        EvitaClient setupClient = await InitializeEvitaClient();
        setupClient.Close();
        await _clientContainers[setupClient].StopAsync();
        _clientContainers.Remove(setupClient);
    }

    private async Task<EvitaClient> InitializeEvitaClient()
    {
        IContainer container = new ContainerBuilder()
            .WithName($"evita-{Guid.NewGuid().ToString()}")
            // Set the image for the container to "evitadb/evitadb".
            .WithImage(ImageName)
            // Bind ports of the container.
            .WithPortBinding(GrpcPort, true)
            .WithPortBinding(SystemApiPort, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilPortIsAvailable(GrpcPort).UntilPortIsAvailable(SystemApiPort))
            // Build the container configuration.
            .Build();

        // Start the container.
        await container.StartAsync();

        // create a evita client configuration the the running instance of evita server
        EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
            .SetHost(Host)
            .SetPort(container.GetMappedPublicPort(GrpcPort))
            .SetSystemApiPort(container.GetMappedPublicPort(SystemApiPort))
            .Build();

        // create a new evita client with the specified configuration
        EvitaClient evitaClient = new EvitaClient(configuration);
        DeleteCreateAndSetupCatalog(evitaClient, Data.TestCatalog, false, true);
        
        _clientContainers.Add(evitaClient, container);
        return evitaClient;
    }

    [Test]
    public void ShouldBeAbleToCreateCatalogAndEntitySchemaAndInsertNewEntityWithAttribute()
    {
        EvitaClient client = GetClient();
        
        string testCollection = "testingCollection";
        string attributeDateTime = "attrDateTime";
        string attributeDecimalRange = "attrDecimalRange";
        string nonExistingAttribute = "nonExistingAttribute";

        // delete test catalog if it exists
        client.DeleteCatalogIfExists(Data.TestCatalog);

        // define new catalog
        ICatalogSchema catalogSchema = client.DefineCatalog(Data.TestCatalog).ToInstance();
        That(catalogSchema.Name, Is.EqualTo(Data.TestCatalog));

        using (EvitaClientSession rwSession = client.CreateReadWriteSession(Data.TestCatalog))
        {
            // create a new entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(new CreateEntitySchemaMutation(testCollection));

            That(catalogSchema.GetEntitySchema(testCollection), Is.Not.Null);
            That(catalogSchema.GetEntitySchema(testCollection)!.Version, Is.EqualTo(1));
            That(catalogSchema.Version, Is.EqualTo(2));

            // create two attributes schema mutations
            CreateAttributeSchemaMutation createAttributeDateTime = new CreateAttributeSchemaMutation(
                attributeDateTime, nameof(attributeDateTime), null, false, true, true, false, true,
                typeof(DateTimeOffset), null, 0
            );
            CreateAttributeSchemaMutation createAttributeDecimalRange = new CreateAttributeSchemaMutation(
                attributeDecimalRange, nameof(attributeDecimalRange), null, false, true, true, false, true,
                typeof(DecimalNumberRange), null, 2
            );

            // add the two attributes to the entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(
                new ModifyEntitySchemaMutation(testCollection,
                    createAttributeDateTime, createAttributeDecimalRange)
            );
            That(catalogSchema.Version, Is.EqualTo(2));
            That(catalogSchema.GetEntitySchema(testCollection)!.Version, Is.EqualTo(3));

            // check if the entity schema has the two attributes
            ISealedEntitySchema? entitySchema = rwSession.GetEntitySchema(testCollection);
            That(entitySchema, Is.Not.Null);
            That(entitySchema!.Attributes.Count, Is.EqualTo(2));
            That(entitySchema.Version, Is.EqualTo(3));
            That(entitySchema.Attributes.ContainsKey(attributeDateTime), Is.True);
            That(entitySchema.Attributes[attributeDateTime].Type, Is.EqualTo(typeof(DateTimeOffset)));
            That(entitySchema.Attributes.ContainsKey(attributeDecimalRange), Is.True);
            That(entitySchema.Attributes[attributeDecimalRange].Type, Is.EqualTo(typeof(DecimalNumberRange)));

            // close the session and switch catalog to the alive state
            rwSession.GoLiveAndClose();
        }

        using EvitaClientSession newSession = client.CreateReadWriteSession(Data.TestCatalog);

        // insert a new entity with one of attributes defined in the entity schema
        DateTimeOffset dateTimeNow = DateTimeOffset.Now;
        ISealedEntity newEntity = newSession.UpsertAndFetchEntity(
            new EntityUpsertMutation(
                testCollection,
                null,
                EntityExistence.MayExist,
                new UpsertAttributeMutation(attributeDateTime, dateTimeNow)
            ),
            AttributeContent()
        );

        string dateTimeFormat = "yyyy-MM-dd HH:mm:ss zzz";

        That(newEntity.Schema.Attributes.Count, Is.EqualTo(2));
        That(newEntity.GetAttributeNames().Contains(attributeDateTime), Is.True);
        That(
            (newEntity.GetAttribute(attributeDateTime) as DateTimeOffset?)?.ToString(dateTimeFormat),
            Is.EqualTo(dateTimeNow.ToString(dateTimeFormat))
        );

        // insert a new entity with attribute that is not defined in the entity schema
        ISealedEntity notInAttributeSchemaEntity = newSession.UpsertAndFetchEntity(
            new EntityUpsertMutation(
                testCollection,
                null,
                EntityExistence.MayExist,
                new UpsertAttributeMutation(nonExistingAttribute, true)
            ),
            AttributeContent()
        );

        // schema of the entity should have 3 attributes
        That(notInAttributeSchemaEntity.Schema.Attributes.Count, Is.EqualTo(3));
        That(notInAttributeSchemaEntity.GetAttributeNames().Contains(nonExistingAttribute), Is.True);
        That(notInAttributeSchemaEntity.GetAttribute(nonExistingAttribute), Is.EqualTo(true));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldAllowCreatingCatalogAlongWithTheSchema()
    {
        EvitaClient client = GetClient();
        string someCatalogName = "differentCatalog";
        try
        {
            client.DefineCatalog(someCatalogName)
                .WithDescription("Some description.")
                .UpdateVia(client.CreateReadWriteSession(someCatalogName));
            That(client.GetCatalogNames().Contains(someCatalogName), Is.True);
        }
        finally
        {
            client.DeleteCatalogIfExists(someCatalogName);
        }
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsInPrototypingMode()
    {
        EvitaClient client = GetClient();

        string someCatalogName = "differentCatalog";
        CultureInfo locale = Data.EnglishLocale;
        try
        {
            client.DefineCatalog(someCatalogName)
                .WithDescription("This is a tutorial catalog.")
                .UpdateViaNewSession(client);
            That(client.GetCatalogNames().Contains(someCatalogName), Is.True);
            client.UpdateCatalog(
                someCatalogName,
                session =>
                {
                    session.CreateNewEntity(Entities.Brand, 1)
                        .SetAttribute(Data.AttributeName, locale, "Lenovo")
                        .UpsertVia(session);

                    ISealedEntitySchema? brand = session.GetEntitySchema(Entities.Brand);
                    That(brand is not null, Is.True);

                    IAttributeSchema? nameAttribute = brand?.GetAttribute(Data.AttributeName);
                    That(nameAttribute is not null, Is.True);
                    That(nameAttribute?.Localized, Is.True);

                    // now create an example category tree
                    session.CreateNewEntity(Entities.Category, 10)
                        .SetAttribute(Data.AttributeName, locale, "Electronics")
                        .UpsertVia(session);

                    session.CreateNewEntity(Entities.Category, 11)
                        .SetAttribute(Data.AttributeName, locale, "Laptops")
                        // laptops will be a child category of electronics
                        .SetParent(10)
                        .UpsertVia(session);

                    // finally, create a product
                    session.CreateNewEntity(Entities.Product)
                        // with a few attributes
                        .SetAttribute(Data.AttributeName, locale, "ThinkPad P15 Gen 1")
                        .SetAttribute(Data.AttributeCores, 8)
                        .SetAttribute(Data.AttributeGraphics, "NVIDIA Quadro RTX 4000 with Max-Q Design")
                        // and price for sale
                        .SetPrice(
                            1, Data.PriceListBasic,
                            Data.CurrencyUsd,
                            1420m, 20m, 1704m,
                            true
                        )
                        // link it to the manufacturer
                        .SetReference(
                            Data.ReferenceBrand, Entities.Brand,
                            Cardinality.ExactlyOne,
                            1
                        )
                        // and to the laptop category
                        .SetReference(
                            Data.ReferenceCategories, Entities.Category,
                            Cardinality.ZeroOrMore,
                            11
                        )
                        .UpsertVia(session);

                    ISealedEntitySchema? product = session.GetEntitySchema(Entities.Product);
                    That(product is not null, Is.True);

                    IAttributeSchema? productNameAttribute = product?.GetAttribute(Data.AttributeName);
                    That(productNameAttribute is not null, Is.True);
                    That(productNameAttribute?.Localized, Is.True);
                }
            );
        }
        finally
        {
            client.DeleteCatalogIfExists(someCatalogName);
        }
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsSchemas()
    {
        EvitaClient client = GetClient();

        string someCatalogName = "differentCatalog";
        try
        {
            client.DefineCatalog(someCatalogName)
                .WithDescription("This is a tutorial catalog.")
                // define brand schema
                .WithEntitySchema(
                    Entities.Brand,
                    whichIs => whichIs.WithDescription("A manufacturer of products.")
                        .WithAttribute<string>(
                            Data.AttributeName,
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                )
                // define category schema
                .WithEntitySchema(
                    Entities.Category,
                    whichIs => whichIs.WithDescription("A category of products.")
                        .WithAttribute<string>(
                            Data.AttributeName,
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                        .WithHierarchy()
                )
                // define product schema
                .WithEntitySchema(
                    Entities.Product,
                    whichIs => whichIs.WithDescription("A product in inventory.")
                        .WithAttribute<string>(
                            Data.AttributeName,
                            thatIs => thatIs.Localized().Filterable().Sortable()
                        )
                        .WithAttribute<int>(
                            Data.AttributeCores,
                            thatIs => thatIs.WithDescription("Number of CPU cores.")
                                .Filterable()
                        )
                        .WithAttribute<string>(
                            Data.AttributeGraphics,
                            thatIs => thatIs.WithDescription("Graphics card.")
                                .Filterable()
                        )
                        .WithPrice()
                        .WithReferenceToEntity(
                            Data.ReferenceBrand, Entities.Brand, Cardinality.ExactlyOne,
                            thatIs => thatIs.Indexed()
                        )
                        .WithReferenceToEntity(
                            Data.ReferenceCategories, Entities.Category, Cardinality.ZeroOrMore,
                            thatIs => thatIs.Indexed()
                        )
                )
                // and now push all the definitions (mutations) to the server
                .UpdateViaNewSession(client);
            That(client.GetCatalogNames().Contains(someCatalogName), Is.True);
            client.QueryCatalog(someCatalogName, session =>
            {
                ISet<string> allEntityTypes = session.GetAllEntityTypes();
                That(allEntityTypes.Contains(Entities.Brand), Is.True);
                That(allEntityTypes.Contains(Entities.Category), Is.True);
                That(allEntityTypes.Contains(Entities.Category), Is.True);
            });
        }
        finally
        {
            client.DeleteCatalogIfExists(someCatalogName);
        }
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldBeAbleToRunParallelClients()
    {
        EvitaClient client = GetClient();
        EvitaClient anotherParallelClient = new EvitaClient(client.Configuration);
        ListCatalogNames(anotherParallelClient);
        ListCatalogNames(client);
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldAbleToFetchNonCachedEntitySchemaFromCatalogSchema()
    {
        EvitaClient client = GetClient();
        EvitaClient clientWithEmptyCache = new EvitaClient(client.Configuration);
        clientWithEmptyCache.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntitySchema? productSchema = session.GetCatalogSchema().GetEntitySchema(Entities.Product);
                That(productSchema, Is.Not.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldListCatalogNames()
    {
        EvitaClient client = GetClient();
        ISet<string> catalogNames = ListCatalogNames(client);
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(Data.TestCatalog), Is.True);
        _clients.Enqueue(client);
    }

    private static ISet<string> ListCatalogNames(EvitaClient client)
    {
        return client.GetCatalogNames();
    }

    [Test]
    public void ShouldCreateCatalog()
    {
        EvitaClient client = GetClient();
        string newCatalogName = "newCatalog";
        try
        {
            client.DefineCatalog(newCatalogName);
            ISet<string> catalogNames = client.GetCatalogNames();

            That(catalogNames.Count, Is.EqualTo(2));
            That(catalogNames.Contains(Data.TestCatalog), Is.True);
            That(catalogNames.Contains(newCatalogName), Is.True);
        }
        finally
        {
            client.DeleteCatalogIfExists(newCatalogName);
        }
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldRemoveCatalog()
    {
        EvitaClient client = GetClient();
        string newCatalogName = "newCatalog";
        client.DefineCatalog(newCatalogName).UpdateViaNewSession(client);
        bool removed = client.DeleteCatalogIfExists(newCatalogName);
        That(removed, Is.True);

        ISet<string> catalogNames = client.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(Data.TestCatalog), Is.True);
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldReplaceCatalog()
    {
        EvitaClient client = GetClient();
        string newCatalog = "newCatalog";
        client.DefineCatalog(newCatalog);

        ISet<string> catalogNames = client.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(2));
        That(catalogNames.Contains(newCatalog), Is.True);
        That(catalogNames.Contains(Data.TestCatalog), Is.True);
        int initialSchemaVersion = client.QueryCatalog(Data.TestCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version);

        client.ReplaceCatalog(Data.TestCatalog, newCatalog);

        ISet<string> catalogNamesAgain = client.GetCatalogNames();
        That(catalogNamesAgain.Count, Is.EqualTo(1));
        That(catalogNamesAgain.Contains(newCatalog), Is.True);
        That(
            client.QueryCatalog(newCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version),
            Is.EqualTo(initialSchemaVersion + 1)
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldRenameCatalog()
    {
        EvitaClient client = GetClient();
        string newCatalog = "newCatalog";

        ISet<string> catalogNames = client.GetCatalogNames();
        That(catalogNames.Count, Is.EqualTo(1));
        That(catalogNames.Contains(Data.TestCatalog), Is.True);
        int initialSchemaVersion = client.QueryCatalog(Data.TestCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version);

        client.RenameCatalog(Data.TestCatalog, newCatalog);
        
        ISet<string> catalogNamesAgain = client.GetCatalogNames();
        That(catalogNamesAgain.Count, Is.EqualTo(1));
        That(catalogNamesAgain.Contains(newCatalog), Is.True);
        That(
            client.QueryCatalog(newCatalog, evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version),
            Is.EqualTo(initialSchemaVersion + 1));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldReplaceCollection()
    {
        EvitaClient client = GetClient();
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        client.UpdateCatalog(
            Data.TestCatalog,
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
        client.UpdateCatalog(
            Data.TestCatalog,
            session => session.ReplaceCollection(
                newCollection,
                Entities.Product
            ));
        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.False);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.True);
                That(session.GetEntitySchemaOrThrow(newCollection).Version,
                    Is.EqualTo(productSchemaVersion!.Value + 1));
                That(session.GetEntityCollectionSize(newCollection), Is.EqualTo(productCount!));
            }
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldRenameCollection()
    {
        EvitaClient client = GetClient();
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.True);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.False);
                productSchemaVersion = session.GetEntitySchemaOrThrow(Entities.Product).Version;
                productCount = session.GetEntityCollectionSize(Entities.Product);
            }
        );
        client.UpdateCatalog(
            Data.TestCatalog,
            session => session.RenameCollection(
                Entities.Product,
                newCollection
            ));
        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                That(session.GetAllEntityTypes().Contains(Entities.Product), Is.False);
                That(session.GetAllEntityTypes().Contains(newCollection), Is.True);
                That(session.GetEntitySchemaOrThrow(newCollection).Version,
                    Is.EqualTo(productSchemaVersion!.Value + 1));
                That(session.GetEntityCollectionSize(newCollection), Is.EqualTo(productCount!.Value));
            }
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryCatalog()
    {
        EvitaClient client = GetClient();
        ICatalogSchema catalogSchema = client.QueryCatalog(
            Data.TestCatalog,
            x => x.GetCatalogSchema()
        );
        That(catalogSchema, Is.Not.Null);
        That(catalogSchema.Name, Is.EqualTo(Data.TestCatalog));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryOneEntityReference()
    {
        EvitaClient client = GetClient();
        EntityReference entityReference = client.QueryCatalog(
            Data.TestCatalog,
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
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldNotQueryOneMissingEntity()
    {
        EvitaClient client = GetClient();
        EntityReference? entityReference = client.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryOneEntityReference(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(-100)
                    )
                )
            ));
        That(entityReference, Is.Null);
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryOneSealedEntity()
    {
        EvitaClient client = GetClient();
        IList<ISealedEntity> products = CreatedEntities[Entities.Product];
        int primaryKey = products.ElementAt(Random.Next(products.Count)).PrimaryKey!.Value;
        ISealedEntity sealedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryOneSealedEntity(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(primaryKey)
                    ),
                    Require(
                        EntityFetch(
                            HierarchyContent(),
                            AttributeContentAll(),
                            AssociatedDataContentAll(),
                            PriceContentAll(),
                            ReferenceContentAll(),
                            DataInLocalesAll()
                        )
                    )
                )
            ) ?? throw new EvitaInvalidUsageException("Entity not found"));

        var x = products.Single(x => x.PrimaryKey == primaryKey);
        That(sealedEntity, Is.Not.Null);
        That(sealedEntity.Type, Is.EqualTo(Entities.Product));
        That(sealedEntity, IsExactlyTheSame.DeepEqualTo(products.Single(x=>x.PrimaryKey == primaryKey))
            .WithComparisonConfig(_entityComparisonConfig));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryListOfEntityReferences()
    {
        EvitaClient client = GetClient();
        int[] requestedIds = { 1, 2, 5 };
        IList<EntityReference> entityReferences = client.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryListOfEntityReferences(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(requestedIds)
                    )
                )
            ));

        That(entityReferences, Is.Not.Null);
        That(entityReferences.Count, Is.EqualTo(3));

        for (int i = 0; i < entityReferences.Count; i++)
        {
            EntityReference entityReference = entityReferences.ElementAt(i);
            That(entityReference.Type, Is.EqualTo(Entities.Product));
            That(entityReference.PrimaryKey, Is.EqualTo(requestedIds[i]));
        }
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryListOfSealedEntities()
    {
        EvitaClient client = GetClient();
        int[] requestedIds = { 1, 2, 5 };
        IList<ISealedEntity> products = CreatedEntities[Entities.Product];
        IList<ISealedEntity> sealedEntities = client.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryListOfSealedEntities(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(requestedIds)
                    ),
                    Require(
                        EntityFetchAll()
                    )
                )
            ));

        That(sealedEntities, Is.Not.Null);
        That(sealedEntities.Count, Is.EqualTo(3));

        for (int i = 0; i < sealedEntities.Count; i++)
        {
            ISealedEntity sealedEntity = sealedEntities.ElementAt(i);
            That(sealedEntity.Type, Is.EqualTo(Entities.Product));
            That(sealedEntity.PrimaryKey, Is.EqualTo(requestedIds[i]));
            That(sealedEntity, Is.EqualTo(products.Single(x=>x.PrimaryKey == requestedIds[i])));
        }
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldGetListWithExtraResults()
    {
        EvitaClient client = GetClient();
        ISealedEntity? someProductWithCategory = CreatedEntities[Entities.Product]
            .Where(it => it.GetReferences(Data.ReferenceCategories).Any())
            .FirstOrDefault(it => it.GetAllPricesForSale().Count > 0);
        IList<IPrice> allPricesForSale = someProductWithCategory!.GetAllPricesForSale();
        EvitaResponse<ISealedEntity> result = client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                return session.QuerySealedEntity(
                    Query(
                        Collection(Entities.Product),
                        FilterBy(
                            PriceInPriceLists(allPricesForSale.Select(x => x.PriceList)
                                .ToArray()),
                            PriceInCurrency(allPricesForSale.Select(x => x.Currency)
                                .First()),
                            EntityLocaleEquals(someProductWithCategory.GetAllLocales().First())
                        ),
                        Require(
                            EntityFetchAll(),
                            QueryTelemetry(),
                            PriceHistogram(20),
                            AttributeHistogram(20, Data.AttributeQuantity),
                            HierarchyOfReference(
                                Data.ReferenceCategories,
                                FromRoot(Data.HierarchyReferenceRoot, EntityFetchAll())
                            ),
                            FacetSummary(FacetStatisticsDepth.Impact)
                        )
                    )
                );
            }
        );

        That(result, Is.Not.Null);
        That(result.RecordPage.TotalRecordCount, Is.GreaterThan(0));

        That(result.GetExtraResult<QueryTelemetry>(), Is.Not.Null);

        PriceHistogram? priceHistogram =
            result.GetExtraResult<PriceHistogram>();
        That(priceHistogram, Is.Not.Null);
        That(priceHistogram!.Max.CompareTo(priceHistogram.Min), Is.GreaterThanOrEqualTo(0));
        That(priceHistogram.Buckets.Length, Is.GreaterThan(0));
        That(priceHistogram.Min.CompareTo(0m), Is.GreaterThan(0));

        AttributeHistogram? attributeHistogram =
            result.GetExtraResult<AttributeHistogram>();
        That(attributeHistogram, Is.Not.Null);
        IHistogram? theHistogram = attributeHistogram!.GetHistogram(Data.AttributeQuantity);
        That(theHistogram, Is.Not.Null);
        That(theHistogram!.Max.CompareTo(theHistogram.Min), Is.GreaterThanOrEqualTo(0));
        That(theHistogram.Buckets.Length, Is.GreaterThan(0));
        That(theHistogram.Min.CompareTo(0m), Is.GreaterThan(0));

        Hierarchy? hierarchy = result.GetExtraResult<Hierarchy>();
        That(hierarchy, Is.Not.Null);
        IDictionary<string, List<LevelInfo>>? categoryHierarchy = hierarchy!.GetReferenceHierarchy(Data.ReferenceCategories);
        That(categoryHierarchy, Is.Not.Null);
        That(categoryHierarchy![Data.HierarchyReferenceRoot].Count, Is.GreaterThanOrEqualTo(0));

        FacetSummary? facetSummary =
            result.GetExtraResult<FacetSummary>();
        That(facetSummary, Is.Not.Null);
        That(facetSummary!.GetFacetGroupStatistics().Count, Is.GreaterThan(0));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldGetSingleEntity()
    {
        EvitaClient client = GetClient();
        ISealedEntity? sealedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        That(sealedEntity, Is.Not.Null);

        That(sealedEntity!.Type, Is.EqualTo(Entities.Product));
        That(sealedEntity.PrimaryKey, Is.EqualTo(7));
        That(sealedEntity, Is.EqualTo(CreatedEntities[Entities.Product].Single(x=>x.PrimaryKey == 7)));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldEnrichSingleEntity()
    {
        EvitaClient client = GetClient();
        IList<ISealedEntity> products = CreatedEntities[Entities.Product];
        ISealedEntity? sealedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                AttributeContent()
            ));

        That(sealedEntity!.Type, Is.EqualTo(Entities.Product));
        That(sealedEntity.PrimaryKey, Is.EqualTo(7));
        That(sealedEntity, Is.Not.DeepEqualTo(products.Single(x=>x.PrimaryKey == 7))
            .WithComparisonConfig(_entityComparisonConfig));

        ISealedEntity? enrichedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        That(enrichedEntity!.Type, Is.EqualTo(Entities.Product));
        That(enrichedEntity.PrimaryKey, Is.EqualTo(7));

        ISealedEntity entityToCompare = products.Single(x => x.PrimaryKey == 7);
        
        That(enrichedEntity, IsExactlyTheSame.DeepEqualTo(entityToCompare)
            .WithComparisonConfig(_entityComparisonConfig));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldLimitSingleEntity()
    {
        EvitaClient client = GetClient();
        IList<ISealedEntity> products = CreatedEntities[Entities.Product];
        ISealedEntity? sealedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        That(sealedEntity!.Type, Is.EqualTo(Entities.Product));
        That(sealedEntity.PrimaryKey, Is.EqualTo(7));
        That(sealedEntity, IsExactlyTheSame.DeepEqualTo(products.Single(x=>x.PrimaryKey == 7))
            .WithComparisonConfig(_entityComparisonConfig));

        ISealedEntity? limitedEntity = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                AttributeContent()
            ));

        That(limitedEntity!.Type, Is.EqualTo(Entities.Product));
        That(limitedEntity.PrimaryKey, Is.EqualTo(7));
        That(limitedEntity, Is.Not.DeepEqualTo(products.Single(x=>x.PrimaryKey == 7))
            .WithComparisonConfig(_entityComparisonConfig));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldRetrieveCollectionSize()
    {
        EvitaClient client = GetClient();
        int productCount = client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntityCollectionSize(Entities.Product));

        That(productCount, Is.EqualTo(CreatedEntities[Entities.Product].Count));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldQueryListOfSealedEntitiesEvenWithoutProperRequirements()
    {
        EvitaClient client = GetClient();
        IList<ISealedEntity> sealedEntities = client.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryListOfSealedEntities(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(1, 2, 5)
                    )
                )
            ));
        That(sealedEntities.Count, Is.EqualTo(3));
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldCallTerminationCallbackWhenClientClosesSession()
    {
        EvitaClient client = GetClient();
        Guid? terminatedSessionId = null;
        EvitaClientSession theSession = client.CreateSession(
            new SessionTraits(
                Data.TestCatalog,
                session => terminatedSessionId = session.SessionId
            )
        );
        theSession.Close();
        That(terminatedSessionId, Is.Not.Null);
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldCallTerminationCallbackWhenClientIsClosed()
    {
        EvitaClient client = GetClient();
        Guid? terminatedSessionId = null;
        client.CreateSession(
            new SessionTraits(
                Data.TestCatalog,
                session =>
                {
                    ModifyGuid(ref terminatedSessionId, session.SessionId);
                }
            )
        );
        client.Close();
        That(terminatedSessionId, Is.Not.Null);
    }

    [Test]
    public void ShouldTranslateErrorCorrectlyAndLeaveSessionOpen()
    {
        EvitaClient client = GetClient();
        using EvitaClientSession clientSession = client.CreateReadOnlySession(Data.TestCatalog);
        try
        {
            clientSession.GetEntity("nonExisting", 1, EntityFetchAll().Requirements!);
        }
        catch (EvitaInvalidUsageException ex)
        {
            That(clientSession.Active, Is.True);
            That(ex.PublicMessage, Is.EqualTo("No collection found for entity type `nonExisting`!"));
            That(ex.PrivateMessage, Is.EqualTo(ex.PublicMessage));
            That(ex.ErrorCode, Is.Not.Null);
        }
        finally
        {
            clientSession.Close();
        }
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldReturnEntitySchema()
    {
        EvitaClient client = GetClient();
        EvitaClientSession evitaSession = client.CreateReadOnlySession(Data.TestCatalog);
        That(evitaSession.GetEntitySchema(Entities.Product), Is.Not.Null);
        That(evitaSession.GetEntitySchemaOrThrow(Entities.Product), Is.Not.Null);
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldCreateAndDropEntityCollection()
    {
        EvitaClient client = GetClient();
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                string newEntityType = "newEntityType";
                session.DefineEntitySchema(newEntityType)
                    .WithAttribute<string>(Data.AttributeName, thatIs => thatIs.Localized().Filterable())
                    .UpdateVia(session);

                That(session.GetAllEntityTypes().Contains(newEntityType), Is.True);
                session.DeleteCollection(newEntityType);
                That(session.GetAllEntityTypes().Contains(newEntityType), Is.False);
            }
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldUpsertNewEntity()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);
                That(entityMutation, Is.Not.Null);

                EntityReference newProduct = session.UpsertEntity(entityMutation);
                newProductId = newProduct.PrimaryKey;
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity =
                    session.GetEntity(Entities.Product, newProductId!.Value, EntityFetchAllContent());

                That(loadedEntity, Is.Not.Null);
                AssertSomeNewProductContent(loadedEntity!);
            }
        );

        // reset data
        client.UpdateCatalog(
            Data.TestCatalog,
            session => { session.DeleteEntity(Entities.Product, newProductId!.Value); }
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldUpsertAndFetchNewEntity()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                AssertSomeNewProductContent(updatedEntity);
            }
        );

        // reset data
        client.UpdateCatalog(
            Data.TestCatalog,
            session => { session.DeleteEntity(Entities.Product, newProductId!.Value); }
        );
        
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteExistingEntity()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);
                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;
                session.DeleteEntity(Entities.Product, updatedEntity.PrimaryKey!.Value);
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                That(loadedEntity, Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteAndFetchExistingEntity()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                ISealedEntity? removedEntity = session.DeleteEntity(
                    Entities.Product, updatedEntity.PrimaryKey!.Value, EntityFetchAllContent()
                );

                That(removedEntity, Is.Not.Null);
                AssertSomeNewProductContent(removedEntity!);
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                That(loadedEntity, Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteEntityByQuery()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                int deletedEntities = session.DeleteEntities(
                    Query(
                        Collection(Entities.Product),
                        FilterBy(EntityPrimaryKeyInSet(newProductId!.Value))
                    )
                );

                That(deletedEntities, Is.EqualTo(1));
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                That(loadedEntity, Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteEntitiesAndFetchByQuery()
    {
        EvitaClient client = GetClient();
        int? newProductId = null;
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                ISealedEntity[] deletedEntities = session.DeleteSealedEntitiesAndReturnBodies(
                    Query(
                        Collection(Entities.Product),
                        FilterBy(EntityPrimaryKeyInSet(newProductId!.Value)),
                        Require(EntityFetchAll())
                    )
                );

                That(deletedEntities.Length, Is.EqualTo(1));
                AssertSomeNewProductContent(deletedEntities[0]);
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                That(loadedEntity, Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteHierarchy()
    {
        EvitaClient client = GetClient();
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                CreateSomeNewCategory(session, 50, null);
                CreateSomeNewCategory(session, 51, 50);
                CreateSomeNewCategory(session, 52, 51);
                CreateSomeNewCategory(session, 53, 50);

                int deletedEntities = session.DeleteEntityAndItsHierarchy(
                    Entities.Category, 50
                );

                That(deletedEntities, Is.EqualTo(4));
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                That(session.GetEntity(Entities.Category, 50, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 51, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 52, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 53, EntityFetchAllContent()), Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldDeleteHierarchyAndFetchRoot()
    {
        EvitaClient client = GetClient();
        client.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                CreateSomeNewCategory(session, 50, null);
                CreateSomeNewCategory(session, 51, 50);
                CreateSomeNewCategory(session, 52, 51);
                CreateSomeNewCategory(session, 53, 50);

                DeletedHierarchy<ISealedEntity> deletedHierarchy = session.DeleteEntityAndItsHierarchy(
                    Entities.Category, 50, EntityFetchAllContent()
                );

                That(deletedHierarchy.DeletedEntities, Is.EqualTo(4));
                That(deletedHierarchy.DeletedRootEntity, Is.Not.Null);
                That(deletedHierarchy.DeletedRootEntity!.PrimaryKey, Is.EqualTo(50));
                That(deletedHierarchy.DeletedRootEntity.GetAttribute(Data.AttributeName, Data.EnglishLocale),
                    Is.EqualTo("New category #50"));
            }
        );

        client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                That(session.GetEntity(Entities.Category, 50, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 51, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 52, EntityFetchAllContent()), Is.Null);
                That(session.GetEntity(Entities.Category, 53, EntityFetchAllContent()), Is.Null);
            }
        );
        _clients.Enqueue(client);
    }

    [Test]
    public void ShouldThrowWhenAddingEntityThatViolatesSchema()
    {
        EvitaClient client = GetClient();
        Throws<InvalidDataTypeMutationException>(() => CreateProductThatViolatesSchema(client, Entities.Product));
        _clients.Enqueue(client);
    }
    
    /*[Test]
    public void ShouldTestCdc()
    {
        IObservable<ChangeSystemCapture> captures =
            _client!.RegisterSystemChangeCapture(new ChangeSystemCaptureRequest(CaptureContent.Header));
        IDisposable subscription = captures.Subscribe(c => { Console.WriteLine(c.Operation); });
        subscription.Dispose();
    }*/

    private IEntitySchema CreateProductSchema(EvitaClientSession session, string entityType)
    {
        IEntitySchemaBuilder builder = session.DefineEntitySchema(entityType)
            .WithAttribute<string>(Data.AttributeName,
                whichIs => whichIs.Filterable().Sortable().Localized()
                    .WithDescription("This describes the attribute `name`"))
            .WithAttribute<DateTimeOffset[]>(Data.AttributeValidity,
                whichIs => whichIs.Nullable())
            .WithAttribute<decimal>(Data.AttributeQuantity, whichIs => whichIs.Filterable().Nullable().IndexDecimalPlaces(2))
            .WithAssociatedData<TestAsDataObj[]>(Data.AssociatedDataReferencedFiles,
                whichIs => whichIs.Nullable().Localized())
            .WithLocale(Data.CzechLocale, Data.EnglishLocale)
            .WithReferenceTo(Data.ReferenceRelatedProducts, entityType, Cardinality.ZeroOrMore,
                whichIs => whichIs.Indexed().Faceted().WithGroupType(Data.ReferenceGroupType)
                    .WithAttribute<int>(Data.AttributePriority, attr => attr.Sortable()))
            .WithReferenceToEntity(Data.ReferenceCategories, Entities.Category, Cardinality.ZeroOrMore,
                whichIs => whichIs.Indexed().WithGroupType(Data.ReferenceGroupType))
            .WithPrice()
            .WithoutHierarchy()
            .WithGeneratedPrimaryKey();

        ISealedEntitySchema schema = builder.UpdateAndFetchVia(session);

        IAttributeSchema? nameAttributeSchema = schema.GetAttribute(Data.AttributeName);
        That(nameAttributeSchema, Is.Not.Null);
        That(nameAttributeSchema!.Type, Is.EqualTo(typeof(string)));
        That(nameAttributeSchema.Filterable, Is.True);
        That(nameAttributeSchema.Sortable, Is.True);
        That(nameAttributeSchema.Nullable, Is.False);
        That(nameAttributeSchema.Localized, Is.True);
        That(nameAttributeSchema.Unique, Is.False);
        That(nameAttributeSchema.Description, Is.Not.Null.Or.Empty);

        IAttributeSchema? validityAttributeSchema = schema.GetAttribute(Data.AttributeValidity);
        That(validityAttributeSchema, Is.Not.Null);
        That(validityAttributeSchema!.Type, Is.EqualTo(typeof(DateTimeOffset[])));
        That(validityAttributeSchema.Filterable, Is.False);
        That(validityAttributeSchema.Sortable, Is.False);
        That(validityAttributeSchema.Nullable, Is.True);
        That(validityAttributeSchema.Localized, Is.False);
        That(validityAttributeSchema.Unique, Is.False);
        That(validityAttributeSchema.Description, Is.Null.Or.Empty);
        
        IAttributeSchema? quantityAttributeSchema = schema.GetAttribute(Data.AttributeQuantity);
        That(quantityAttributeSchema, Is.Not.Null);
        That(quantityAttributeSchema!.Type, Is.EqualTo(typeof(decimal)));
        That(quantityAttributeSchema.Filterable, Is.True);
        That(quantityAttributeSchema.Sortable, Is.False);
        That(quantityAttributeSchema.Nullable, Is.True);
        That(quantityAttributeSchema.Localized, Is.False);
        That(quantityAttributeSchema.Unique, Is.False);
        That(quantityAttributeSchema.Description, Is.Null.Or.Empty);

        IAttributeSchema? aliasAttributeSchema = schema.GetAttribute(Data.AttributeAlias);
        That(aliasAttributeSchema, Is.Null);

        IAssociatedDataSchema? associatedDataSchema = schema.GetAssociatedData(Data.AssociatedDataReferencedFiles);
        That(associatedDataSchema, Is.Not.Null);
        That(associatedDataSchema!.Type, Is.EqualTo(typeof(ComplexDataObject)));
        That(associatedDataSchema.Localized, Is.True);
        That(associatedDataSchema.Nullable, Is.True);

        IAssociatedDataSchema? nonExistingAssociatedDataSchema = schema.GetAssociatedData(Data.AssociatedDataLabels);
        That(nonExistingAssociatedDataSchema, Is.Null);

        IReferenceSchema? relatedProductsReferenceSchema = schema.GetReference(Data.ReferenceRelatedProducts);
        That(relatedProductsReferenceSchema, Is.Not.Null);
        That(relatedProductsReferenceSchema!.Cardinality, Is.EqualTo(Cardinality.ZeroOrMore));
        That(relatedProductsReferenceSchema.IsIndexed, Is.True);
        That(relatedProductsReferenceSchema.ReferencedEntityType, Is.EqualTo(entityType));
        That(relatedProductsReferenceSchema.ReferencedGroupType, Is.EqualTo(Data.ReferenceGroupType));
        
        IReferenceSchema? categoriesReferenceSchema = schema.GetReference(Data.ReferenceCategories);
        That(categoriesReferenceSchema, Is.Not.Null);
        That(categoriesReferenceSchema!.Cardinality, Is.EqualTo(Cardinality.ZeroOrMore));
        That(categoriesReferenceSchema.IsIndexed, Is.True);
        That(categoriesReferenceSchema.ReferencedEntityType, Is.EqualTo(Entities.Category));
        That(categoriesReferenceSchema.ReferencedGroupType, Is.EqualTo(Data.ReferenceGroupType));

        IAttributeSchema? referenceAttributeSchema = relatedProductsReferenceSchema.GetAttribute(Data.AttributePriority);
        That(referenceAttributeSchema, Is.Not.Null);
        That(referenceAttributeSchema!.Filterable, Is.False);
        That(referenceAttributeSchema.Sortable, Is.True);

        That(categoriesReferenceSchema.GetAttribute(Data.AttributeQuantity), Is.Null);

        That(schema.WithPrice, Is.True);
        That(schema.WithHierarchy, Is.False);
        That(schema.WithGeneratedPrimaryKey, Is.True);

        return schema;
    }

    private IList<ISealedEntity> CreateProductsThatMatchSchema(EvitaClientSession session, string entityType, int count)
    {
        List<ISealedEntity> entities = new List<ISealedEntity>();

        for (int i = 0; i < count; i++)
        {
            IEntityBuilder builder = session.CreateNewEntity(entityType);

            string enAttributeName = "name-" + Data.EnglishLocale.TwoLetterISOLanguageName;
            string csAttributeName = "name-" + Data.CzechLocale.TwoLetterISOLanguageName;
            builder.SetAttribute(Data.AttributeName, Data.EnglishLocale, enAttributeName);
            builder.SetAttribute(Data.AttributeName, Data.CzechLocale, csAttributeName);

            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset dateTimeOffset =
                new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Offset);

            DateTimeOffset[] attributeValidity =
                { dateTimeOffset, dateTimeOffset.AddDays(7), dateTimeOffset.AddDays(14) };
            builder.SetAttribute(Data.AttributeValidity, attributeValidity);

            string attributeCode = Guid.NewGuid().ToString();
            builder.SetAttribute(Data.AttributeCode, attributeCode);
            
            decimal quantity = 5.5m;
            builder.SetAttribute(Data.AttributeQuantity, quantity);

            TestAsDataObj[] asData =
            {
                new("cs", "/cs/macbook-pro-13-2022"),
                new("en", "/en/macbook-pro-13-2022")
            };

            builder.SetAssociatedData(Data.AssociatedDataReferencedFiles, Data.EnglishLocale, asData);
            builder.SetAssociatedData(Data.AssociatedDataReferencedFiles, Data.CzechLocale, asData);

            IPrice price = new Price(new PriceKey(1, "basic", new Currency("CZK")), null, 100, 15, 115,
                DateTimeRange.Between(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(7)), true);
            builder.SetPrice(price.PriceId, price.PriceList, price.Currency, price.PriceWithoutTax, price.TaxRate,
                price.PriceWithTax, price.Validity, price.Sellable);

            PriceInnerRecordHandling priceInnerRecordHandling = PriceInnerRecordHandling.FirstOccurrence;
            builder.SetPriceInnerRecordHandling(priceInnerRecordHandling);

            GroupEntityReference groupEntityReference = new GroupEntityReference(Data.ReferenceGroupType, 1, 1);

            int referencePrimaryKey = 2;
            builder.SetReference(Data.ReferenceRelatedProducts, referencePrimaryKey, referenceBuilder =>
                referenceBuilder
                    .SetGroup(groupEntityReference.ReferencedEntity, groupEntityReference.ReferencedEntityPrimaryKey)
                    .SetAttribute(Data.AttributePriority, 5));
            IReference reference = builder.GetReference(Data.ReferenceRelatedProducts, referencePrimaryKey)!;
            
            builder.SetReference(Data.ReferenceCategories, referencePrimaryKey);

            IReference categoriesReference = builder.GetReference(Data.ReferenceCategories, referencePrimaryKey)!;

            AtomicReference<EntityReference> entityReference = new AtomicReference<EntityReference>();
            DoesNotThrow(() => entityReference.Value = builder.UpsertVia(session));

            ISealedEntity? entity = session.GetEntity(entityReference.Value!.Type,
                entityReference.Value.PrimaryKey!.Value, EntityFetchAllContent());

            That(entity, Is.Not.Null);
            That(entity!.GetAttribute(Data.AttributeName, Data.EnglishLocale), Is.EqualTo(enAttributeName));
            That(entity.GetAttribute(Data.AttributeName, Data.CzechLocale), Is.EqualTo(csAttributeName));
            That(entity.GetAttribute(Data.AttributeCode), Is.EqualTo(attributeCode));
            That(entity.GetAttribute(Data.AttributeQuantity), Is.EqualTo(quantity));
            That(entity.GetAttributeArray(Data.AttributeValidity), Is.EqualTo(attributeValidity));
            That(entity.GetAssociatedData(Data.AssociatedDataReferencedFiles, Data.EnglishLocale)?.GetType(),
                Is.EqualTo(typeof(ComplexDataObject)));
            That(entity.GetAssociatedData(Data.AssociatedDataReferencedFiles, Data.CzechLocale)?.GetType(),
                Is.EqualTo(typeof(ComplexDataObject)));
            That(entity.GetAssociatedData<TestAsDataObj[]>(Data.AssociatedDataReferencedFiles, Data.EnglishLocale),
                Is.EqualTo(asData));
            That(entity.GetAssociatedData<TestAsDataObj[]>(Data.AssociatedDataReferencedFiles, Data.CzechLocale),
                Is.EqualTo(asData));
            That(entity.GetPrice(price.Key), Is.EqualTo(price));
            That(entity.InnerRecordHandling, Is.EqualTo(priceInnerRecordHandling));
            That(entity.GetReference(reference.ReferenceName, reference.ReferencedPrimaryKey), Is.EqualTo(reference));
            That(entity.GetReference(categoriesReference.ReferenceName, categoriesReference.ReferencedPrimaryKey), Is.EqualTo(categoriesReference));
            
            entities.Add(entity);
        }

        return entities;
    }

    private void CreateProductThatViolatesSchema(EvitaClient client, string entityType)
    {
        using EvitaClientSession rwSession = client.CreateReadWriteSession(Data.TestCatalog);
        IEntityBuilder builder = rwSession.CreateNewEntity(entityType);
        builder.SetAttribute(Data.AttributeName, Data.EnglishLocale, 5);
        builder.UpsertVia(rwSession);
    }

    private ISealedEntity? CreateSomeNewCategory(EvitaClientSession session, int primaryKey, int? parentPrimaryKey = null)
    {
        IEntityBuilder builder = session.CreateNewEntity(Entities.Category, primaryKey)
            .SetAttribute(Data.AttributeName, Data.EnglishLocale, "New category #" + primaryKey)
            .SetAttribute(Data.AttributeCode, "category-" + primaryKey)
            .SetAttribute(Data.AttributePriority, (long)primaryKey);

        if (parentPrimaryKey == null)
        {
            builder.RemoveParent();
        }
        else
        {
            builder.SetParent(parentPrimaryKey.Value);
        }

        builder.UpsertVia(session);

        return session.GetEntity(Entities.Category, primaryKey, EntityFetchAllContent());
    }

    private static IEntityMutation CreateSomeNewProduct(EvitaClientSession session)
    {
        return session.CreateNewEntity(Entities.Product)
            .SetAttribute(Data.AttributeName, Data.EnglishLocale, "New product")
            .SetAttribute(Data.AttributeCode, "product-" + (session.GetEntityCollectionSize(Entities.Product) + 1))
            .ToMutation() ?? throw new EvitaInternalError("Cannot create product mutation");
    }

    private void DeleteCreateAndSetupCatalog(EvitaClient client, string catalogName, bool checkDelete = false, bool fillData = false)
    {
        if (!checkDelete)
        {
            _ = client.DeleteCatalogIfExists(catalogName);
        }
        client.DefineCatalog(catalogName);
        client.UpdateCatalog(catalogName, session =>
        {
            session.GetCatalogSchema().OpenForWrite()
                .WithAttribute<string>(Data.AttributeCode, thatIs => thatIs.UniqueGlobally())
                .UpdateVia(session);
            if (session.CatalogState == CatalogState.WarmingUp)
            {
                session.GoLiveAndClose();
            }
        });
        
        using EvitaClientSession session = client.CreateReadWriteSession(Data.TestCatalog);
        ISet<string> allEntityTypes = session.GetAllEntityTypes();
        
        if (!allEntityTypes.Contains(Entities.Category) || session.GetEntityCollectionSize(Entities.Category) == 0)
        {
            ISealedEntity category1 = CreateSomeNewCategory(session, 1, null)!;
            ISealedEntity category2 = CreateSomeNewCategory(session, 2, 1)!;
            if (fillData)
            {
                if (!CreatedEntities.TryGetValue(Entities.Category, out IList<ISealedEntity>? categoryEntities) ||
                    !categoryEntities.Any())
                {
                    CreatedEntities.TryAdd(Entities.Category, new List<ISealedEntity> { category1, category2 });
                } 
            }
            
        }
        
        if (!allEntityTypes.Contains(Entities.Product) || session.GetEntityCollectionSize(Entities.Product) == 0)
        {
            IEntitySchema productSchema = CreateProductSchema(session, Entities.Product);
            IList<ISealedEntity> products = CreateProductsThatMatchSchema(session, Entities.Product, 10);

            if (fillData)
            {
                if (!CreatedSchemas.TryGetValue(Entities.Product, out _))
                {
                    CreatedSchemas.TryAdd(Entities.Product, productSchema);
                }
            
                if (!CreatedEntities.TryGetValue(Entities.Product, out IList<ISealedEntity>? productEntities) || !productEntities.Any())
                {
                    CreatedEntities.TryAdd(Entities.Product, products);
                }
            }
        }
    }

    private static void AssertSomeNewProductContent(ISealedEntity loadedEntity)
    {
        That(loadedEntity.PrimaryKey, Is.Not.Null);
        That(loadedEntity.GetAttribute(Data.AttributeName, Data.EnglishLocale), Is.EqualTo("New product"));
    }

    private static void ModifyGuid(ref Guid? guid, Guid newValue)
    {
        guid = newValue;
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var (client, container) in _clientContainers)
        {
            client.Close();
            await container.StopAsync();
        }
    }

    private record TestAsDataObj(string Locale, string Url)
    {
        public TestAsDataObj() : this("", "")
        {
        }
    }
}