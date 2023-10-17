using EvitaDB.Client;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.TestX.Utils;
using FluentAssertions;
using Xunit.Abstractions;
using static EvitaDB.Client.Queries.IQueryConstraints;

namespace EvitaDB.TestX;

public class EvitaClientReadTest : BaseTest
{
    public EvitaClientReadTest(ITestOutputHelper outputHelper, SetupFixture setupFixture)
        : base(outputHelper, setupFixture)
    {
    }
    
    [Fact]
    public void ShouldQueryCatalog()
    {
        ICatalogSchema catalogSchema = _client!.QueryCatalog(
            Data.TestCatalog,
            x => x.GetCatalogSchema()
        );
        Assert.NotNull(catalogSchema);
        Assert.Equal(Data.TestCatalog, catalogSchema.Name);
    }

    [Fact]
    public void ShouldQueryOneEntityReference()
    {
        EntityReference entityReference = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryOneEntityReference(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(1)
                    )
                )
            ) ?? throw new Exception("Entity reference not found!"));
        Assert.NotNull(entityReference);
        Assert.Equal(Entities.Product, entityReference.Type);
        Assert.Equal(1, entityReference.PrimaryKey);
    }

    [Fact]
    public void ShouldNotQueryOneMissingEntity()
    {
        EntityReference? entityReference = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryOneEntityReference(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(-100)
                    )
                )
            ));
        Assert.Null(entityReference);
    }

    [Fact]
    public void ShouldQueryOneSealedEntity()
    {
        IList<ISealedEntity> products = _setupFixture.CreatedEntities[Entities.Product];
        int primaryKey = products.ElementAt(Random.Next(products.Count)).PrimaryKey!.Value;
        ISealedEntity sealedEntity = _client!.QueryCatalog(
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

        Assert.NotNull(sealedEntity);
        Assert.Equal(Entities.Product, sealedEntity.Type);
        products.Single(x => x.PrimaryKey == primaryKey).Should().BeEquivalentTo(sealedEntity, options => options.Excluding(x=>x.ParentEntity).Excluding(x=>x.Parent));
    }

    [Fact]
    public void ShouldQueryListOfEntityReferences()
    {
        int[] requestedIds = { 1, 2, 5 };
        IList<EntityReference> entityReferences = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryListOfEntityReferences(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(requestedIds)
                    )
                )
            ));

        Assert.NotNull(entityReferences);
        Assert.Equal(3, entityReferences.Count);

        for (int i = 0; i < entityReferences.Count; i++)
        {
            EntityReference entityReference = entityReferences.ElementAt(i);
            Assert.Equal(Entities.Product, entityReference.Type);
            Assert.Equal(requestedIds[i], entityReference.PrimaryKey);
        }
    }

    [Fact]
    public void ShouldQueryListOfSealedEntities()
    {
        int[] requestedIds = { 1, 2, 5 };
        IList<ISealedEntity> products = _setupFixture.CreatedEntities[Entities.Product];
        IList<ISealedEntity> sealedEntities = _client!.QueryCatalog(
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

        Assert.NotNull(sealedEntities);
        Assert.Equal(3, sealedEntities.Count);

        for (int i = 0; i < sealedEntities.Count; i++)
        {
            ISealedEntity sealedEntity = sealedEntities.ElementAt(i);
            Assert.Equal(Entities.Product, sealedEntity.Type);
            Assert.Equal(requestedIds[i], sealedEntity.PrimaryKey);
            Assert.Equal(products.Single(x => x.PrimaryKey == requestedIds[i]), sealedEntity);
        }
    }

    [Fact]
    public void ShouldGetListWithExtraResults()
    {
        ISealedEntity? someProductWithCategory = _setupFixture.CreatedEntities[Entities.Product]
            .Where(it => it.GetReferences(Data.ReferenceCategories).Any())
            .FirstOrDefault(it => it.GetAllPricesForSale().Count > 0);
        IList<IPrice> allPricesForSale = someProductWithCategory!.GetAllPricesForSale();
        EvitaResponse<ISealedEntity> result = _client!.QueryCatalog(
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

        Assert.NotNull(result);
        Assert.True(result.RecordPage.TotalRecordCount > 0);

        Assert.NotNull(result.GetExtraResult<Client.Models.ExtraResults.QueryTelemetry>());

        Client.Models.ExtraResults.PriceHistogram? priceHistogram = result.GetExtraResult<Client.Models.ExtraResults.PriceHistogram>();
        Assert.NotNull(priceHistogram);
        Assert.True(priceHistogram.Max.CompareTo(priceHistogram.Min) >= 0);
        Assert.True(priceHistogram.Buckets.Length > 0);
        Assert.True(priceHistogram.Min.CompareTo(0m) > 0);

        Client.Models.ExtraResults.AttributeHistogram? attributeHistogram =
            result.GetExtraResult<Client.Models.ExtraResults.AttributeHistogram>();
        Assert.NotNull(attributeHistogram);
        IHistogram? theHistogram = attributeHistogram.GetHistogram(Data.AttributeQuantity);
        Assert.NotNull(theHistogram);
        Assert.True(theHistogram.Max.CompareTo(theHistogram.Min) >= 0);
        Assert.True(theHistogram.Buckets.Length > 0);
        Assert.True(theHistogram.Min.CompareTo(0m) > 0);

        Hierarchy? hierarchy = result.GetExtraResult<Hierarchy>();
        Assert.NotNull(hierarchy);
        IDictionary<string, List<LevelInfo>>? categoryHierarchy =
            hierarchy.GetReferenceHierarchy(Data.ReferenceCategories);
        Assert.NotNull(categoryHierarchy);
        Assert.True(categoryHierarchy[Data.HierarchyReferenceRoot].Count >= 0);

        Client.Models.ExtraResults.FacetSummary? facetSummary =
            result.GetExtraResult<Client.Models.ExtraResults.FacetSummary>();
        Assert.NotNull(facetSummary);
        Assert.True(facetSummary.GetFacetGroupStatistics().Count > 0);
    }

    [Fact]
    public void ShouldGetSingleEntity()
    {
        ISealedEntity? sealedEntity = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        Assert.NotNull(sealedEntity);
        
        Assert.Equal(Entities.Product, sealedEntity.Type);
        Assert.Equal(7, sealedEntity.PrimaryKey);
        Assert.Equal(_setupFixture.CreatedEntities[Entities.Product].Single(x => x.PrimaryKey == 7), sealedEntity);
    }

    [Fact]
    public void ShouldEnrichSingleEntity()
    {
        IList<ISealedEntity> products = _setupFixture.CreatedEntities[Entities.Product];
        ISealedEntity? sealedEntity = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                AttributeContent()
            ));

        Assert.Equal(Entities.Product, sealedEntity!.Type);
        Assert.Equal(7, sealedEntity.PrimaryKey);
        products.Single(x => x.PrimaryKey == 7).Should().NotBeEquivalentTo(sealedEntity, options => options.Excluding(x=>x.ParentEntity).Excluding(x=>x.Parent));

        ISealedEntity? enrichedEntity = _client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        Assert.Equal(Entities.Product, enrichedEntity!.Type);
        Assert.Equal(7, enrichedEntity.PrimaryKey);

        ISealedEntity entityToCompare = products.Single(x => x.PrimaryKey == 7);

        entityToCompare.Should().BeEquivalentTo(entityToCompare);
    }

    [Fact]
    public void ShouldLimitSingleEntity()
    {
        IList<ISealedEntity> products = _setupFixture.CreatedEntities[Entities.Product];
        ISealedEntity? sealedEntity = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                EntityFetchAll().Requirements!
            ));

        Assert.Equal(Entities.Product, sealedEntity!.Type);
        Assert.Equal(7, sealedEntity.PrimaryKey);
        products.Single(x => x.PrimaryKey == 7).Should().BeEquivalentTo(sealedEntity, options => options.Excluding(x=>x.ParentEntity).Excluding(x=>x.Parent));

        ISealedEntity? limitedEntity = _client.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntity(
                Entities.Product,
                7,
                AttributeContent()
            ));

        Assert.Equal(Entities.Product, limitedEntity!.Type);
        Assert.Equal(7, limitedEntity.PrimaryKey);
        products.Single(x => x.PrimaryKey == 7).Should().NotBeEquivalentTo(limitedEntity, options => options.Excluding(x=>x.ParentEntity).Excluding(x=>x.Parent));
    }

    [Fact]
    public void ShouldRetrieveCollectionSize()
    {
        int productCount = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.GetEntityCollectionSize(Entities.Product));

        Assert.Equal(_setupFixture.CreatedEntities[Entities.Product].Count, productCount);
    }

    [Fact]
    public void ShouldQueryListOfSealedEntitiesEvenWithoutProperRequirements()
    {
        IList<ISealedEntity> sealedEntities = _client!.QueryCatalog(
            Data.TestCatalog,
            session => session.QueryListOfSealedEntities(
                Query(
                    Collection(Entities.Product),
                    FilterBy(
                        EntityPrimaryKeyInSet(1, 2, 5)
                    )
                )
            ));
        Assert.Equal(3, sealedEntities.Count);
    }
    
    [Fact]
    public void ShouldListCatalogNames()
    {
        ISet<string> catalogNames = ListCatalogNames(_client!);
        _outputHelper.WriteLine(string.Join(", ", catalogNames));
        Assert.Equal(1, catalogNames.Count);
        Assert.Contains(Data.TestCatalog, catalogNames);
    }
    
    [Fact]
    public void ShouldBeAbleToRunParallelClients()
    {
        EvitaClient anotherParallelClient = new EvitaClient(_client!.Configuration);
        _ = ListCatalogNames(anotherParallelClient);
        _ = ListCatalogNames(_client);
    }
    
    private static ISet<string> ListCatalogNames(EvitaClient client)
    {
        return client.GetCatalogNames();
    }
    
    /*[Fact]
    public void ShouldTestCdc()
    {
        IObservable<ChangeSystemCapture> captures =
            _client!.RegisterSystemChangeCapture(new ChangeSystemCaptureRequest(CaptureContent.Header));
        IDisposable subscription = captures.Subscribe(c => { _outputHelper.WriteLine(c.Operation.ToString()); });
        subscription.Dispose();
    }*/
}