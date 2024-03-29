﻿using System.Globalization;
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
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Builder that is used to alter existing <see cref="Entity"/>. Entity is immutable object so there is need for another object
/// that would simplify the process of updating its contents. This is why the builder class exists.
/// This builder is suitable for the situation when there already is some entity at place, and we need to alter it.
/// </summary>
public class ExistingEntityBuilder : IEntityBuilder
{
    private Entity BaseEntity { get; }
    private ExistingEntityAttributesBuilder AttributesBuilder { get; }
    private ExistingAssociatedDataBuilder AssociatedDataBuilder { get; }
    private ExistingPricesBuilder PricesBuilder { get; }
    private IDictionary<ReferenceKey, List<ReferenceMutation>> ReferenceMutations { get; }
    private ISet<ReferenceKey> RemovedReferences { get; } = new HashSet<ReferenceKey>();
    private ParentMutation? HierarchyMutation { get; set; }

    public string Type => BaseEntity.Type;
    public int? PrimaryKey => BaseEntity.PrimaryKey;

    public LocalePredicate LocalePredicate { get; }
    public HierarchyPredicate HierarchyPredicate { get; }
    public AttributeValuePredicate AttributePredicate { get; }
    public AssociatedDataValuePredicate AssociatedDataPredicate { get; }
    public ReferencePredicate ReferencePredicate { get; }
    public PricePredicate PricePredicate { get; }

    private static void AssertPricesFetched(PricePredicate pricePredicate)
    {
        Assert.IsTrue(pricePredicate.PriceContentMode == PriceContentMode.All,
            "Prices were not fetched and cannot be updated. Please enrich the entity first or load it with all the prices.");
    }

    public IEntityClassifierWithParent? ParentEntity
    {
        get
        {
            if (HierarchyMutation is not null)
            {
                return new EntityReferenceWithParent(
                    Type,
                    HierarchyMutation.MutateLocal(BaseEntity.Schema, BaseEntity.Parent),
                    null
                );
            }

            return BaseEntity.ParentEntity;
        }
    }

    public int Version => BaseEntity.Version + 1;
    public PriceInnerRecordHandling? InnerRecordHandling => PricesBuilder.InnerRecordHandling;
    public IEntitySchema Schema => BaseEntity.Schema;
    public int? Parent => BaseEntity.Parent;
    public bool Dropped => false;
    public IPrice? PriceForSale => PricesBuilder.PriceForSale;

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

    public IList<IPrice> GetPrices()
    {
        return PricesBuilder.GetPrices();
    }

    public bool PricesAvailable()
    {
        return PricesBuilder.PricesAvailable();
    }

    public IList<IPrice> GetAllPricesForSale(Currency? currency, DateTimeOffset? atTheMoment, params string[] priceListPriority)
    {
        return ((IPrices) PricesBuilder).GetAllPricesForSale(currency, atTheMoment, priceListPriority);
    }

    public IList<IPrice> GetAllPricesForSale()
    {
        return ((IPrices) PricesBuilder).GetAllPricesForSale();
    }

    public ExistingEntityBuilder(Entity baseEntity, ICollection<ILocalMutation> localMutations)
    {
        BaseEntity = baseEntity;
        
        LocalePredicate = baseEntity.LocalePredicate;
        HierarchyPredicate = baseEntity.HierarchyPredicate;
        AttributePredicate = baseEntity.AttributePredicate;
        AssociatedDataPredicate = baseEntity.AssociatedDataPredicate;
        ReferencePredicate = baseEntity.ReferencePredicate;
        PricePredicate = baseEntity.PricePredicate;
        
        AttributesBuilder = new ExistingEntityAttributesBuilder(BaseEntity.Schema, BaseEntity.Attributes);
        AssociatedDataBuilder = new ExistingAssociatedDataBuilder(BaseEntity.Schema, BaseEntity.AssociatedData);
        PricesBuilder = new ExistingPricesBuilder(BaseEntity.Schema, BaseEntity.Prices, PricePredicate);
        ReferenceMutations = new Dictionary<ReferenceKey, List<ReferenceMutation>>();
        foreach (ILocalMutation localMutation in localMutations)
        {
            AddMutation(localMutation);
        }
    }

    public ExistingEntityBuilder(Entity baseEntity) : this(baseEntity, new List<ILocalMutation>())
    {
    }

