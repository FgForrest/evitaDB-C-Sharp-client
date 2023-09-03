using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

public interface IEntitySchemaEditor<out TS> : IEntitySchema where TS : IEntitySchemaBuilder
{
	TS CooperatingWith(Func<CatalogSchema> catalogSupplier);
	
	TS VerifySchemaStrictly();
	
	TS VerifySchemaButAllow(params EvolutionMode[] evolutionMode);
	
	TS VerifySchemaButCreateOnTheFly();

	TS WithDescription(string? description);
	
	TS Deprecated(string deprecationNotice);
	
	TS NotDeprecatedAnymore();
	
	TS WithGeneratedPrimaryKey();
	
	TS WithoutGeneratedPrimaryKey();
	
	TS WithHierarchy();
	
	TS WithoutHierarchy();
	
	TS WithAttribute(string attributeName, Type ofType);

	//TS WithAttribute(string attributeName, Type ofType, Action<AttributeSchemaEditor> whichIs);

	TS WithoutAttribute(string attributeName);
	
	TS WithPrice();
	
	TS WithPrice(int indexedDecimalPlaces);
	
	TS WithPriceInCurrency(params Currency[] currency);
	
	TS WithPriceInCurrency(int indexedPricePlaces, params Currency[] currency);
	
	TS WithoutPrice();
	
	TS WithoutPriceInCurrency(Currency currency);
	
	TS WithLocale(params CultureInfo[] locale);
	
	TS WithoutLocale(CultureInfo locale);
	
	TS WithGlobalAttribute(string attributeName);

	TS WithAssociatedData(string dataName, Type ofType);
	
	//TS WithAssociatedData(string dataName, Type ofType, Action<AssociatedDataSchemaEditor>? whichIs);
	
	TS WithoutAssociatedData(string dataName);
	
	TS WithReferenceTo(string name, string externalEntityType, Cardinality cardinality);
	
	/*TS WithReferenceTo(
		string name,
		string externalEntityType,
		Cardinality cardinality,
		Action<ReferenceSchemaEditor.ReferenceSchemaBuilder>? whichIs
	);*/
	
	TS WithReferenceToEntity(string name, string entityType, Cardinality cardinality);
	
	//TS WithReferenceToEntity(string name, string entityType, Cardinality cardinality, Action<ReferenceSchemaEditor.ReferenceSchemaBuilder> whichIs);
		
	TS WithoutReferenceTo(string name);
}