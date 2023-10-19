using EvitaDB.Client;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Session;
using EvitaDB.Client.Utils;
using static EvitaDB.Client.Queries.IQueryConstraints;
using Assert = Xunit.Assert;

namespace EvitaDB.Test.Utils;

public static class DataManipulationUtil
{
    private static IEntitySchema CreateProductSchema(EvitaClientSession session, string entityType)
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
        Assert.NotNull(nameAttributeSchema);
        Assert.Equal(typeof(string), nameAttributeSchema.Type);
        Assert.True(nameAttributeSchema.Filterable);
        Assert.True(nameAttributeSchema.Sortable);
        Assert.False(nameAttributeSchema.Nullable);
        Assert.True(nameAttributeSchema.Localized);
        Assert.False(nameAttributeSchema.Unique);
        Assert.NotNull(nameAttributeSchema.Description);

        IAttributeSchema? validityAttributeSchema = schema.GetAttribute(Data.AttributeValidity);
        Assert.NotNull(validityAttributeSchema);
        Assert.Equal(typeof(DateTimeOffset[]), validityAttributeSchema.Type);
        Assert.False(validityAttributeSchema.Filterable);
        Assert.False(validityAttributeSchema.Sortable);
        Assert.True(validityAttributeSchema.Nullable);
        Assert.False(validityAttributeSchema.Localized);
        Assert.False(validityAttributeSchema.Unique);
        Assert.Null(validityAttributeSchema.Description);
        
        IAttributeSchema? quantityAttributeSchema = schema.GetAttribute(Data.AttributeQuantity);
        Assert.NotNull(quantityAttributeSchema);
        Assert.Equal(typeof(decimal), quantityAttributeSchema.Type);
        Assert.True(quantityAttributeSchema.Filterable);
        Assert.False(quantityAttributeSchema.Sortable);
        Assert.True(quantityAttributeSchema.Nullable);
        Assert.False(quantityAttributeSchema.Localized);
        Assert.False(quantityAttributeSchema.Unique);
        Assert.Null(quantityAttributeSchema.Description);

        IAttributeSchema? aliasAttributeSchema = schema.GetAttribute(Data.AttributeAlias);
        Assert.Null(aliasAttributeSchema);

        IAssociatedDataSchema? associatedDataSchema = schema.GetAssociatedData(Data.AssociatedDataReferencedFiles);
        Assert.NotNull(associatedDataSchema);
        Assert.Equal(typeof(ComplexDataObject), associatedDataSchema.Type);
        Assert.True(associatedDataSchema.Localized);
        Assert.True(associatedDataSchema.Nullable);

        IAssociatedDataSchema? nonExistingAssociatedDataSchema = schema.GetAssociatedData(Data.AssociatedDataLabels);
        Assert.Null(nonExistingAssociatedDataSchema);

        IReferenceSchema? relatedProductsReferenceSchema = schema.GetReference(Data.ReferenceRelatedProducts);
        Assert.NotNull(relatedProductsReferenceSchema);
        Assert.Equal(Cardinality.ZeroOrMore, relatedProductsReferenceSchema.Cardinality);
        Assert.True(relatedProductsReferenceSchema.IsIndexed);
        Assert.Equal(entityType, relatedProductsReferenceSchema.ReferencedEntityType);
        Assert.Equal(Data.ReferenceGroupType, relatedProductsReferenceSchema.ReferencedGroupType);
        
        IReferenceSchema? categoriesReferenceSchema = schema.GetReference(Data.ReferenceCategories);
        Assert.NotNull(categoriesReferenceSchema);
        Assert.Equal(Cardinality.ZeroOrMore, categoriesReferenceSchema.Cardinality);
        Assert.True(categoriesReferenceSchema.IsIndexed);
        Assert.Equal(Entities.Category, categoriesReferenceSchema.ReferencedEntityType);
        Assert.Equal(Data.ReferenceGroupType, categoriesReferenceSchema.ReferencedGroupType);

        IAttributeSchema? referenceAttributeSchema = relatedProductsReferenceSchema.GetAttribute(Data.AttributePriority);
        Assert.NotNull(referenceAttributeSchema);
        Assert.False(referenceAttributeSchema.Filterable);
        Assert.True(referenceAttributeSchema.Sortable);
        
