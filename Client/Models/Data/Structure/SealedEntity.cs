using System.Collections.Immutable;
using System.Globalization;
using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Data.Mutations;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Schemas.Dtos;
using Client.Queries.Requires;

namespace Client.Models.Data.Structure;

public class SealedEntity : IEntity
{
    public int Version { get; }
    public string EntityType { get; }
    public int? PrimaryKey { get; }
    public EntitySchema Schema { get; }
    public int? Parent { get; }
    public Dictionary<ReferenceKey, Reference> References { get; }
    public Attributes Attributes { get; }
    public AssociatedData AssociatedData { get; }
    public Prices Prices { get; }
    public ISet<CultureInfo> Locales { get; }

    public static SealedEntity InternalBuild(
        int? primaryKey,
        int? version,
        EntitySchema entitySchema,
        int? parent,
        IEnumerable<Reference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        IEnumerable<CultureInfo> locales)
    {
        return new SealedEntity(
            version ?? 1,
            entitySchema,
            primaryKey,
            parent,
            references,
            attributes,
            associatedData,
            prices,
            locales
        );
    }

    public static SealedEntity InternalBuild(
        SealedEntity entity,
        int? primaryKey,
        int? version,
        EntitySchema entitySchema,
        int? parent,
        IEnumerable<Reference>? references,
        Attributes? attributes,
        AssociatedData? associatedData,
        Prices? prices,
        ISet<CultureInfo>? locales)
    {
        return new SealedEntity(
            version ?? 1,
            entitySchema,
            primaryKey,
            parent,
            references ?? entity.References.Values,
            attributes ?? entity.Attributes,
            associatedData ?? entity.AssociatedData,
            prices ?? entity.Prices,
            locales ?? entity.Locales.ToImmutableHashSet()
        );
    }

    private SealedEntity(
        int version,
        EntitySchema schema,
        int? primaryKey,
        int? parent,
        IEnumerable<Reference> references,
        Attributes attributes,
        AssociatedData associatedData,
        Prices prices,
        IEnumerable<CultureInfo> locales
    )
    {
        Version = version;
        EntityType = schema.Name;
        Schema = schema;
        PrimaryKey = primaryKey;
        Parent = parent;
        References = references.ToDictionary(x => x.ReferenceKey, x => x);
        Attributes = attributes;
        AssociatedData = associatedData;
        Prices = prices;
        Locales = new HashSet<CultureInfo>(locales).ToImmutableHashSet();
    }

    public SealedEntity(string type, int? primaryKey)
    {
        Version = 1;
        EntityType = type;
        Schema = EntitySchema.InternalBuild(type);
        PrimaryKey = primaryKey;
        Parent = null;
        References = new Dictionary<ReferenceKey, Reference>();
        Attributes = new Attributes(Schema);
        AssociatedData = new AssociatedData(Schema);
        Prices = new Prices(1, new HashSet<Price>(), PriceInnerRecordHandling.None);
        Locales = new HashSet<CultureInfo>().ToImmutableHashSet();
    }

    public static SealedEntity MutateEntity(
        EntitySchema entitySchema,
        SealedEntity? entity,
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
        Dictionary<ReferenceKey, Reference> newReferences =
            new Dictionary<ReferenceKey, Reference>(localMutations.Count);
        Dictionary<PriceKey, Price> newPrices = new Dictionary<PriceKey, Price>(localMutations.Count);

        foreach (ILocalMutation localMutation in localMutations)
        {
            if (localMutation is AttributeMutation attributeMutation)
            {
                MutateAttributes(entitySchema, entity, newAttributes, attributeMutation);
            }
            /*else if (localMutation is HierarchicalPlacementMutation hierarchicalPlacementMutation)
            {
                newPlacement = mutateHierarchyPlacement(entitySchema, possibleEntity, hierarchicalPlacementMutation);
            }
            else if (localMutation is HierarchicalPlacementMutation hierarchicalPlacementMutation)
            {
                newPlacement = mutateHierarchyPlacement(entitySchema, possibleEntity, hierarchicalPlacementMutation);
            }
            else if (localMutation is AssociatedDataMutation associatedDataMutation)
            {
                mutateAssociatedData(entitySchema, possibleEntity, newAssociatedData, associatedDataMutation);
            }
            else if (localMutation instanceof ReferenceMutation<?> referenceMutation) 
            { 
                mutateReferences(entitySchema, possibleEntity, newReferences, referenceMutation);
            } 
            else if (localMutation instanceof PriceMutation priceMutation) 
            {
                mutatePrices(entitySchema, possibleEntity, newPrices, priceMutation);
            } 
            else if (localMutation instanceof SetPriceInnerRecordHandlingMutation innerRecordHandlingMutation) {
                newPriceInnerRecordHandling =
                    mutateInnerPriceRecordHandling(entitySchema, possibleEntity, innerRecordHandlingMutation);
            }*/
        }

        // create or reuse existing attribute container
        Attributes newAttributeContainer = RecreateAttributeContainer(entitySchema, entity, newAttributes);

        // create or reuse existing associated data container
        /*AssociatedData newAssociatedDataContainer =
            recreateAssociatedDataContainer(entitySchema, entity, newAssociatedData);*/

        // create or reuse existing reference container
        /*ICollection<ReferenceContract>
        mergedReferences = recreateReferences(entity, newReferences);*/

        // create or reuse existing prices
        /*Prices priceContainer = recreatePrices(entity, newPriceInnerRecordHandling, newPrices);*/

        // aggregate entity locales
        /*ISet<CultureInfo> entityLocales = new HashSet<>(newAttributeContainer.getAttributeLocales());
        entityLocales.addAll(newAssociatedDataContainer.getAssociatedDataLocales());*/

        if (newParent != oldParent || newPriceInnerRecordHandling != null ||
            newAttributes.Any() || newAssociatedData.Any() || newPrices.Any() ||
            newReferences.Any())
        {
            //TODO: ADD SUPPORT FOR OTHER DATA
            return new SealedEntity(
                entity is null ? 1 : entity.Version + 1,
                entitySchema,
                entity?.PrimaryKey,
                newParent,
                entity?.GetReferences(),
                newAttributeContainer,
                entity?.AssociatedData,
                entity.Prices,
                entity?.Locales
            );
        }
        return entity ?? new SealedEntity(entitySchema.Name, null);
    }
    
