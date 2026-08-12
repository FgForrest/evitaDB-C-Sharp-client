using EvitaDB.Client;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Test.Utils;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Mirrors the most valuable scenarios of <see cref="EvitaClientReadTest"/> and <see cref="EvitaClientWriteTest"/>
/// through the async API surface (async query trio, entity CRUD, archiving, catalog access and the commit
/// progress of a transactional session close).
/// </summary>
public class EvitaClientAsyncTest : BaseTest<SetupFixture>
{
    public EvitaClientAsyncTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
        : base(outputHelper, setupFixture)
    {
    }

    [Fact]
    public async Task ShouldListCatalogNamesAsync()
    {
        ISet<string> catalogNames =
            await Client!.GetCatalogNamesAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(Data.TestCatalog, catalogNames);
    }

    [Fact]
    public async Task ShouldQueryOneEntityReferenceAsync()
    {
        EntityReference? entityReference = await Client!.QueryCatalogAsync(Data.TestCatalog, session =>
            session.QueryOneEntityReferenceAsync(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(1)
                    )
                )
            ), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(entityReference);
        Assert.Equal(Entities.Product, entityReference.Type);
        Assert.Equal(1, entityReference.PrimaryKey);
    }

    [Fact]
    public async Task ShouldQueryOneSealedEntityAsync()
    {
        IList<ISealedEntity> products = SetupFixture.CreatedEntities[Entities.Product];
        int primaryKey = products.ElementAt(Random.Next(products.Count)).PrimaryKey!.Value;
        ISealedEntity? sealedEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.QueryOneSealedEntityAsync(
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
                            ReferenceContentAllWithAttributes(),
                            DataInLocalesAll()
                        )
                    )
                )
            ),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(sealedEntity);
        Assert.Equal(Entities.Product, sealedEntity.Type);
        Assert.False(products.Single(x => x.PrimaryKey == primaryKey).DiffersFrom(sealedEntity));
    }

    [Fact]
    public async Task ShouldQueryListOfSealedEntitiesAsync()
    {
        int[] requestedIds = { 1, 2, 5 };
        IList<ISealedEntity> products = SetupFixture.CreatedEntities[Entities.Product];
        IList<ISealedEntity> sealedEntities = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.QueryListOfSealedEntitiesAsync(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(requestedIds)
                    ),
                    Require(
                        EntityFetchAll()
                    )
                )
            ), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(sealedEntities);
        Assert.Equal(3, sealedEntities.Count);

        for (int i = 0; i < sealedEntities.Count; i++)
        {
            ISealedEntity sealedEntity = sealedEntities.ElementAt(i);
            Assert.Equal(Entities.Product, sealedEntity.Type);
            Assert.Equal(requestedIds[i], sealedEntity.PrimaryKey);
            Assert.False(products.Single(x => x.PrimaryKey == requestedIds[i]).DiffersFrom(sealedEntity));
        }
    }

    [Fact]
    public async Task ShouldQuerySealedEntityResponseWithPagingAsync()
    {
        EvitaResponse<ISealedEntity> response = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.QuerySealedEntityAsync(
                Query(
                    Collection(Entities.Product),
                    Require(
                        Page(1, 5),
                        EntityFetchAll()
                    )
                )
            ), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(5, response.RecordData.Count);
        Assert.Equal(10, response.RecordPage.TotalRecordCount);
        Assert.All(response.RecordData, entity => Assert.Equal(Entities.Product, entity.Type));
    }

    [Fact]
    public async Task ShouldGetSingleEntityAsync()
    {
        ISealedEntity? sealedEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(sealedEntity);
        Assert.Equal(Entities.Product, sealedEntity.Type);
        Assert.Equal(7, sealedEntity.PrimaryKey);
        Assert.False(
            SetupFixture.CreatedEntities[Entities.Product].Single(x => x.PrimaryKey == 7).DiffersFrom(sealedEntity));
    }

    [Fact]
    public async Task ShouldRetrieveCollectionSizeAndEntityTypesAsync()
    {
        (int size, ISet<string> entityTypes) = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            async session => (
                await session.GetEntityCollectionSizeAsync(Entities.Product),
                await session.GetAllEntityTypesAsync()
            ), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(10, size);
        Assert.Contains(Entities.Product, entityTypes);
    }

    [Fact]
    public async Task ShouldUpsertFetchAndDeleteNewEntityAsync()
    {
        int newProductId = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            async session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);

                EntityReference newProduct = await session.UpsertEntityAsync(entityMutation);
                return newProduct.PrimaryKey!.Value;
            }, cancellationToken: TestContext.Current.CancellationToken);

        ISealedEntity? loadedEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(Entities.Product, newProductId, EntityFetchAllContent()),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(loadedEntity);
        Assert.Equal(newProductId, loadedEntity.PrimaryKey);
        Assert.Equal("New product", loadedEntity.GetAttribute(Data.AttributeName, Data.EnglishLocale));

        bool deleted = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            session => session.DeleteEntityAsync(Entities.Product, newProductId),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(deleted);

        ISealedEntity? deletedEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(Entities.Product, newProductId, EntityFetchAllContent()),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(deletedEntity);
    }

    [Fact]
    public async Task ShouldArchiveAndRestoreEntityAsync()
    {
        int newProductId = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            async session =>
            {
                IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);
                EntityReference newProduct = await session.UpsertEntityAsync(entityMutation);
                return newProduct.PrimaryKey!.Value;
            }, cancellationToken: TestContext.Current.CancellationToken);

        bool archived = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            session => session.ArchiveEntityAsync(Entities.Product, newProductId),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(archived);

        // the archived entity is no longer part of the live scope
        ISealedEntity? liveEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(Entities.Product, newProductId, EntityFetchAllContent()),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(liveEntity);

        bool restored = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            session => session.RestoreEntityAsync(Entities.Product, newProductId),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(restored);

        ISealedEntity? restoredEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(Entities.Product, newProductId, EntityFetchAllContent()),
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(restoredEntity);

        // reset data
        _ = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            session => session.DeleteEntityAsync(Entities.Product, newProductId), cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldReportCommitProgressPhasesOnTransactionalCloseAsync()
    {
        EvitaClientSession session = Client!.CreateReadWriteSession(Data.TestCatalog);
        IEntityMutation entityMutation = DataManipulationUtil.CreateSomeNewProduct(session);
        EntityReference newProduct = await session.UpsertEntityAsync(entityMutation, cancellationToken: TestContext.Current.CancellationToken);
        int newProductId = newProduct.PrimaryKey!.Value;

        CommitProgress commitProgress = session.CloseNowWithProgress();
        CommitVersions conflictsResolved = await commitProgress.OnConflictResolved;
        CommitVersions walAppended = await commitProgress.OnWalAppended;
        CommitVersions changesVisible = await commitProgress.OnChangesVisible;

        Assert.True(commitProgress.IsCompletedSuccessfully);
        Assert.Equal(conflictsResolved.CatalogVersion, walAppended.CatalogVersion);
        Assert.Equal(walAppended.CatalogVersion, changesVisible.CatalogVersion);
        Assert.False(session.Active);

        // the committed changes must be visible to other sessions once the last phase completes
        ISealedEntity? loadedEntity = await Client!.QueryCatalogAsync(
            Data.TestCatalog,
            s => s.GetEntityAsync(Entities.Product, newProductId, EntityFetchAllContent()), cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(loadedEntity);

        // reset data
        _ = await Client!.UpdateCatalogAsync(
            Data.TestCatalog,
            s => s.DeleteEntityAsync(Entities.Product, newProductId), cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ShouldNotExecuteCallWithCancelledTokenAsync()
    {

        TestContext.Current.CancelCurrentTest();

        Exception? exception = await Record.ExceptionAsync(() => Client!.QueryCatalogAsync(
            Data.TestCatalog,
            session => session.GetEntityAsync(
                Entities.Product, 7, EntityFetchAll().Requirements!, cancellationToken: TestContext.Current.CancellationToken), cancellationToken: TestContext.Current.CancellationToken));

        Assert.NotNull(exception);
    }
}