        Assert.Null(categoriesReferenceSchema.GetAttribute(Data.AttributeQuantity));
        
        Assert.True(schema.WithPrice);
        Assert.False(schema.WithHierarchy);
        Assert.True(schema.WithGeneratedPrimaryKey);

        return schema;
    }

    private static IList<ISealedEntity> CreateProductsThatMatchSchema(EvitaClientSession session, string entityType, int count)
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

            AtomicReference<EntityReference> entityReference = new AtomicReference<EntityReference>
            {
                Value = builder.UpsertVia(session)
            };

            ISealedEntity? entity = session.GetEntity(entityReference.Value.Type,
                entityReference.Value.PrimaryKey!.Value, EntityFetchAllContent());

            Assert.NotNull(entity);
            Assert.Equal(enAttributeName, entity.GetAttribute(Data.AttributeName, Data.EnglishLocale));
            Assert.Equal(csAttributeName, entity.GetAttribute(Data.AttributeName, Data.CzechLocale));
            Assert.Equal(attributeCode, entity.GetAttribute(Data.AttributeCode));
            Assert.Equal(quantity, entity.GetAttribute(Data.AttributeQuantity));
            Assert.Equal(attributeValidity.Select(x=>x as object), entity.GetAttributeArray(Data.AttributeValidity)!);
            Assert.Equal(typeof(ComplexDataObject), entity.GetAssociatedData(Data.AssociatedDataReferencedFiles, Data.EnglishLocale)?.GetType());
            Assert.Equal(typeof(ComplexDataObject), entity.GetAssociatedData(Data.AssociatedDataReferencedFiles, Data.CzechLocale)?.GetType());
            Assert.Equal(asData, entity.GetAssociatedData<TestAsDataObj[]>(Data.AssociatedDataReferencedFiles, Data.EnglishLocale)!);
            Assert.Equal(asData, entity.GetAssociatedData<TestAsDataObj[]>(Data.AssociatedDataReferencedFiles, Data.CzechLocale)!);
            Assert.Equal(price, entity.GetPrice(price.Key));
            Assert.Equal(priceInnerRecordHandling, entity.InnerRecordHandling);
            Assert.Equal(reference, entity.GetReference(reference.ReferenceName, reference.ReferencedPrimaryKey));
            Assert.Equal(categoriesReference, entity.GetReference(categoriesReference.ReferenceName, categoriesReference.ReferencedPrimaryKey));
            
            entities.Add(entity);
        }

        return entities;
    }

    public static void CreateProductThatViolatesSchema(EvitaClient client, string entityType)
    {
        using EvitaClientSession rwSession = client.CreateReadWriteSession(Data.TestCatalog);
        IEntityBuilder builder = rwSession.CreateNewEntity(entityType);
        builder.SetAttribute(Data.AttributeName, Data.EnglishLocale, 5);
        builder.UpsertVia(rwSession);
    }

    public static ISealedEntity? CreateSomeNewCategory(EvitaClientSession session, int primaryKey, int? parentPrimaryKey)
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

    public static IEntityMutation CreateSomeNewProduct(EvitaClientSession session)
    {
        return session.CreateNewEntity(Entities.Product)
            .SetAttribute(Data.AttributeName, Data.EnglishLocale, "New product")
            .SetAttribute(Data.AttributeCode, "product-" + (session.GetEntityCollectionSize(Entities.Product) + 1))
            .ToMutation() ?? throw new EvitaInternalError("Cannot create product mutation");
    }

    public static IDictionary<string, IList<ISealedEntity>> DeleteCreateAndSetupCatalog(EvitaClient client, string catalogName)
    {
        IDictionary<string, IList<ISealedEntity>> createdEntities = new Dictionary<string, IList<ISealedEntity>>();
        _ = client.DeleteCatalogIfExists(catalogName);
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
            createdEntities.Add(Entities.Category, new List<ISealedEntity> { category1, category2 });
        }
        
        if (!allEntityTypes.Contains(Entities.Product) || session.GetEntityCollectionSize(Entities.Product) == 0)
        {
            _ = CreateProductSchema(session, Entities.Product);
            IList<ISealedEntity> products = CreateProductsThatMatchSchema(session, Entities.Product, 10);
            createdEntities.Add(Entities.Product, products);
        }

        return createdEntities;
    }
}