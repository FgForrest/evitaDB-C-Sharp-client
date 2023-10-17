using System.Globalization;
using EvitaDB.Client;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB.Client.Session;
using EvitaDB.TestX.Utils;
using Xunit.Abstractions;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.TestX;

public class EvitaClientWriteTest : BaseTest
{
    public EvitaClientWriteTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
    : base(outputHelper, setupFixture)
    {
    }

    [Fact]
    public void ShouldBeAbleToCreateCatalogAndEntitySchemaAndInsertNewEntityWithAttribute()
    {
        string testCollection = "testingCollection";
        string attributeDateTime = "attrDateTime";
        string attributeDecimalRange = "attrDecimalRange";
        string nonExistingAttribute = "nonExistingAttribute";

        // delete test catalog if it exists
        _client!.DeleteCatalogIfExists(Data.TestCatalog);

        // define new catalog
        ICatalogSchema catalogSchema = _client.DefineCatalog(Data.TestCatalog).ToInstance();
        Assert.Equal(Data.TestCatalog, catalogSchema.Name);

        using (EvitaClientSession rwSession = _client.CreateReadWriteSession(Data.TestCatalog))
        {
            // create a new entity schema
            catalogSchema = rwSession.UpdateAndFetchCatalogSchema(new CreateEntitySchemaMutation(testCollection));

            Assert.NotNull(catalogSchema.GetEntitySchema(testCollection));
            Assert.Equal(1, catalogSchema.GetEntitySchema(testCollection)!.Version);
            Assert.Equal(2, catalogSchema.Version);

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
            Assert.Equal(2, catalogSchema.Version);
            Assert.Equal(3, catalogSchema.GetEntitySchema(testCollection)!.Version);

            // check if the entity schema has the two attributes
            ISealedEntitySchema? entitySchema = rwSession.GetEntitySchema(testCollection);
            Assert.NotNull(entitySchema);
            Assert.Equal(2, entitySchema.Attributes.Count);
            Assert.Equal(3, entitySchema.Version);
            Assert.True(entitySchema.Attributes.ContainsKey(attributeDateTime));
            Assert.Equal(typeof(DateTimeOffset), entitySchema.Attributes[attributeDateTime].Type);
            Assert.True(entitySchema.Attributes.ContainsKey(attributeDecimalRange));
            Assert.Equal(typeof(DecimalNumberRange), entitySchema.Attributes[attributeDecimalRange].Type);

            // close the session and switch catalog to the alive state
            rwSession.GoLiveAndClose();
        }

        using EvitaClientSession newSession = _client.CreateReadWriteSession(Data.TestCatalog);

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
        
        Assert.Equal(2, newEntity.Schema.Attributes.Count);
        Assert.Contains(attributeDateTime, newEntity.GetAttributeNames());
        Assert.Equal(dateTimeNow.ToString(DateTimeFormat),
            (newEntity.GetAttribute(attributeDateTime) as DateTimeOffset?)?.ToString(DateTimeFormat));

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
        Assert.Equal(3, notInAttributeSchemaEntity.Schema.Attributes.Count);
        Assert.Contains(nonExistingAttribute, notInAttributeSchemaEntity.GetAttributeNames());
        Assert.Equal(true, notInAttributeSchemaEntity.GetAttribute(nonExistingAttribute));
    }

    [Fact]
    public void ShouldAllowCreatingCatalogAlongWithTheSchema()
    {
        string someCatalogName = "differentCatalog";
        try
        {
            _client!.DefineCatalog(someCatalogName)
                .WithDescription("Some description.")
                .UpdateVia(_client.CreateReadWriteSession(someCatalogName));
            Assert.Contains(someCatalogName, _client.GetCatalogNames());
        }
        finally
        {
            _client!.DeleteCatalogIfExists(someCatalogName);
        }
    }

    [Fact]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsInPrototypingMode()
    {
        string someCatalogName = "differentCatalog";
        CultureInfo locale = Data.EnglishLocale;
        try
        {
            _client!.DefineCatalog(someCatalogName)
                .WithDescription("This is a tutorial catalog.")
                .UpdateViaNewSession(_client);
            Assert.Contains(someCatalogName, _client.GetCatalogNames());
            _client.UpdateCatalog(
                someCatalogName,
                session =>
                {
                    session.CreateNewEntity(Entities.Brand, 1)
                        .SetAttribute(Data.AttributeName, locale, "Lenovo")
                        .UpsertVia(session);

                    ISealedEntitySchema? brand = session.GetEntitySchema(Entities.Brand);
                    Assert.NotNull(brand);

                    IAttributeSchema? nameAttribute = brand.GetAttribute(Data.AttributeName);
                    Assert.NotNull(nameAttribute);
                    Assert.True(nameAttribute.Localized);

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
                    Assert.NotNull(product);

                    IAttributeSchema? productNameAttribute = product.GetAttribute(Data.AttributeName);
                    Assert.NotNull(productNameAttribute);
                    Assert.True(productNameAttribute.Localized);
                }
            );
        }
        finally
        {
            _client!.DeleteCatalogIfExists(someCatalogName);
        }
    }

