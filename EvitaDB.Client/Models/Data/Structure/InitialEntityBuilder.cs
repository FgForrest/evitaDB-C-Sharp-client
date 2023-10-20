using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Mutations.Entities;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using EvitaDB.Client.Models.Data.Mutations.Reference;
using EvitaDB.Client.Models.Data.Structure.Predicates;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Builder that is used to create the entity.
/// Due to performance reasons there is special implementation for the situation when entity is newly created.
/// In this case we know everything is new and we don't need to closely monitor the changes so this can speed things up.
/// </summary>
public class InitialEntityBuilder : IEntityBuilder
{
    public string Type { get; }
    public int? PrimaryKey { get; }
    public int Version => 1;
    public IEntitySchema Schema { get; }
    public int? Parent { get; private set; }
    private InitialEntityAttributesBuilder AttributesBuilder { get; }
    private IAssociatedDataBuilder AssociatedDataBuilder { get; }
    private IPricesBuilder PricesBuilder { get; }
    private IDictionary<ReferenceKey, IReference> References { get; }
    public PriceInnerRecordHandling? InnerRecordHandling => PricesBuilder.InnerRecordHandling;

    public IEnumerable<IPrice> GetPrices()
    {
        return PricesBuilder.GetPrices();
    }

    public bool PricesAvailable() => PricesBuilder.PricesAvailable();
    public IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment, params string[] priceListPriority)
    {
        return PricesBuilder.GetAllPricesForSale(currency, atTheMoment, priceListPriority);
    }

    public IList<IPrice> GetAllPricesForSale()
    {
        return PricesBuilder.GetAllPricesForSale();
    }

    public bool AssociatedDataAvailable => AssociatedDataBuilder.AssociatedDataAvailable();
    public bool AttributesAvailable() => AttributesBuilder.AttributesAvailable();
    public bool AttributesAvailable(CultureInfo locale)
    {
        return AttributesBuilder.AttributesAvailable(locale);
    }

    public bool ReferencesAvailable() => true;

    public bool ParentAvailable() => true;

    public bool Dropped => false;

    public IEnumerable<IReference> GetReferences(string referenceName)
    {
        return References.Values
            .Where(it => Equals(referenceName, it.ReferenceName))
            .ToList();
    }

    public IPrice PriceForSale => throw new ContextMissingException();
    
    public InitialEntityBuilder(
        IEntitySchema entitySchema,
        int? primaryKey,
        IEnumerable<AttributeValue> attributeValues,
        ICollection<AssociatedDataValue> associatedDataValues,
        ICollection<IReference> referenceContracts,
        PriceInnerRecordHandling? priceInnerRecordHandling,
        ICollection<IPrice> prices
    )
    {
        Type = entitySchema.Name;
        Schema = entitySchema;
        PrimaryKey = primaryKey;
        AttributesBuilder = new InitialEntityAttributesBuilder(entitySchema);
        foreach (AttributeValue attributeValue in attributeValues)
        {
            AttributeKey attributeKey = attributeValue.Key;
            if (attributeKey.Localized)
            {
                AttributesBuilder.SetAttribute(
                    attributeKey.AttributeName,
                    attributeKey.Locale!,
                    attributeValue.Value
                );
            }
            else
            {
                AttributesBuilder.SetAttribute(
                    attributeKey.AttributeName,
                    attributeValue.Value
                );
            }
        }

        AssociatedDataBuilder = new InitialAssociatedDataBuilder(Schema);
        foreach (AssociatedDataValue associatedDataValue in associatedDataValues)
        {
            AssociatedDataKey associatedDataKey = associatedDataValue.Key;
            if (associatedDataKey.Localized)
            {
                AssociatedDataBuilder.SetAssociatedData(
                    associatedDataKey.AssociatedDataName,
                    associatedDataKey.Locale!,
                    associatedDataValue.Value
                );
            }
            else
            {
                AssociatedDataBuilder.SetAssociatedData(
                    associatedDataKey.AssociatedDataName,
                    associatedDataValue.Value
                );
            }
        }

        PricesBuilder = new InitialPricesBuilder(Schema);
        if (priceInnerRecordHandling is not null)
        {
            PricesBuilder.SetPriceInnerRecordHandling(priceInnerRecordHandling.Value);
        }

        foreach (IPrice price in prices)
        {
            PricesBuilder.SetPrice(
                price.PriceId,
                price.PriceList,
                price.Currency,
                price.InnerRecordId,
                price.PriceWithoutTax,
                price.TaxRate,
                price.PriceWithTax,
                price.Validity,
                price.Sellable
            );
        }

        References = referenceContracts.ToDictionary(x => x.ReferenceKey, x => x);
    }

    public InitialEntityBuilder(IEntitySchema schema, int? primaryKey)
    {
        Type = schema.Name;
        Schema = schema;
        PrimaryKey = primaryKey;
        AttributesBuilder = new InitialEntityAttributesBuilder(schema);
        AssociatedDataBuilder = new InitialAssociatedDataBuilder(schema);
        PricesBuilder = new InitialPricesBuilder(schema);
        References = new Dictionary<ReferenceKey, IReference>();
    }

    public ISet<CultureInfo> GetAllLocales()
    {
        return AttributesBuilder.GetAttributeLocales().Concat(AssociatedDataBuilder.GetAssociatedDataLocales())
            .ToHashSet();
    }

    public IEnumerable<IReference> GetReferences()
    {
        return References.Values;
    }


    public IReference? GetReference(string referenceName, int referencedEntityId)
    {
        return References.TryGetValue(new ReferenceKey(referenceName, referencedEntityId), out var reference)
            ? reference
            : null;
    }

    public IReference? GetReference(ReferenceKey referenceKey)
    {
        return References.TryGetValue(referenceKey, out var reference)
            ? reference
            : null;
    }

    public IPrice? GetPrice(PriceKey priceKey)
    {
        return PricesBuilder.GetPrice(priceKey);
    }

    public IPrice? GetPrice(int priceId, string priceList, Currency currency)
    {
        return PricesBuilder.GetPrice(priceId, priceList, currency);
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        return PricesBuilder.HasPriceInInterval(from, to, queryPriceMode);
    }

    public IEntityClassifierWithParent? ParentEntity => null;
    
    public bool AttributeAvailable(string attributeName) => AttributesBuilder.AttributeAvailable(attributeName);

    public bool AttributeAvailable(string attributeName, CultureInfo locale) =>
        AttributesBuilder.AttributeAvailable(attributeName, locale);

    public object? GetAttribute(string attributeName)
    {
        return AttributesBuilder.GetAttribute(attributeName);
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttribute(attributeName, locale);
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        return AttributesBuilder.GetAttributeArray(attributeName);
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttributeArray(attributeName, locale);
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return AttributesBuilder.GetAttributeValue(attributeName);
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.GetAttributeValue(attributeName, locale);
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        return AttributesBuilder.GetAttributeValue(attributeKey);
    }

    public IEntityAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return AttributesBuilder.GetAttributeSchema(attributeName);
    }

    public ISet<string> GetAttributeNames()
    {
        return AttributesBuilder.GetAttributeNames();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        return AttributesBuilder.GetAttributeKeys();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        return AttributesBuilder.GetAttributeValues();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return AttributesBuilder.GetAttributeValues(attributeName);
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return AttributesBuilder.GetAttributeLocales();
    }


    bool IAssociatedData.AssociatedDataAvailable()
    {
        return AssociatedDataBuilder.AssociatedDataAvailable();
    }

    bool IAssociatedData.AssociatedDataAvailable(CultureInfo locale)
    {
        return AssociatedDataBuilder.AssociatedDataAvailable(locale);
    }

    bool IAssociatedData.AssociatedDataAvailable(string associatedDataName)
    {
        return AssociatedDataBuilder.AssociatedDataAvailable(associatedDataName);
    }

    bool IAssociatedData.AssociatedDataAvailable(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataBuilder.AssociatedDataAvailable(associatedDataName, locale);
    }

    public object? GetAssociatedData(string associatedDataName)
    {
        return AssociatedDataBuilder.GetAssociatedData(associatedDataName);
    }

    public T? GetAssociatedData<T>(string associatedDataName) where T : class
    {
        return AssociatedDataBuilder.GetAssociatedData<T>(associatedDataName);
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataBuilder.GetAssociatedData(associatedDataName, locale);
    }

    public T? GetAssociatedData<T>(string associatedDataName, CultureInfo locale) where T : class
    {
        return AssociatedDataBuilder.GetAssociatedData<T>(associatedDataName, locale);
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        return AssociatedDataBuilder.GetAssociatedDataArray(associatedDataName);
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataBuilder.GetAssociatedDataArray(associatedDataName, locale);
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        return AssociatedDataBuilder.GetAssociatedDataValue(associatedDataName);
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataBuilder.GetAssociatedDataValue(associatedDataName, locale);
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        return AssociatedDataBuilder.GetAssociatedDataValue(associatedDataKey);
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return AssociatedDataBuilder.GetAssociatedDataSchema(associatedDataName);
    }

    public ISet<string> GetAssociatedDataNames()
    {
        return AssociatedDataBuilder.GetAssociatedDataNames();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return AssociatedDataBuilder.GetAssociatedDataKeys();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return AssociatedDataBuilder.GetAssociatedDataValues();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return AssociatedDataBuilder.GetAssociatedDataValues(associatedDataName);
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        return AssociatedDataBuilder.GetAssociatedDataLocales();
    }

    public IEntityBuilder SetParent(int parentPrimaryKey)
    {
        Parent = parentPrimaryKey;
        return this;
    }

    public IEntityBuilder RemoveParent()
    {
        Parent = null;
        return this;
    }

    public IEntityBuilder SetReference(string referenceName, int referencedPrimaryKey)
    {
        IReferenceSchema referenceSchema = GetReferenceSchemaOrThrowException(referenceName);
        return SetReference(referenceName, referenceSchema.ReferencedEntityType, referenceSchema.Cardinality,
            referencedPrimaryKey, null);
    }

    public IEntityBuilder SetReference(string referenceName, int referencedPrimaryKey,
        Action<IReferenceBuilder>? whichIs)
    {
        IReferenceSchema referenceSchema = GetReferenceSchemaOrThrowException(referenceName);
        return SetReference(referenceName, referenceSchema.ReferencedEntityType, referenceSchema.Cardinality,
            referencedPrimaryKey, whichIs);
    }

    public IEntityBuilder SetReference(string referenceName, string referencedEntityType, Cardinality cardinality,
        int referencedPrimaryKey)
    {
        return SetReference(referenceName, referencedEntityType, cardinality, referencedPrimaryKey, null);
    }

    public IEntityBuilder SetReference(string referenceName, string referencedEntityType, Cardinality cardinality,
        int referencedPrimaryKey, Action<IReferenceBuilder>? whichIs)
    {
        InitialReferenceBuilder builder = new InitialReferenceBuilder(Schema, referenceName, referencedPrimaryKey, cardinality, referencedEntityType);
        whichIs?.Invoke(builder);
        IReference reference = builder.Build();
        References.Add(new ReferenceKey(referenceName, referencedPrimaryKey), reference);
        return this;
    }

    public IEntityBuilder RemoveReference(string referenceName, int referencedPrimaryKey)
    {
        References.Remove(new ReferenceKey(referenceName, referencedPrimaryKey));
        return this;
    }

    public IEntityMutation ToMutation()
    {
        List<ILocalMutation> localMutations = new List<ILocalMutation>();
        if (Parent != null)
        {
            localMutations.Add(new SetParentMutation(Parent.Value));
        }

        foreach (IReference reference in References.Values)
        {
            localMutations.Add(new InsertReferenceMutation(
                reference.ReferenceKey,
                reference.ReferenceCardinality,
                reference.ReferencedEntityType
            ));
            if (reference.Group is not null)
            {
                localMutations.Add(new SetReferenceGroupMutation(
                    reference.ReferenceKey,
                    reference.Group.Type,
                    reference.Group.PrimaryKey!.Value
                ));
            }

            foreach (var attributeValue in reference.GetAttributeValues())
            {
                localMutations.Add(new ReferenceAttributeMutation(
                    reference.ReferenceKey,
                    new UpsertAttributeMutation(attributeValue.Key, attributeValue.Value!)
                ));
            }
        }

        localMutations.AddRange(AttributesBuilder
            .GetAttributeValues()
            .Where(it => it.Value != null)
            .Select(it => new UpsertAttributeMutation(it.Key, it.Value!))
        );
        localMutations.AddRange(
            AssociatedDataBuilder
                .GetAssociatedDataValues()
                .Where(it => it.Value != null)
                .Select(it => new UpsertAssociatedDataMutation(it.Key, it.Value!))
        );

        localMutations.Add(new SetPriceInnerRecordHandlingMutation(PricesBuilder.InnerRecordHandling!.Value));
        localMutations.AddRange(PricesBuilder
            .GetPrices()
            .Select(it => new UpsertPriceMutation(it.Key, it))
        );

        return new EntityUpsertMutation(
            Type,
            PrimaryKey,
            EntityExistence.MustNotExist,
            localMutations
        );
    }


    public Entity ToInstance()
    {
        return Entity.InternalBuild(
            PrimaryKey,
            Version,
            Schema,
            Parent,
            ParentEntity,
            References.Values,
            AttributesBuilder.Build(),
            AssociatedDataBuilder.Build(),
            PricesBuilder.Build(),
            GetAllLocales(),
            LocalePredicate.DefaultInstance,
            HierarchyPredicate.DefaultInstance,
            AttributeValuePredicate.DefaultInstance,
            AssociatedDataValuePredicate.DefaultInstance,
            ReferencePredicate.DefaultInstance,
            PricePredicate.DefaultInstance
        );
    }

    public bool DiffersFrom(IEntity? otherObject)
    {
        return (this as IEntity).DiffersFrom(otherObject);
    }

    public IEntityBuilder RemoveAttribute(string attributeName)
    {
        AttributesBuilder.RemoveAttribute(attributeName);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IEntityBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        AttributesBuilder.RemoveAttribute(attributeName, locale);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IEntityBuilder MutateAttribute(AttributeMutation mutation)
    {
        AttributesBuilder.MutateAttribute(mutation);
        return this;
    }

    public IEntityBuilder RemoveAssociatedData(string associatedDataName)
    {
        AssociatedDataBuilder.RemoveAssociatedData(associatedDataName);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, object? associatedDataValue)
    {
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, associatedDataValue);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, object[]? associatedDataValue)
    {
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, associatedDataValue);
        return this;
    }

    public IEntityBuilder RemoveAssociatedData(string associatedDataName, CultureInfo locale)
    {
        AssociatedDataBuilder.RemoveAssociatedData(associatedDataName, locale);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, CultureInfo locale, object? associatedDataValue)
    {
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, locale, associatedDataValue);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, CultureInfo locale, object[]? associatedDataValue)
    {
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, locale, associatedDataValue);
        return this;
    }

    public IEntityBuilder MutateAssociatedData(AssociatedDataMutation mutation)
    {
        AssociatedDataBuilder.MutateAssociatedData(mutation);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, bool sellable)
    {
        PricesBuilder.SetPrice(priceId, priceList, currency, priceWithoutTax, taxRate, priceWithTax, sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax, decimal taxRate, decimal priceWithTax, bool sellable)
    {
        PricesBuilder.SetPrice(priceId, priceList, currency, innerRecordId, priceWithoutTax, taxRate, priceWithTax,
            sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        PricesBuilder.SetPrice(priceId, priceList, currency, priceWithoutTax, taxRate, priceWithTax, validity,
            sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        PricesBuilder.SetPrice(priceId, priceList, currency, innerRecordId, priceWithoutTax, taxRate, priceWithTax,
            validity, sellable);
        return this;
    }

    public IEntityBuilder RemovePrice(int priceId, string priceList, Currency currency)
    {
        PricesBuilder.RemovePrice(priceId, priceList, currency);
        return this;
    }

    public IEntityBuilder SetPriceInnerRecordHandling(PriceInnerRecordHandling priceInnerRecordHandling)
    {
        PricesBuilder.SetPriceInnerRecordHandling(priceInnerRecordHandling);
        return this;
    }

    public IEntityBuilder RemovePriceInnerRecordHandling()
    {
        PricesBuilder.RemovePriceInnerRecordHandling();
        return this;
    }

    public IEntityBuilder RemoveAllNonTouchedPrices()
    {
        PricesBuilder.RemoveAllNonTouchedPrices();
        return this;
    }

    private IReferenceSchema GetReferenceSchemaOrThrowException(string referenceName)
    {
        return Schema.GetReference(referenceName) ?? throw new ReferenceNotKnownException(referenceName);
    }
}