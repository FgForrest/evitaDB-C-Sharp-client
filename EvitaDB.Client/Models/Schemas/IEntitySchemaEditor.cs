using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas;

public interface IEntitySchemaEditor<out TS> : IEntitySchema, INamedSchemaWithDeprecationEditor<TS>,
	IAttributeProviderSchemaEditor<TS, IEntityAttributeSchema, IEntityAttributeSchemaBuilder>,
	ISortableAttributeCompoundSchemaProviderEditor<TS, IEntityAttributeSchema>
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

	/// <summary>
	/// Declares a reference whose contents the <em>server</em> derives from a reference on another entity.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A category does not store its products; the product stores its category, and the category exposes the
	/// reflection of that. Reflected references are read-only by nature - the way to change one is to change the
	/// reference it reflects - so this declares the schema and nothing writes the data.
	/// </para>
	/// <para>
	/// Counterpart of the Java driver's <c>withReflectedReferenceToEntity</c>. This overload takes the options
	/// directly rather than a configurator, because they are exactly the options a reflected reference has: the
	/// rest of a reference schema (indexing, its own attributes) either comes from the reflected side or does
	/// not apply.
	/// </para>
	/// </remarks>
	/// <param name="name">the reflected reference's name on this entity</param>
	/// <param name="entityType">the entity declaring the reference being reflected</param>
	/// <param name="reflectedReferenceName">the name of that reference on <paramref name="entityType"/></param>
	/// <param name="cardinality">how many, or null to take the reflected side's</param>
	/// <param name="faceted">whether the reflected reference is faceted</param>
	/// <param name="attributeInheritance">how the reflected side's reference attributes are inherited</param>
	/// <param name="attributeInheritanceFilter">which attributes that behaviour applies to</param>
	TS WithReflectedReferenceToEntity(
		string name,
		string entityType,
		string reflectedReferenceName,
		Cardinality? cardinality = null,
		bool? faceted = null,
		AttributeInheritanceBehavior attributeInheritance = AttributeInheritanceBehavior.InheritAllExcept,
		string[]? attributeInheritanceFilter = null);
		
	TS WithoutReferenceTo(string name);
}