    private void AddMutation(ILocalMutation localMutation)
    {
        if (localMutation is ParentMutation hierarchicalPlacementMutation)
        {
            HierarchyMutation = hierarchicalPlacementMutation;
        }
        else if (localMutation is AttributeMutation attributeMutation)
        {
            AttributesBuilder.AddMutation(attributeMutation);
        }
        else if (localMutation is AssociatedDataMutation associatedDataMutation)
        {
            AssociatedDataBuilder.AddMutation(associatedDataMutation);
        }
        else if (localMutation is ReferenceMutation referenceMutation)
        {
            if (!ReferenceMutations.ContainsKey(referenceMutation.ReferenceKey))
            {
                ReferenceMutations.Add(referenceMutation.ReferenceKey, new List<ReferenceMutation>());
            }

            ReferenceMutations[referenceMutation.ReferenceKey].Add(referenceMutation);
        }
        else if (localMutation is PriceMutation priceMutation)
        {
            PricesBuilder.AddMutation(priceMutation);
        }
        else if (localMutation is SetPriceInnerRecordHandlingMutation innerRecordHandlingMutation)
        {
            PricesBuilder.AddMutation(innerRecordHandlingMutation);
        }
        else
        {
            // SHOULD NOT EVER HAPPEN
            throw new EvitaInternalError("Unknown mutation: " + localMutation.GetType());
        }
    }

    public IEnumerable<IReference> GetReferences()
    {
        return BaseEntity.GetReferences()
            .Where(x => !x.Dropped)
            .Select(it =>
            {
                if (ReferenceMutations.TryGetValue(it.ReferenceKey, out List<ReferenceMutation>? mutations))
                {
                    IReference? evaluationResult = EvaluateReferenceMutations(it, mutations);
                    if (evaluationResult is not null && evaluationResult.DiffersFrom(it))
                    {
                        return evaluationResult;
                    }

                    return it;
                }

                return null;
            }).Concat(
                ReferenceMutations
                    .Where(
                        it => BaseEntity.GetReference(it.Key.ReferenceName, it.Key.PrimaryKey) is null)
                    .Select(x => x.Value)
                    .Select(it => EvaluateReferenceMutations(null, it))
            )
            .Where(x => x is not null).ToList()!;
    }

    public IEnumerable<IReference> GetReferences(string referenceName)
    {
        return GetReferences()
            .Where(it => Equals(referenceName, it.ReferenceName))
            .ToList();
    }

    public IReference? GetReference(string referenceName, int referencedEntityId)
    {
        ReferenceKey entityReferenceContract = new ReferenceKey(referenceName, referencedEntityId);
        IReference? reference = BaseEntity.GetReference(referenceName, referencedEntityId);

        if (ReferenceMutations.TryGetValue(entityReferenceContract, out List<ReferenceMutation>? mutations))
        {
            EvaluateReferenceMutations(reference, mutations);
        }
        else
        {
            if (ReferenceMutations.TryGetValue(entityReferenceContract,
                    out List<ReferenceMutation>? referenceMutations))
            {
                EvaluateReferenceMutations(null, referenceMutations);
            }
        }

        return reference;
    }

    public ISet<CultureInfo> GetAllLocales()
    {
        return AttributesBuilder
            .GetAttributeLocales()
            .Concat(AssociatedDataBuilder.GetAssociatedDataLocales())
            .ToHashSet();
    }

    public bool ParentAvailable()
    {
        return BaseEntity.ParentAvailable();
    }

    public bool ReferencesAvailable()
    {
        return BaseEntity.ReferencesAvailable();
    }

    public bool DiffersFrom(IEntity? otherObject)
    {
        return (this as IEntity).DiffersFrom(otherObject);
    }

