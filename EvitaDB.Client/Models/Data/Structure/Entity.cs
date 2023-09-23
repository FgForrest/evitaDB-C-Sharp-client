using System.Collections.Immutable;
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
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public class Entity : ISealedEntity
{
    public int Version { get; }
    public string Type { get; }
    public int? PrimaryKey { get; }
    [JsonIgnore] public IEntitySchema Schema { get; }
    public IDictionary<ReferenceKey, IReference> References { get; }
    public Attributes Attributes { get; }
    public AssociatedData AssociatedData { get; }
    public Prices Prices { get; }
    public ISet<CultureInfo> Locales { get; }
    public bool Dropped { get; }
    public PriceInnerRecordHandling? InnerRecordHandling => Prices.InnerRecordHandling;
    
    /// <summary>
    /// This predicate filters out non-fetched locales.
    /// </summary>
    public LocalePredicate LocalePredicate { get; }
    
    /// <summary>
    /// This predicate filters out access to the hierarchy parent that were not fetched in query.
    /// </summary>
    public HierarchyPredicate HierarchyPredicate { get; }
    
    /// <summary>
    /// This predicate filters out attributes that were not fetched in query.
    /// </summary>
    public AttributeValuePredicate AttributePredicate { get; }
    
    /// <summary>
    /// This predicate filters out associated data that were not fetched in query.
    /// </summary>
    public AssociatedDataValuePredicate AssociatedDataPredicate { get; }
    
    /// <summary>
    /// This predicate filters out references that were not fetched in query.
    /// </summary>
    public ReferencePredicate ReferencePredicate { get; }
    
    /// <summary>
    /// This predicate filters out prices that were not fetched in query.
    /// </summary>
    public PricePredicate PricePredicate { get; }
    
    public bool ParentAvailable() => Schema.WithHierarchy;
    public bool PricesAvailable() => Prices.PricesAvailable();
    public bool AttributesAvailable() => Attributes.AttributesAvailable();

    public bool AttributesAvailable(CultureInfo locale)
    {
        return Attributes.AttributesAvailable(locale);
    }

    public bool AttributeAvailable(string attributeName)
    {
        return Attributes.AttributeAvailable(attributeName);
    }

    public bool AttributeAvailable(string attributeName, CultureInfo locale)
    {
        return Attributes.AttributeAvailable(attributeName, locale);
    }

    public bool ReferencesAvailable() => true;
    public bool AssociatedDataAvailable() => AssociatedData.AssociatedDataAvailable();

    public bool AssociatedDataAvailable(CultureInfo locale)
    {
        return AssociatedData.AssociatedDataAvailable(locale);
    }

    public bool AssociatedDataAvailable(string associatedDataName)
    {
        return AssociatedData.AssociatedDataAvailable(associatedDataName);
    }

    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale)
    {
        return AssociatedData.AssociatedDataAvailable(associatedDataName, locale);
    }

    private bool WithHierarchy { get; }
    private ISet<string> ReferencesDefined { get; set; }
    public IPrice? PriceForSale { get; }

    public int? Parent
    {
        get
        {
            Assert.IsTrue(WithHierarchy, () => new EntityIsNotHierarchicalException(Schema.Name));
            return _parent;
        }
    }

    private readonly int? _parent;

    public IEntityClassifierWithParent? ParentEntity
    {
        get
        {
            Assert.IsTrue(WithHierarchy, () => new EntityIsNotHierarchicalException(Schema.Name));
            return _parentEntity;
        }
    }

    private readonly IEntityClassifierWithParent? _parentEntity;

    internal static ISealedEntity InternalBuild(
        int? primaryKey,
        int? version,
        IEntitySchema entitySchema,
        int? parent,
        IEntityClassifierWithParent? parentEntity,
        IEnumerable<Reference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        ISet<CultureInfo> locales,
        EvitaRequest evitaRequest,
        bool dropped = false,
        IPrice? pricesForSale = null)
    {
        return new Entity(
            version ?? 1,
            entitySchema,
            primaryKey,
            parent,
            parentEntity,
            references,
            attributes,
            associatedData,
            prices,
            locales,
            evitaRequest,
            entitySchema.References.Keys.ToHashSet(),
            entitySchema.WithHierarchy,
            dropped,
            pricesForSale
        );
    }

    internal static Entity InternalBuild(
        Entity entity,
        int? primaryKey,
        int? version,
        IEntitySchema entitySchema,
        int? parent,
        IEntityClassifierWithParent? parentEntity,
        IEnumerable<IReference>? references,
        Attributes? attributes,
        AssociatedData? associatedData,
        Prices? prices,
        ISet<CultureInfo>? locales,
        bool dropped = false,
        IPrice? priceForSale = null)
    {
        return new Entity(
            version ?? 1,
            entitySchema,
            primaryKey,
            parent,
            parentEntity,
            references ?? entity.References.Values,
            attributes ?? entity.Attributes,
            associatedData ?? entity.AssociatedData,
            prices ?? entity.Prices,
            locales ?? entity.Locales.ToImmutableHashSet(),
            null,
            new HashSet<string>(),
            entitySchema.WithHierarchy,
            dropped,
            priceForSale
        );
    }

    internal static Entity InternalBuild(
        int? primaryKey,
        int? version,
        IEntitySchema entitySchema,
        int? parent,
        IEntityClassifierWithParent? parentEntity,
        ICollection<IReference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        ISet<CultureInfo> locales,
        LocalePredicate localePredicate,
        HierarchyPredicate hierarchyPredicate,
        AttributeValuePredicate attributePredicate,
        AssociatedDataValuePredicate associatedDataPredicate,
        ReferencePredicate referencePredicate,
        PricePredicate pricePredicate,
        bool dropped = false,
        IPrice? priceForSale = null
    )
    {
        return new Entity(
            version ?? 1,
            entitySchema,
            primaryKey,
            parent,
            parentEntity,
            references,
            attributes,
            associatedData,
            prices,
            locales,
            dropped,
            priceForSale,
            localePredicate,
            hierarchyPredicate,
            attributePredicate,
            associatedDataPredicate,
            referencePredicate,
            pricePredicate
        );
    }

    private Entity(
        int version,
        IEntitySchema schema,
        int? primaryKey,
        int? parent,
        IEntityClassifierWithParent? parentEntity,
        IEnumerable<IReference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        IEnumerable<CultureInfo> locales,
        bool dropped,
        IPrice? priceForSale,
        LocalePredicate localePredicate,
        HierarchyPredicate hierarchyPredicate,
        AttributeValuePredicate attributePredicate,
        AssociatedDataValuePredicate associatedDataPredicate,
        ReferencePredicate referencePredicate,
        PricePredicate pricePredicate
    )
    {
        Version = version;
        Type = schema.Name;
        Schema = schema;
        PrimaryKey = primaryKey;
        _parent = parent;
        _parentEntity = parentEntity;
        References = references.ToImmutableSortedDictionary(x => x.ReferenceKey, x => x);
        Attributes = attributes;
        AssociatedData = associatedData;
        Prices = prices;
        Locales = new HashSet<CultureInfo>(locales).ToImmutableHashSet();
        Dropped = dropped;
        WithHierarchy = Schema.WithHierarchy;
        ReferencesDefined = Schema.References.Keys.ToHashSet();
        PriceForSale = priceForSale;
        LocalePredicate = localePredicate;
        HierarchyPredicate = hierarchyPredicate;
        AttributePredicate = attributePredicate;
        AssociatedDataPredicate = associatedDataPredicate;
        ReferencePredicate = referencePredicate;
        PricePredicate = pricePredicate;
    }

    private Entity(
        int version,
        IEntitySchema schema,
        int? primaryKey,
        int? parent,
        IEntityClassifierWithParent? parentEntity,
        IEnumerable<IReference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        ISet<CultureInfo> locales,
        EvitaRequest? evitaRequest,
        ISet<string> referencesDefined,
        bool withHierarchy,
        bool dropped = false,
        IPrice? priceForSale = null
    )
    {
        Version = version;
        Type = schema.Name;
        Schema = schema;
        PrimaryKey = primaryKey;
        _parent = parent;
        _parentEntity = parentEntity;
        References = references.ToImmutableSortedDictionary(x => x.ReferenceKey, x => x);
        Attributes = attributes;
        AssociatedData = associatedData;
        Prices = prices;
        Locales = new HashSet<CultureInfo>(locales).ToImmutableHashSet();
        Dropped = dropped;
        ReferencesDefined = referencesDefined;
        WithHierarchy = withHierarchy;
        PriceForSale = priceForSale;
        if (evitaRequest is null)
        {
            LocalePredicate = LocalePredicate.DefaultInstance;
            HierarchyPredicate = HierarchyPredicate.DefaultInstance;
            AttributePredicate = AttributeValuePredicate.DefaultInstance;
            AssociatedDataPredicate = AssociatedDataValuePredicate.DefaultInstance;
            ReferencePredicate = ReferencePredicate.DefaultInstance;
            PricePredicate = PricePredicate.DefaultInstance;
        }
        else
        {
            LocalePredicate = new LocalePredicate(evitaRequest);
            HierarchyPredicate = new HierarchyPredicate(evitaRequest);
            AttributePredicate = new AttributeValuePredicate(evitaRequest);
            AssociatedDataPredicate = new AssociatedDataValuePredicate(evitaRequest);
            ReferencePredicate = new ReferencePredicate(evitaRequest);
            PricePredicate = new PricePredicate(evitaRequest, null);
        }
    }

    public Entity(string type, int? primaryKey)
    {
        Version = 1;
        Type = type;
        Schema = EntitySchema.InternalBuild(type);
        PrimaryKey = primaryKey;
        _parent = null;
        _parentEntity = null;
        References = new Dictionary<ReferenceKey, IReference>();
        Attributes = new Attributes(Schema);
        AssociatedData = new AssociatedData(Schema);
        Prices = new Prices(Schema, 1, new HashSet<IPrice>(), PriceInnerRecordHandling.None);
        Locales = new HashSet<CultureInfo>().ToImmutableHashSet();
        WithHierarchy = Schema.WithHierarchy;
        ReferencesDefined = new HashSet<string>();
        LocalePredicate = LocalePredicate.DefaultInstance;
        HierarchyPredicate = HierarchyPredicate.DefaultInstance;
        AttributePredicate = AttributeValuePredicate.DefaultInstance;
        AssociatedDataPredicate = AssociatedDataValuePredicate.DefaultInstance;
        ReferencePredicate = ReferencePredicate.DefaultInstance;
        PricePredicate = PricePredicate.DefaultInstance;
    }

    public IEntityBuilder OpenForWrite()
    {
        return new ExistingEntityBuilder(this);
    }

    public IEntityBuilder WithMutations(params ILocalMutation[] localMutations)
    {
        return new ExistingEntityBuilder(
            this,
            localMutations.ToList()
        );
    }

    public IEntityBuilder WithMutations(ICollection<ILocalMutation> localMutations)
    {
        return new ExistingEntityBuilder(
            this,
            localMutations
        );
    }

    public void CheckReferenceName(string referenceName)
    {
        Assert.IsTrue(ReferencesDefined.Contains(referenceName),
            () => new ReferenceNotFoundException(referenceName, Schema));
    }

    public static Entity MutateEntity(
        IEntitySchema entitySchema,
        Entity? entity,
        ICollection<ILocalMutation> localMutations
    )
    {
        int? oldParent = entity?.Parent;
        int? newParent = oldParent;
        PriceInnerRecordHandling? newPriceInnerRecordHandling = null;
        Dictionary<AttributeKey, AttributeValue> newAttributes =
            new Dictionary<AttributeKey, AttributeValue>(localMutations.Count);
        Dictionary<AssociatedDataKey, AssociatedDataValue> newAssociatedData =
            new Dictionary<AssociatedDataKey, AssociatedDataValue>(localMutations.Count);
        Dictionary<ReferenceKey, IReference> newReferences =
            new Dictionary<ReferenceKey, IReference>(localMutations.Count);
        Dictionary<PriceKey, IPrice> newPrices = new Dictionary<PriceKey, IPrice>(localMutations.Count);

        foreach (ILocalMutation localMutation in localMutations)
        {
            if (localMutation is ParentMutation parentMutation)
            {
                newParent = MutateHierarchyPlacement(entitySchema, entity, parentMutation);
            }
            else if (localMutation is AttributeMutation attributeMutation)
            {
                MutateAttributes(entitySchema, entity, newAttributes, attributeMutation);
            }
            else if (localMutation is AssociatedDataMutation associatedDataMutation)
            {
                MutateAssociatedData(entitySchema, entity, newAssociatedData, associatedDataMutation);
            }
            else if (localMutation is ReferenceMutation referenceMutation)
            {
                MutateReferences(entitySchema, entity, newReferences, referenceMutation);
            }
            else if (localMutation is PriceMutation priceMutation)
            {
                MutatePrices(entitySchema, entity, newPrices, priceMutation);
            }
            else if (localMutation is SetPriceInnerRecordHandlingMutation innerRecordHandlingMutation)
            {
                newPriceInnerRecordHandling =
                    MutateInnerPriceRecordHandling(entitySchema, entity, innerRecordHandlingMutation);
            }
        }

        // create or reuse existing attribute container
        Attributes newAttributeContainer = RecreateAttributeContainer(entitySchema, entity, newAttributes);

        // create or reuse existing associated data container
        AssociatedData newAssociatedDataContainer =
            RecreateAssociatedDataContainer(entitySchema, entity, newAssociatedData);

        // create or reuse existing reference container
        ReferenceTuple mergedReferences = RecreateReferences(entity, newReferences);

        // create or reuse existing prices
        Prices priceContainer = RecreatePrices(entitySchema, entity, newPriceInnerRecordHandling, newPrices);

        // aggregate entity locales
        ISet<CultureInfo> entityLocales = new HashSet<CultureInfo>(newAttributeContainer.GetAttributeLocales());
        foreach (CultureInfo associatedDataLocale in newAssociatedDataContainer.GetAssociatedDataLocales())
        {
            entityLocales.Add(associatedDataLocale);
        }

        if (newParent != oldParent || newPriceInnerRecordHandling != null ||
            newAttributes.Any() || newAssociatedData.Any() || newPrices.Any() ||
            newReferences.Any())
        {
            return new Entity(
                entity is null ? 1 : entity.Version + 1,
                entitySchema,
                entity?.PrimaryKey,
                newParent,
                null,
                mergedReferences.References,
                newAttributeContainer,
                newAssociatedDataContainer,
                priceContainer,
                entityLocales,
                null,
                mergedReferences.ReferencesDefined,
                entitySchema.WithHierarchy || newParent is not null
            );
        }

        return entity ?? new Entity(entitySchema.Name, null);
    }

    private static Prices RecreatePrices(
        IEntitySchema entitySchema,
        Entity? possibleEntity,
        PriceInnerRecordHandling? newPriceInnerRecordHandling,
        IDictionary<PriceKey, IPrice> newPrices
    )
    {
        Prices priceContainer;
        if (!newPrices.Any())
        {
            if (newPriceInnerRecordHandling is not null)
            {
                if (possibleEntity is not null)
                {
                    priceContainer = new Prices(entitySchema, possibleEntity.Version + 1,
                        possibleEntity.Prices.GetPrices(), newPriceInnerRecordHandling.Value,
                        possibleEntity.Prices.GetPrices().Any());
                }
                else
                {
                    priceContainer = new Prices(entitySchema, 1, new List<IPrice>(), newPriceInnerRecordHandling.Value,
                        false);
                }
            }
            else
            {
                priceContainer = possibleEntity is not null
                    ? possibleEntity.Prices
                    : new Prices(entitySchema, 1, new List<IPrice>(), PriceInnerRecordHandling.None, false);
            }
        }
        else
        {
            List<IPrice> mergedPrices = possibleEntity is not null
                ? possibleEntity.GetPrices().Where(x => !newPrices.ContainsKey(x.Key)).ToList()
                : new List<IPrice>();
            mergedPrices.AddRange(newPrices.Values);

            if (newPriceInnerRecordHandling is not null)
            {
                if (possibleEntity is not null)
                {
                    priceContainer = new Prices(entitySchema, possibleEntity.Version + 1,
                        mergedPrices, newPriceInnerRecordHandling.Value,
                        mergedPrices.Any());
                }
                else
                {
                    priceContainer = new Prices(entitySchema, 1, mergedPrices, newPriceInnerRecordHandling.Value,
                        mergedPrices.Any());
                }
            }
            else
            {
                if (possibleEntity is not null)
                {
                    priceContainer = new Prices(entitySchema, possibleEntity.Version + 1, mergedPrices,
                        possibleEntity.InnerRecordHandling, mergedPrices.Any());
                }
                else
                {
                    priceContainer = new Prices(entitySchema, 1, mergedPrices, PriceInnerRecordHandling.None,
                        mergedPrices.Any());
                }
            }
        }

        return priceContainer;
    }

    private static ReferenceTuple RecreateReferences(
        ISealedEntity? possibleEntity,
        IDictionary<ReferenceKey, IReference> newReferences
    )
    {
        ISet<string> mergedTypes;
        List<IReference> mergedReferences;
        if (!newReferences.Any())
        {
            if (possibleEntity is not null)
            {
                mergedTypes = possibleEntity.Schema.References.Keys.ToHashSet();
                mergedReferences = possibleEntity.GetReferences().ToList();
            }
            else
            {
                mergedTypes = new HashSet<string>();
                mergedReferences = new List<IReference>();
            }
        }
        else
        {
            if (possibleEntity is not null)
            {
                mergedTypes = possibleEntity.Schema.References.Keys.ToHashSet();
                foreach (var newReferencesValue in newReferences.Values)
                {
                    mergedTypes.Add(newReferencesValue.ReferenceName);
                }

                mergedReferences = possibleEntity.GetReferences().Where(x => !newReferences.ContainsKey(x.ReferenceKey))
                    .ToList();
                mergedReferences.AddRange(newReferences.Values);
            }
            else
            {
                mergedTypes = new HashSet<string>();
                mergedReferences = new List<IReference>();
            }
        }

        return new ReferenceTuple(
            mergedReferences,
            mergedTypes
        );
    }

    private static AssociatedData RecreateAssociatedDataContainer(
        IEntitySchema entitySchema,
        Entity? possibleEntity,
        IDictionary<AssociatedDataKey, AssociatedDataValue> newAssociatedData
    )
    {
        AssociatedData newAssociatedDataContainer;
        if (!newAssociatedData.Any())
        {
            newAssociatedDataContainer = possibleEntity?.AssociatedData ?? new AssociatedData(entitySchema);
        }
        else
        {
            List<AssociatedDataValue> associatedDataValues = possibleEntity is null
                ? new List<AssociatedDataValue>()
                : possibleEntity.GetAssociatedDataValues().Where(x => !newAssociatedData.ContainsKey(x.Key)).ToList();
            associatedDataValues.AddRange(newAssociatedData.Values);
            List<IAssociatedDataSchema> associatedDataSchemas = entitySchema.AssociatedData.Values.ToList();
            associatedDataSchemas.AddRange(newAssociatedData.Values
                .Where(x => !entitySchema.AssociatedData.ContainsKey(x.Key.AssociatedDataName))
                .Select(IAssociatedDataBuilder.CreateImplicitSchema));
            newAssociatedDataContainer = new AssociatedData(entitySchema, associatedDataValues,
                associatedDataSchemas.ToDictionary(x => x.Name, x => x));
        }

        return newAssociatedDataContainer;
    }

    private static PriceInnerRecordHandling? MutateInnerPriceRecordHandling(
        IEntitySchema entitySchema,
        Entity? possibleEntity,
        SetPriceInnerRecordHandlingMutation innerRecordHandlingMutation
    )
    {
        IPrices? existingPrices = possibleEntity?.Prices;
        IPrices? newPriceContainer = ReturnIfChanged(
            existingPrices,
            innerRecordHandlingMutation.MutateLocal(entitySchema, existingPrices)
        );
        PriceInnerRecordHandling? newPriceInnerRecordHandling =
            newPriceContainer?.InnerRecordHandling;
        return newPriceInnerRecordHandling;
    }

    private static void MutatePrices(
        IEntitySchema entitySchema,
        ISealedEntity? possibleEntity,
        IDictionary<PriceKey, IPrice> newPrices,
        PriceMutation priceMutation
    )
    {
        IPrice? existingPriceValue = possibleEntity?.GetPrice(priceMutation.PriceKey);
        IPrice? changedValue = ReturnIfChanged(existingPriceValue,
            priceMutation.MutateLocal(entitySchema, existingPriceValue));
        if (changedValue is not null)
        {
            newPrices.Add(changedValue.Key, changedValue);
        }
    }

    private static void MutateReferences(
        IEntitySchema entitySchema,
        Entity? possibleEntity,
        IDictionary<ReferenceKey, IReference> newReferences,
        ReferenceMutation referenceMutation)
    {
        IReference? existingReferenceValue;
        if (possibleEntity is null)
        {
            existingReferenceValue = newReferences.TryGetValue(referenceMutation.ReferenceKey, out IReference? value)
                ? value
                : null;
        }
        else
        {
            existingReferenceValue = possibleEntity.GetReferenceWithoutSchemaCheck(referenceMutation.ReferenceKey);
        }

        IReference? changedValue = ReturnIfChanged(existingReferenceValue,
            referenceMutation.MutateLocal(entitySchema, existingReferenceValue));
        if (changedValue is not null)
        {
            newReferences.Add(changedValue.ReferenceKey, changedValue);
        }
    }

    private static void MutateAssociatedData(
        IEntitySchema entitySchema,
        ISealedEntity? possibleEntity,
        IDictionary<AssociatedDataKey, AssociatedDataValue> newAssociatedData,
        AssociatedDataMutation associatedDataMutation
    )
    {
        AssociatedDataValue? existingAssociatedDataValue;
        if (possibleEntity is null)
        {
            existingAssociatedDataValue = null;
        }
        else
        {
            AssociatedDataKey associatedDataKey = associatedDataMutation.AssociatedDataKey;
            existingAssociatedDataValue =
                possibleEntity.GetAssociatedDataNames().Contains(associatedDataKey.AssociatedDataName)
                    ? possibleEntity.GetAssociatedDataValue(associatedDataKey)
                    : null;
        }

        AssociatedDataValue? changedValue = ReturnIfChanged(existingAssociatedDataValue,
            associatedDataMutation.MutateLocal(entitySchema, existingAssociatedDataValue));
        if (changedValue is not null)
        {
            newAssociatedData.Add(changedValue.Key, changedValue);
        }
    }

    private static void MutateAttributes(
        IEntitySchema entitySchema,
        ISealedEntity? possibleEntity,
        IDictionary<AttributeKey, AttributeValue> newAttributes,
        AttributeMutation attributeMutation
    )
    {
        AttributeValue? existingAttributeValue;
        if (possibleEntity is null)
        {
            existingAttributeValue = null;
        }
        else
        {
            AttributeKey attributeKey = attributeMutation.AttributeKey;
            existingAttributeValue = possibleEntity.GetAttributeNames().Contains(attributeKey.AttributeName)
                ? possibleEntity.GetAttributeValue(attributeKey)
                : null;
        }

        AttributeValue? changedValue = ReturnIfChanged(existingAttributeValue,
            attributeMutation.MutateLocal(entitySchema, existingAttributeValue));
        if (changedValue is not null)
        {
            newAttributes.Add(changedValue.Key, changedValue);
        }
    }

    private static int? MutateHierarchyPlacement(
        IEntitySchema entitySchema,
        ISealedEntity? possibleEntity,
        ParentMutation parentMutation
    )
    {
        int? existingPlacement = possibleEntity?.Parent;
        int? newParent = parentMutation.MutateLocal(entitySchema, existingPlacement);
        return newParent;
    }

    private static T? ReturnIfChanged<T>(T? originalValue, T mutatedValue) where T : IVersioned
    {
        return mutatedValue.Version > originalValue?.Version ? mutatedValue : default;
    }

    private static Attributes RecreateAttributeContainer(
        IEntitySchema entitySchema,
        Entity? possibleEntity,
        Dictionary<AttributeKey, AttributeValue> newAttributes
    )
    {
        Attributes newAttributeContainer;
        if (!newAttributes.Any())
        {
            newAttributeContainer = possibleEntity?.Attributes ?? new Attributes(entitySchema);
        }
        else
        {
            List<AttributeValue> attributeValues = possibleEntity is null
                ? new List<AttributeValue>()
                : possibleEntity.GetAttributeValues().Where(x => !newAttributes.ContainsKey(x.Key)).ToList();
            attributeValues.AddRange(newAttributes.Values);
            List<IAttributeSchema> attributeSchemas = entitySchema.Attributes.Values.ToList();
            attributeSchemas.AddRange(newAttributes.Values
                .Where(x => !entitySchema.Attributes.ContainsKey(x.Key.AttributeName))
                .Select(IAttributesBuilder.CreateImplicitSchema));
            newAttributeContainer = new Attributes(entitySchema, null, attributeValues,
                attributeSchemas.ToDictionary(x => x.Name, x => x));
        }

        return newAttributeContainer;
    }

    public IEnumerable<IReference> GetReferences()
    {
        return References.Values;
    }

    public IEnumerable<IReference> GetReferences(string referenceName)
    {
        return References.Values
            .Where(x => x.ReferenceName == referenceName)
            .ToList();
    }

    public IReference? GetReference(string referenceName, int referencedEntityId)
    {
        return References.TryGetValue(new ReferenceKey(referenceName, referencedEntityId), out var reference)
            ? reference
            : null;
    }

    public ISet<CultureInfo> GetAllLocales()
    {
        return Locales;
    }

    public object? GetAttribute(string attributeName)
    {
        return Attributes.GetAttribute(attributeName);
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        return Attributes.GetAttributeArray(attributeName);
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        return Attributes.GetAttributeValue(attributeName);
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        return Attributes.GetAttribute(attributeName, locale);
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        return Attributes.GetAttributeArray(attributeName, locale);
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        return Attributes.GetAttributeValue(attributeName, locale);
    }

    public IAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return Attributes.GetAttributeSchema(attributeName);
    }

    public ISet<string> GetAttributeNames()
    {
        return Attributes.GetAttributeNames();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        return Attributes.GetAttributeKeys();
    }

    public ICollection<AttributeValue> GetAttributeValues()
    {
        return Attributes.GetAttributeValues();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        return Attributes.GetAttributeValues(attributeName);
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        return Attributes.GetAttributeLocales();
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        return Attributes.GetAttributeValue(attributeKey);
    }

    public object? GetAssociatedData(string associatedDataName)
    {
        return AssociatedData.GetAssociatedData(associatedDataName);
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        return AssociatedData.GetAssociatedDataArray(associatedDataName);
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        return AssociatedData.GetAssociatedDataValue(associatedDataName);
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return AssociatedData.GetAssociatedData(associatedDataName, locale);
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        return AssociatedData.GetAssociatedDataArray(associatedDataName, locale);
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        return AssociatedData.GetAssociatedDataValue(associatedDataName, locale) ??
               AssociatedData.GetAssociatedDataValue(associatedDataName);
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return AssociatedData.GetAssociatedDataSchema(associatedDataName);
    }

    public ISet<string> GetAssociatedDataNames()
    {
        return AssociatedData.GetAssociatedDataNames();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return AssociatedData.GetAssociatedDataKeys();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return AssociatedData.GetAssociatedDataValues();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return AssociatedData.GetAssociatedDataValues();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        return AssociatedData.GetAssociatedDataLocales();
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        return AssociatedData.GetAssociatedDataValue(associatedDataKey);
    }

    public IPrice? GetPrice(PriceKey priceKey)
    {
        return Prices.GetPrice(priceKey);
    }

    public IPrice? GetPrice(int priceId, string priceList, Currency currency)
    {
        return Prices.GetPrice(priceId, priceList, currency);
    }

    public List<IPrice> GetAllPricesForSale()
    {
        return Prices.GetAllPricesForSale(null, null);
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        throw new ContextMissingException();
    }

    public IEnumerable<IPrice> GetPrices()
    {
        return Prices.GetPrices();
    }

    public IReference? GetReferenceWithoutSchemaCheck(ReferenceKey referenceKey)
    {
        return References.TryGetValue(referenceKey, out var reference) ? reference : null;
    }

    public int GetPricesVersion()
    {
        return Prices.Version;
    }

    public IReference? GetReference(ReferenceKey referenceKey)
    {
        return References.TryGetValue(referenceKey, out var reference) ? reference : null;
    }

    private record ReferenceTuple(IEnumerable<IReference> References, ISet<string> ReferencesDefined);

    public bool DiffersFrom(IEntity? otherObject)
    {
        return (this as IEntity).DiffersFrom(otherObject);
    }

    public override string ToString()
    {
        ISet<CultureInfo> locales = Locales;
        return (Dropped ? "❌ " : "") +
               "Entity " + Type + " ID=" + PrimaryKey +
               (ParentAvailable()
                   ? $"{", ↰ " + ParentEntity?.PrimaryKey}"
                   : "" +
                     (ReferencesAvailable()
                         ? ", " + string.Join(", ", GetReferences().Select(it => it.ToString()))
                         : "") +
                     (AttributesAvailable()
                         ? string.Join(", ", GetAttributeValues().Select(it => it.ToString()))
                         : "") +
                     (AssociatedDataAvailable()
                         ? string.Join(", ", GetAssociatedDataValues().Select(it => it.ToString()))
                         : "") +
                     (PricesAvailable() ? string.Join(", ", GetPrices().Select(it => it.ToString())) : "") +
                     (!locales.Any()
                         ? ""
                         : ", localized to " + string.Join(", ", locales.Select(x => x.TwoLetterISOLanguageName))));
    }
}