    [Fact]
    public void ShouldAllowCreatingCatalogAndEntityCollectionsSchemas()
    {
        string someCatalogName = "differentCatalog";
        try
        {
            _client!.DefineCatalog(someCatalogName)
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
                .UpdateViaNewSession(_client);
            Assert.Contains(someCatalogName, _client.GetCatalogNames());
            _client.QueryCatalog(someCatalogName, session =>
            {
                ISet<string> allEntityTypes = session.GetAllEntityTypes();
                Assert.Contains(Entities.Brand, allEntityTypes);
                Assert.Contains(Entities.Product, allEntityTypes);
                Assert.Contains(Entities.Category, allEntityTypes);
            });
        }
        finally
        {
            _client!.DeleteCatalogIfExists(someCatalogName);
        }
    }

    

    [Fact]
    public void ShouldBeAbleToFetchNonCachedEntitySchemaFromCatalogSchema()
    {
        EvitaClient clientWithEmptyCache = new EvitaClient(_client!.Configuration);
        clientWithEmptyCache.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntitySchema? productSchema = session.GetCatalogSchema().GetEntitySchema(Entities.Product);
                Assert.NotNull(productSchema);
            }
        );
    }

    [Fact]
    public void ShouldCreateCatalog()
    {
        string newCatalogName = "newCatalog";
        try
        {
            _client!.DefineCatalog(newCatalogName);
            ISet<string> catalogNames = _client.GetCatalogNames();

            Assert.Equal(2 , catalogNames.Count);
            Assert.Contains(Data.TestCatalog, catalogNames);
            Assert.Contains(newCatalogName, catalogNames);
        }
        finally
        {
            _client!.DeleteCatalogIfExists(newCatalogName);
        }
    }

    [Fact]
    public void ShouldRemoveCatalog()
    {
        string newCatalogName = "newCatalog";
        _client!.DefineCatalog(newCatalogName).UpdateViaNewSession(_client);
        bool removed = _client.DeleteCatalogIfExists(newCatalogName);
        Assert.True(removed);

        ISet<string> catalogNames = _client.GetCatalogNames();
        Assert.Equal(1, catalogNames.Count);
        Assert.Contains(Data.TestCatalog, catalogNames);
    }

    [Fact]
    public void ShouldReplaceCatalog()
    {
        string newCatalog = "newCatalog";
        _client!.DefineCatalog(newCatalog);

        ISet<string> catalogNames = _client.GetCatalogNames();
        Assert.Equal(2, catalogNames.Count);
        Assert.Contains(newCatalog, catalogNames);
        Assert.Contains(Data.TestCatalog, catalogNames);
        int initialSchemaVersion = _client.QueryCatalog(Data.TestCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version);

        _client.ReplaceCatalog(Data.TestCatalog, newCatalog);

        ISet<string> catalogNamesAgain = _client.GetCatalogNames();
        Assert.Equal(1, catalogNamesAgain.Count);
        Assert.Contains(newCatalog, catalogNamesAgain);
        Assert.Equal(initialSchemaVersion + 1, _client.QueryCatalog(newCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version));
    }

    [Fact]
    public void ShouldRenameCatalog()
    {
        string newCatalog = "newCatalog";

        ISet<string> catalogNames = _client!.GetCatalogNames();
        Assert.Equal(1, catalogNames.Count);
        Assert.Contains(Data.TestCatalog, catalogNames);
        int initialSchemaVersion = _client.QueryCatalog(Data.TestCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version);

        _client.RenameCatalog(Data.TestCatalog, newCatalog);

        ISet<string> catalogNamesAgain = _client.GetCatalogNames();
        Assert.Equal(1, catalogNamesAgain.Count);
        Assert.Contains(newCatalog, catalogNamesAgain);
        Assert.Equal(initialSchemaVersion + 1, _client.QueryCatalog(newCatalog,
            evitaSessionContract => evitaSessionContract.GetCatalogSchema().Version));
        _ = _client.DeleteCatalogIfExists(newCatalog);
    }

    [Fact]
    public void ShouldReplaceCollection()
    {
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.Contains(Entities.Product, session.GetAllEntityTypes());
                Assert.DoesNotContain(newCollection, session.GetAllEntityTypes());
                session.DefineEntitySchema(newCollection)
                    .WithGlobalAttribute(Data.AttributeCode)
                    .UpdateVia(session);
                Assert.Contains(newCollection, session.GetAllEntityTypes());
                productSchemaVersion = session.GetEntitySchemaOrThrow(Entities.Product).Version;
                productCount = session.GetEntityCollectionSize(Entities.Product);
            }
        );
        _client.UpdateCatalog(
            Data.TestCatalog,
            session => session.ReplaceCollection(
                newCollection,
                Entities.Product
            ));
        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.DoesNotContain(Entities.Product, session.GetAllEntityTypes());
                Assert.Contains(newCollection, session.GetAllEntityTypes());
                Assert.Equal(productSchemaVersion!.Value + 1,
                    session.GetEntitySchemaOrThrow(newCollection).Version);
                Assert.Equal(productCount!.Value, session.GetEntityCollectionSize(newCollection));
            }
        );
    }

    [Fact]
    public void ShouldRenameCollection()
    {
        string newCollection = "newCollection";
        int? productCount = null;
        int? productSchemaVersion = null;
        _client!.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.Contains(Entities.Product, session.GetAllEntityTypes());
                Assert.DoesNotContain(newCollection, session.GetAllEntityTypes());
                productSchemaVersion = session.GetEntitySchemaOrThrow(Entities.Product).Version;
                productCount = session.GetEntityCollectionSize(Entities.Product);
            }
        );
        _client.UpdateCatalog(
            Data.TestCatalog,
            session => session.RenameCollection(
                Entities.Product,
                newCollection
            ));
        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.DoesNotContain(Entities.Product, session.GetAllEntityTypes());
                Assert.Contains(newCollection, session.GetAllEntityTypes());
                Assert.Equal(productSchemaVersion!.Value + 1,
                    session.GetEntitySchemaOrThrow(newCollection).Version);
                Assert.Equal(productCount!.Value, session.GetEntityCollectionSize(newCollection));
            }
        );
    }

    [Fact]
    public void ShouldCallTerminationCallbackWhenClientClosesSession()
    {
        Guid? terminatedSessionId = null;
        EvitaClientSession theSession = _client!.CreateSession(
            new SessionTraits(
                Data.TestCatalog,
                session => terminatedSessionId = session.SessionId
            )
        );
        theSession.Close();
        Assert.NotNull(terminatedSessionId);
    }

    [Fact]
    public void ShouldCallTerminationCallbackWhenClientIsClosed()
    {
        Guid? terminatedSessionId = null;
        _client!.CreateSession(
            new SessionTraits(
                Data.TestCatalog,
                session => { ModifyGuid(ref terminatedSessionId, session.SessionId); }
            )
        );
        _client.Close();
        Assert.NotNull(terminatedSessionId);
    }

    [Fact]
    public void ShouldTranslateErrorCorrectlyAndLeaveSessionOpen()
    {
        using EvitaClientSession clientSession = _client!.CreateReadOnlySession(Data.TestCatalog);
        try
        {
            clientSession.GetEntity("nonExisting", 1, EntityFetchAll().Requirements!);
        }
        catch (EvitaInvalidUsageException ex)
        {
            Assert.True(clientSession.Active);
            Assert.Equal("No collection found for entity type `nonExisting`!", ex.PublicMessage);
            Assert.Equal(ex.PublicMessage, ex.PrivateMessage);
            Assert.NotNull(ex.ErrorCode);
        }
        finally
        {
            clientSession.Close();
        }
    }

    [Fact]
    public void ShouldReturnEntitySchema()
    {
        EvitaClientSession evitaSession = _client!.CreateReadOnlySession(Data.TestCatalog);
        Assert.NotNull(evitaSession.GetEntitySchema(Entities.Product));
        Assert.NotNull(evitaSession.GetEntitySchemaOrThrow(Entities.Product));
    }

    [Fact]
    public void ShouldCreateAndDropEntityCollection()
    {
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                string newEntityType = "newEntityType";
                session.DefineEntitySchema(newEntityType)
                    .WithAttribute<string>(Data.AttributeName, thatIs => thatIs.Localized().Filterable())
                    .UpdateVia(session);

                Assert.Contains(newEntityType, session.GetAllEntityTypes());
                session.DeleteCollection(newEntityType);
                Assert.DoesNotContain(newEntityType, session.GetAllEntityTypes());
            }
        );
    }

    [Fact]
    public void ShouldUpsertNewEntity()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);
                Assert.NotNull(entityMutation);

                EntityReference newProduct = session.UpsertEntity(entityMutation);
                newProductId = newProduct.PrimaryKey;
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity =
                    session.GetEntity(Entities.Product, newProductId!.Value, EntityFetchAllContent());

                Assert.NotNull(loadedEntity);
                AssertSomeNewProductContent(loadedEntity);
            }
        );

        // reset data
        _client.UpdateCatalog(
            Data.TestCatalog,
            session => { session.DeleteEntity(Entities.Product, newProductId!.Value); }
        );
    }

    [Fact]
    public void ShouldUpsertAndFetchNewEntity()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                AssertSomeNewProductContent(updatedEntity);
            }
        );

        // reset data
        _client.UpdateCatalog(
            Data.TestCatalog,
            session => { session.DeleteEntity(Entities.Product, newProductId!.Value); }
        );
    }

    [Fact]
    public void ShouldDeleteExistingEntity()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);
                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;
                session.DeleteEntity(Entities.Product, updatedEntity.PrimaryKey!.Value);
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                Assert.Null(loadedEntity);
            }
        );
    }

    [Fact]
    public void ShouldDeleteAndFetchExistingEntity()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);

                ISealedEntity updatedEntity = session.UpsertAndFetchEntity(
                    entityMutation, EntityFetchAll().Requirements!
                );
                newProductId = updatedEntity.PrimaryKey;

                ISealedEntity? removedEntity = session.DeleteEntity(
                    Entities.Product, updatedEntity.PrimaryKey!.Value, EntityFetchAllContent()
                );

                Assert.NotNull(removedEntity);
                AssertSomeNewProductContent(removedEntity);
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                Assert.Null(loadedEntity);
            }
        );
    }

    [Fact]
    public void ShouldDeleteEntityByQuery()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);

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

                Assert.Equal(1, deletedEntities);
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                Assert.Null(loadedEntity);
            }
        );
    }

    [Fact]
    public void ShouldDeleteEntitiesAndFetchByQuery()
    {
        int? newProductId = null;
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);

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

                Assert.Single(deletedEntities);
                AssertSomeNewProductContent(deletedEntities[0]);
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                ISealedEntity? loadedEntity = session.GetEntity(
                    Entities.Product, newProductId!.Value, EntityFetchAllContent()
                );
                Assert.Null(loadedEntity);
            }
        );
    }

    [Fact]
    public void ShouldDeleteHierarchy()
    {
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                DataManipulationUtil.CreateSomeNewCategory(session, 50, null);
                DataManipulationUtil.CreateSomeNewCategory(session, 51, 50);
                DataManipulationUtil.CreateSomeNewCategory(session, 52, 51);
                DataManipulationUtil.CreateSomeNewCategory(session, 53, 50);

                int deletedEntities = session.DeleteEntityAndItsHierarchy(
                    Entities.Category, 50
                );

                Assert.Equal(4, deletedEntities);
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.Null(session.GetEntity(Entities.Category, 50, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 51, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 52, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 53, EntityFetchAllContent()));
            }
        );
    }

    [Fact]
    public void ShouldDeleteHierarchyAndFetchRoot()
    {
        _client!.UpdateCatalog(
            Data.TestCatalog,
            session =>
            {
                DataManipulationUtil.CreateSomeNewCategory(session, 50, null);
                DataManipulationUtil.CreateSomeNewCategory(session, 51, 50);
                DataManipulationUtil.CreateSomeNewCategory(session, 52, 51);
                DataManipulationUtil.CreateSomeNewCategory(session, 53, 50);

                DeletedHierarchy<ISealedEntity> deletedHierarchy = session.DeleteEntityAndItsHierarchy(
                    Entities.Category, 50, EntityFetchAllContent()
                );

                Assert.Equal(4, deletedHierarchy.DeletedEntities);
                Assert.NotNull(deletedHierarchy.DeletedRootEntity);
                Assert.Equal(50, deletedHierarchy.DeletedRootEntity!.PrimaryKey);
                Assert.Equal("New category #50",
                    deletedHierarchy.DeletedRootEntity.GetAttribute(Data.AttributeName, Data.EnglishLocale));
            }
        );

        _client.QueryCatalog(
            Data.TestCatalog,
            session =>
            {
                Assert.Null(session.GetEntity(Entities.Category, 50, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 51, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 52, EntityFetchAllContent()));
                Assert.Null(session.GetEntity(Entities.Category, 53, EntityFetchAllContent()));
            }
        );
    }

    [Fact]
    public void ShouldThrowWhenAddingEntityThatViolatesSchema()
    {
        Assert.Throws<InvalidDataTypeMutationException>(() => DataManipulationUtil.CreateProductThatViolatesSchema(_client!, Entities.Product));
    }

    private static void AssertSomeNewProductContent(ISealedEntity loadedEntity)
    {
        Assert.NotNull(loadedEntity);
        Assert.Equal("New product", loadedEntity.GetAttribute(Data.AttributeName, Data.EnglishLocale));
    }
    
    private static void ModifyGuid(ref Guid? guid, Guid newValue)
    {
        guid = newValue;
    }
}