    public IEntityBuilder RemoveAttribute(string attributeName)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName),
            "Attribute " + attributeName + " was not fetched and cannot be removed. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.RemoveAttribute(attributeName);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName),
            "Attribute " + attributeName + " was not fetched and cannot be updated. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName),
            "Attribute " + attributeName + " was not fetched and cannot be updated. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.SetAttribute(attributeName, attributeValue);
        return this;
    }

    public IEntityBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName, locale),
            "Attribute " + attributeName + " in locale " + locale +" was not fetched and cannot be removed. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.RemoveAttribute(attributeName, locale);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName, locale),
            "Attribute " + attributeName + " in locale " + locale +" was not fetched and cannot be updated. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(attributeName, locale),
            "Attribute " + attributeName + " in locale " + locale +" was not fetched and cannot be updated. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.SetAttribute(attributeName, locale, attributeValue);
        return this;
    }

    public IEntityBuilder MutateAttribute(AttributeMutation mutation)
    {
        Assert.IsTrue(
            mutation.AttributeKey.Localized ? 
                AttributePredicate.WasFetched(mutation.AttributeKey.AttributeName, mutation.AttributeKey.Locale!) :
                AttributePredicate.WasFetched(mutation.AttributeKey.AttributeName),
            "Attribute " + mutation.AttributeKey.AttributeName + " in locale " + mutation.AttributeKey.Locale +" was not fetched and cannot be updated. Please enrich the entity first or load it with attributes."
        );
        AttributesBuilder.MutateAttribute(mutation);
        return this;
    }

    public IEntityBuilder RemoveAssociatedData(string associatedDataName)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " was not fetched and cannot be removed. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.RemoveAssociatedData(associatedDataName);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, object? associatedDataValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " was not fetched and cannot be updated. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, associatedDataValue);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, object[]? associatedDataValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " was not fetched and cannot be updated. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, associatedDataValue);
        return this;
    }

    public IEntityBuilder RemoveAssociatedData(string associatedDataName, CultureInfo locale)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " in locale " + locale + " was not fetched and cannot be removed. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.RemoveAssociatedData(associatedDataName, locale);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, CultureInfo locale, object? associatedDataValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " in locale " + locale + " was not fetched and cannot be updated. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, locale, associatedDataValue);
        return this;
    }

    public IEntityBuilder SetAssociatedData(string associatedDataName, CultureInfo locale,
        object[]? associatedDataValue)
    {
        Assert.IsTrue(
            AttributePredicate.WasFetched(associatedDataName),
            "Associated data " + associatedDataName + " in locale " + locale + " was not fetched and cannot be updated. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.SetAssociatedData(associatedDataName, locale, associatedDataValue);
        return this;
    }

    public IEntityBuilder MutateAssociatedData(AssociatedDataMutation mutation)
    {
        Assert.IsTrue(
            mutation.AssociatedDataKey.Localized ? 
                AttributePredicate.WasFetched(mutation.AssociatedDataKey.AssociatedDataName, mutation.AssociatedDataKey.Locale!) :
                AttributePredicate.WasFetched(mutation.AssociatedDataKey.AssociatedDataName),
            "Associated data " + mutation.AssociatedDataKey.AssociatedDataName + " in locale " + mutation.AssociatedDataKey.Locale +" was not fetched and cannot be updated. Please enrich the entity first or load it with associated data."
        );
        AssociatedDataBuilder.MutateAssociatedData(mutation);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, bool sellable)
    {
        Assert.IsTrue(
            PricePredicate.Test(new Price(new PriceKey(priceId, priceList, currency), 1, decimal.One, decimal.One, decimal.One, null, false)),
            "Price " + priceId + ", " + priceList + ", " + currency + " was not fetched and cannot be updated. Please enrich the entity first or load it with the prices."
        );
        PricesBuilder.SetPrice(priceId, priceList, currency, priceWithoutTax, taxRate, priceWithTax, sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, bool sellable)
    {
        Assert.IsTrue(
            PricePredicate.Test(new Price(new PriceKey(priceId, priceList, currency), 1, decimal.One, decimal.One, decimal.One, null, false)),
            "Price " + priceId + ", " + priceList + ", " + currency + " was not fetched and cannot be updated. Please enrich the entity first or load it with the prices."
        );
        PricesBuilder.SetPrice(priceId, priceList, currency, innerRecordId, priceWithoutTax, taxRate, priceWithTax,
            sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, decimal priceWithoutTax,
        decimal taxRate,
        decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        Assert.IsTrue(
            PricePredicate.Test(new Price(new PriceKey(priceId, priceList, currency), 1, decimal.One, decimal.One, decimal.One, null, false)),
            "Price " + priceId + ", " + priceList + ", " + currency + " was not fetched and cannot be updated. Please enrich the entity first or load it with the prices."
        );
        PricesBuilder.SetPrice(priceId, priceList, currency, priceWithoutTax, taxRate, priceWithTax, validity,
            sellable);
        return this;
    }

    public IEntityBuilder SetPrice(int priceId, string priceList, Currency currency, int? innerRecordId,
        decimal priceWithoutTax,
        decimal taxRate, decimal priceWithTax, DateTimeRange? validity, bool sellable)
    {
        Assert.IsTrue(
            PricePredicate.Test(new Price(new PriceKey(priceId, priceList, currency), 1, decimal.One, decimal.One, decimal.One, null, false)),
            "Price " + priceId + ", " + priceList + ", " + currency + " was not fetched and cannot be updated. Please enrich the entity first or load it with the prices."
        );
        PricesBuilder.SetPrice(priceId, priceList, currency, innerRecordId, priceWithoutTax, taxRate, priceWithTax,
            validity, sellable);
        return this;
    }

    public IEntityBuilder RemovePrice(int priceId, string priceList, Currency currency)
    {
        Assert.IsTrue(
            PricePredicate.Test(new Price(new PriceKey(priceId, priceList, currency), 1, decimal.One, decimal.One, decimal.One, null, false)),
            "Price " + priceId + ", " + priceList + ", " + currency + " was not fetched and cannot be removed. Please enrich the entity first or load it with the prices."
        );
        PricesBuilder.RemovePrice(priceId, priceList, currency);
        return this;
    }

    public IEntityBuilder SetPriceInnerRecordHandling(PriceInnerRecordHandling priceInnerRecordHandling)
    {
        AssertPricesFetched(PricePredicate);
        PricesBuilder.SetPriceInnerRecordHandling(priceInnerRecordHandling);
        return this;
    }

    public IEntityBuilder RemovePriceInnerRecordHandling()
    {
        AssertPricesFetched(PricePredicate);
        PricesBuilder.RemovePriceInnerRecordHandling();
        return this;
    }

    public IEntityBuilder RemoveAllNonTouchedPrices()
    {
        AssertPricesFetched(PricePredicate);
        PricesBuilder.RemoveAllNonTouchedPrices();
        return this;
    }

    public IEntityBuilder SetParent(int parentPrimaryKey)
    {
        HierarchyMutation = !Equals(BaseEntity.Parent, parentPrimaryKey)
            ? new SetParentMutation(parentPrimaryKey)
            : null;
        return this;
    }

    public IEntityBuilder RemoveParent()
    {
        Assert.NotNull(BaseEntity.Parent, "Cannot remove parent that is not present!");
        HierarchyMutation = BaseEntity.Parent is not null ? new RemoveParentMutation() : null;
        return this;
    }

    public IEntityBuilder SetReference(string referenceName, int referencedPrimaryKey)
    {
        return SetReference(referenceName, referencedPrimaryKey, null);
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
        Assert.IsTrue(
            ReferencePredicate.WasFetched(referenceName),
            "References were not fetched and cannot be updated. Please enrich the entity first or load it with the references."
        );
        ReferenceKey referenceKey = new ReferenceKey(referenceName, referencedPrimaryKey);
        IEntitySchema schema = Schema;
        IReference? existingReference = BaseEntity.GetReferenceWithoutSchemaCheck(referenceKey);
        IReferenceBuilder referenceBuilder;
        if (existingReference is not null)
        {
            referenceBuilder = new ExistingReferenceBuilder(existingReference, schema);
        }
        else
        {
            referenceBuilder = new InitialReferenceBuilder(schema, referenceName, referencedPrimaryKey, cardinality,
                referencedEntityType);
        }

        whichIs?.Invoke(referenceBuilder);
        if (existingReference is not null)
        {
            IReference? existingBaseReference = BaseEntity.GetReference(referencedEntityType, referencedPrimaryKey);
            IReference? referenceInBaseEntity = existingBaseReference is not null && existingBaseReference.Dropped
                ? null
                : existingReference;
            List<ReferenceMutation> changeSet = referenceBuilder.BuildChangeSet().ToList();
            if (referenceInBaseEntity is not null && referenceInBaseEntity.Dropped &&
                !RemovedReferences.Contains(referenceKey))
            {
                ReferenceMutations.Add(
                    referenceKey,
                    changeSet
                );
            }
            else
            {
                if (referenceInBaseEntity?.Group is { Dropped: false })
                {
                    changeSet.Add(new RemoveReferenceGroupMutation(referenceKey));
                }

                changeSet.AddRange(
                    referenceInBaseEntity?.GetAttributeValues()
                        .Where(x => !x.Dropped)
                        .Select(x =>
                            new ReferenceAttributeMutation(referenceKey, new RemoveAttributeMutation(x.Key))) ??
                    Array.Empty<ReferenceAttributeMutation>()
                );


                ReferenceMutations.Add(
                    referenceKey,
                    changeSet
                );
            }
        }
        else
        {
            ReferenceMutations.Add(
                referenceKey,
                referenceBuilder.BuildChangeSet().ToList()
            );
        }

        return this;
    }

    public IEntityBuilder RemoveReference(string referenceName, int referencedPrimaryKey)
    {
        Assert.IsTrue(
            ReferencePredicate.WasFetched(referenceName),
            "References were not fetched and cannot be removed. Please enrich the entity first or load it with the references."
        );
        ReferenceKey referenceKey = new ReferenceKey(referenceName, referencedPrimaryKey);
        Assert.IsTrue(GetReference(referenceName, referencedPrimaryKey) is not null,
            "There's no reference of type " + referenceName + " and primary key " + referencedPrimaryKey + "!");
        IReference? theReference = BaseEntity.GetReferenceWithoutSchemaCheck(referenceKey);
        Assert.IsTrue(
            theReference is not null,
            "Reference to " + referenceName + " and primary key " + referenceKey +
            " is not present on the entity " + BaseEntity.Type + " and id " +
            BaseEntity.PrimaryKey + "!"
        );
        ReferenceMutations.Add(
            referenceKey,
            new List<ReferenceMutation> { new RemoveReferenceMutation(referenceKey) }
        );
        RemovedReferences.Add(referenceKey);
        return this;
    }

    public IEntityMutation? ToMutation()
    {
        IDictionary<ReferenceKey, IReference> builtReferences =
            new Dictionary<ReferenceKey, IReference>(BaseEntity.References);
        List<ILocalMutation> mutations = new List<ILocalMutation>();

        if (HierarchyMutation is not null)
        {
            mutations.Add(HierarchyMutation);
        }

        mutations.AddRange(AttributesBuilder.BuildChangeSet());
        mutations.AddRange(AssociatedDataBuilder.BuildChangeSet());
        mutations.AddRange(PricesBuilder.BuildChangeSet());

        foreach (var referenceMutations in ReferenceMutations.Values)
        {
            foreach (var referenceMutation in referenceMutations)
            {
                IReference? existingReference =
                    builtReferences.TryGetValue(referenceMutation.ReferenceKey, out var exReference)
                        ? exReference
                        : null;
                IReference newReference = referenceMutation.MutateLocal(BaseEntity.Schema, existingReference);
                builtReferences.Add(referenceMutation.ReferenceKey, newReference);
                if (existingReference == null || newReference.Version > existingReference.Version)
                {
                    mutations.Add(referenceMutation);
                }
            }
        }

        mutations.Sort();

        if (!mutations.Any())
        {
            return null;
        }

        return new EntityUpsertMutation(
            BaseEntity.Type,
            BaseEntity.PrimaryKey,
            EntityExistence.MustExist,
            mutations
        );
    }

    public Entity ToInstance()
    {
        return ToMutation()?.Mutate(BaseEntity.Schema, BaseEntity) ?? BaseEntity;
    }

    private IReferenceSchema GetReferenceSchemaOrThrowException(string referenceName)
    {
        return Schema.GetReference(referenceName) ?? throw new ReferenceNotKnownException(referenceName);
    }

    private IReference? EvaluateReferenceMutations(IReference? reference, List<ReferenceMutation> mutations)
    {
        IReference? mutatedReference = reference;
        foreach (ReferenceMutation mutation in mutations)
        {
            mutatedReference = mutation.MutateLocal(BaseEntity.Schema, mutatedReference);
        }

        return mutatedReference != null && mutatedReference.DiffersFrom(reference) ? mutatedReference : reference;
    }

    public bool AttributesAvailable()
    {
        return AttributesBuilder.AttributesAvailable();
    }

    public bool AttributesAvailable(CultureInfo locale)
    {
        return AttributesBuilder.AttributesAvailable(locale);
    }

    public bool AttributeAvailable(string attributeName)
    {
        return AttributesBuilder.AttributeAvailable(attributeName);
    }

    public bool AttributeAvailable(string attributeName, CultureInfo locale)
    {
        return AttributesBuilder.AttributeAvailable(attributeName, locale);
    }

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

    public bool AssociatedDataAvailable()
    {
        return AssociatedDataBuilder.AssociatedDataAvailable();
    }

    public bool AssociatedDataAvailable(CultureInfo locale)
    {
        return AssociatedDataBuilder.AssociatedDataAvailable(locale);
    }

    public bool AssociatedDataAvailable(string associatedDataName)
    {
        return AssociatedDataBuilder.AssociatedDataAvailable(associatedDataName);
    }

    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale)
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
}
