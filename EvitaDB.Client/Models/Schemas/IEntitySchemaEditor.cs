using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

public interface IEntitySchemaEditor<out TS> : IEntitySchema, INamedSchemaWithDeprecationEditor<TS>,
	IAttributeProviderSchemaEditor<TS, IAttributeSchema, IAttributeSchemaBuilder>
	where TS : IEntitySchemaEditor<TS>
{
	TS CooperatingWith(Func<CatalogSchema> catalogSupplier);
	
	TS VerifySchemaStrictly();
	
	TS VerifySchemaButAllow(params EvolutionMode[] evolutionMode);
	
	TS VerifySchemaButCreateOnTheFly();
	
	new TS WithGeneratedPrimaryKey();
	
	TS WithoutGeneratedPrimaryKey();
	
	new TS WithHierarchy();
	
	TS WithoutHierarchy();
	
	new TS WithPrice();
	
	TS WithPrice(int indexedDecimalPlaces);
	
	TS WithPriceInCurrency(params Currency[] currency);
	
	TS WithPriceInCurrency(int indexedPricePlaces, params Currency[] currency);
	
	TS WithoutPrice();
	
	TS WithoutPriceInCurrency(Currency currency);
	
	TS WithLocale(params CultureInfo[] locale);
	
	TS WithoutLocale(CultureInfo locale);
	
	TS WithGlobalAttribute(string attributeName);

	TS WithAssociatedData<T>(string dataName);
	
	TS WithAssociatedData<T>(string dataName, Action<IAssociatedDataSchemaEditor>? whichIs);
	
	TS WithoutAssociatedData(string dataName);
	
	TS WithReferenceTo(string name, string externalEntityType, Cardinality cardinality);
	
	TS WithReferenceTo(
		string name,
		string externalEntityType,
		Cardinality cardinality,
		Action<IReferenceSchemaBuilder>? whichIs
	);
	
	TS WithReferenceToEntity(string name, string entityType, Cardinality cardinality);
	
	TS WithReferenceToEntity(string name, string entityType, Cardinality cardinality, Action<IReferenceSchemaBuilder> whichIs);
		
	TS WithoutReferenceTo(string name);
}