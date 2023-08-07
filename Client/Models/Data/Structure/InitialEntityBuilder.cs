using System.Globalization;
using Client.DataTypes;
using Client.Models.Data.Mutations;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using Client.Queries.Requires;

namespace Client.Models.Data.Structure;

public class InitialEntityBuilder : IEntityBuilder
{
    public string EntityType { get; }
    public int? PrimaryKey { get; }
    public int Version { get; }
    public IEntitySchema Schema { get; }
    public int? Parent { get; }
    public IAttributeBuilder AttributesBuilder { get; }
    public IAssociatedDataBuilder AssociatedDataBuilder;
    public IPricesBuilder PricesBuilder;
    public Dictionary<ReferenceKey, Reference> References { get; } = new ();
    public PriceInnerRecordHandling InnerRecordHandling => PriceInnerRecordHandling.None;
    public bool PricesAvailable => Schema.WithPrice; //TODO: fix when possible
    public bool AssociatedDataAvailable => true; //TODO: fix when possible
    public bool AttributesAvailable => AttributesBuilder.AttributesAvailable;
    public bool ReferencesAvailable => true; 
    public bool ParentAvailable => true;

    public InitialEntityBuilder(
		EntitySchema? entitySchema,
		int? primaryKey,
		ICollection<AttributeValue> attributeValues,
		ICollection<AssociatedDataValue> associatedDataValues,
		ICollection<Reference> referenceContracts,
		PriceInnerRecordHandling? priceInnerRecordHandling,
		ICollection<Price> prices
	) {
		EntityType = entitySchema.Name;
		Schema = entitySchema;
		PrimaryKey = primaryKey;
		AttributesBuilder = new InitialAttributesBuilder(entitySchema);
		foreach (AttributeValue attributeValue in attributeValues) {
			AttributeKey attributeKey = attributeValue.Key;
			if (attributeKey.Localized) {
				AttributesBuilder.SetAttribute(
					attributeKey.AttributeName,
					attributeKey.Locale,
					attributeValue.Value
				);
			} else {
				AttributesBuilder.SetAttribute(
					attributeKey.AttributeName,
					attributeValue.Value
				);
			}
		}
		/*this.associatedDataBuilder = new InitialAssociatedDataBuilder(schema);
		for (AssociatedDataValue associatedDataValue : associatedDataValues) {
			final AssociatedDataKey associatedDataKey = associatedDataValue.getKey();
			if (associatedDataKey.isLocalized()) {
				this.associatedDataBuilder.setAssociatedData(
					associatedDataKey.getAssociatedDataName(),
					associatedDataKey.getLocale(),
					associatedDataValue.getValue()
				);
			} else {
				this.associatedDataBuilder.setAssociatedData(
					associatedDataKey.getAssociatedDataName(),
					associatedDataValue.getValue()
				);
			}
		}
		this.pricesBuilder = new InitialPricesBuilder();
		ofNullable(priceInnerRecordHandling)
			.ifPresent(this.pricesBuilder::setPriceInnerRecordHandling);
		for (PriceContract price : prices) {
			this.pricesBuilder.setPrice(
				price.getPriceId(),
				price.getPriceList(),
				price.getCurrency(),
				price.getInnerRecordId(),
				price.getPriceWithoutTax(),
				price.getTaxRate(),
				price.getPriceWithTax(),
				price.getValidity(),
				price.isSellable()
			);
		}

		this.references = referenceContracts.stream()
			.collect(
				Collectors.toMap(
					ReferenceContract::getReferenceKey,
					Function.identity()
				)
			);*/
        
	}
    
    public object? GetAttribute(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        throw new NotImplementedException();
    }

    public IAttributeSchema GetAttributeSchema(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<string> GetAttributeNames()
    {
        throw new NotImplementedException();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue?> GetAttributeValues()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        throw new NotImplementedException();
    }

    public object? GetAssociatedData(string associatedDataName)
    {
        throw new NotImplementedException();
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        throw new NotImplementedException();
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        throw new NotImplementedException();
    }

    public ISet<string> GetAssociatedDataNames()
    {
        throw new NotImplementedException();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        throw new NotImplementedException();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        throw new NotImplementedException();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        throw new NotImplementedException();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        throw new NotImplementedException();
    }

    public IPrice GetPrice(PriceKey priceKey)
    {
        throw new NotImplementedException();
    }

    public IPrice? GetPrice(int priceId, string priceList, Currency currency)
    {
        throw new NotImplementedException();
    }

    public IPrice? GetPriceForSale()
    {
        throw new NotImplementedException();
    }

    public List<IPrice> GetAllPricesForSale()
    {
        throw new NotImplementedException();
    }

    public bool HasPriceInInterval(decimal from, decimal to, QueryPriceMode queryPriceMode)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<IPrice> GetPrices()
    {
        throw new NotImplementedException();
    }

    
    public ICollection<IReference> GetReferences()
    {
        throw new NotImplementedException();
    }

    public IReference? GetReference(string referenceName, int referencedEntityId)
    {
        throw new NotImplementedException();
    }

    public IReference? GetReference(ReferenceKey referenceKey)
    {
        throw new NotImplementedException();
    }

    public ISet<CultureInfo> GetAllLocales()
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder RemoveAttribute(string attributeName)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder MutateAttribute(AttributeMutation mutation)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetParent(int parentPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder RemoveParent()
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetReference(string referenceName, int referencedPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder SetReference(string referenceName, string referencedEntityType, Cardinality cardinality,
        int referencedPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IEntityBuilder RemoveReference(string referenceName, int referencedPrimaryKey)
    {
        throw new NotImplementedException();
    }

    public IEntityMutation? ToMutation()
    {
        throw new NotImplementedException();
    }

    public SealedEntity ToInstance()
    {
        throw new NotImplementedException();
    }

    public bool Dropped => false;
    public IEntityClassifierWithParent? ParentEntity => null;
}