    private static void MutateAttributes(
        EntitySchema entitySchema,
    SealedEntity? possibleEntity,
    Dictionary<AttributeKey, AttributeValue> newAttributes,
    AttributeMutation attributeMutation
    ) {
        AttributeValue? existingAttributeValue;
        if (possibleEntity is null)
        {
            existingAttributeValue = null;
        }
        else
        {
            existingAttributeValue = possibleEntity.GetAttributeValue(attributeMutation.AttributeKey);
        }
        
        /*ReturnIfChanged(existingAttributeValue, attributeMutation.)
        
        ofNullable(
            returnIfChanged(
                existingAttributeValue,
                attributeMutation.mutateLocal(entitySchema, existingAttributeValue)
            )
        ).ifPresent(it -> newAttributes.put(it.getKey(), it));*/
        
        //TODO: ?
    }
    
    private static T? ReturnIfChanged<T>(T? originalValue, T mutatedValue) where T : IVersioned
    {
        if (mutatedValue.Version > originalValue?.Version)
        {
            return mutatedValue;
        }
        return default;
    }

    private static Attributes RecreateAttributeContainer(
        EntitySchema entitySchema,
        SealedEntity? possibleEntity,
        Dictionary<AttributeKey, AttributeValue> newAttributes
    )
    {
        Attributes newAttributeContainer;
        if (!newAttributes.Any())
        {
            if (possibleEntity.Attributes.Empty)
            {
                newAttributeContainer = new Attributes(entitySchema);
            }
            else
            {
                newAttributeContainer = possibleEntity.Attributes;
            }
        }
        else
        {
            List<AttributeValue> attributeValues = possibleEntity is null
                ? new List<AttributeValue>()
                : possibleEntity.GetAttributeValues().Where(x => !newAttributes.ContainsKey(x.Key)).ToList()!;
            attributeValues.AddRange(newAttributes.Values);
            newAttributeContainer = new Attributes(entitySchema, attributeValues!);
        }

        return newAttributeContainer;
    }

    public ICollection<Reference> GetReferences()
    {
        return References.Values;
    }

    public ICollection<Reference> GetReferences(string referenceName)
    {
        return References.Values
            .Where(x => x.ReferenceName == referenceName)
            .ToList();
    }

    public Reference? GetReference(string referenceName, int referencedEntityId)
    {
        return References[new ReferenceKey(referenceName, referencedEntityId)];
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

    public AttributeSchema? GetAttributeSchema(string attributeName)
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

    public ICollection<AttributeValue?> GetAttributeValues()
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

    public object? GetAssociatedDataArray(string associatedDataName)
    {
        return AssociatedData.GetAssociatedData(associatedDataName);
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

    public AssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
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

    public Price? GetPrice(PriceKey priceKey)
    {
        return Prices.GetPrice(priceKey);
    }

    public Price? GetPrice(int priceId, string priceList, Currency currency)
    {
        return Prices.GetPrice(priceId, priceList, currency);
    }

    public Price? GetPriceForSale()
    {
        throw new ContextMissingException();
    }

    public Price? GetPriceForSaleIfAvailable()
    {
        return null;
    }

    public List<Price> GetAllPricesForSale()
    {
        return Prices.GetAllPricesForSale(null, null);
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        throw new ContextMissingException();
    }

    public IEnumerable<Price> GetPrices()
    {
        return Prices.GetPrices();
    }

    public PriceInnerRecordHandling GetPriceInnerRecordHandling()
    {
        return Prices.InnerRecordHandling;
    }

    public int GetPricesVersion()
    {
        return Prices.Version;
    }

    public Reference? GetReference(ReferenceKey referenceKey)
    {
        return References[referenceKey];
    }
}