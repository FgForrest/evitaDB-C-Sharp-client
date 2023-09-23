using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Queries.Order;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="ISortableAttributeCompoundSchema"/> interface.
/// </summary>
public interface ISortableAttributeCompoundSchemaProviderEditor<T> : ISortableAttributeCompoundSchemaProvider 
    where T : ISortableAttributeCompoundSchemaProviderEditor<T>
{
    /// <summary>
    /// <remarks>
    ///	<para>
    /// Adds new <see cref="ISortableAttributeCompoundSchema"/> to the entity or reference.
    /// Method cannot be used for updating attribute elements of existing <see cref="ISortableAttributeCompoundSchema"/>
    /// with particular name, you need to remove it first and then add it again.
    /// </para>
    /// <para>
    /// Sortable attribute compounds are used to sort entities or references by multiple attributes at once. evitaDB
    /// requires a pre-sorted index in order to be able to sort entities or references by particular attribute or
    /// combination of attributes, so it can deliver the results as fast as possible. Sortable attribute compounds
    /// are filtered the same way as <see cref="IAttributeSchema"/> attributes - using <see cref="AttributeNatural"/> ordering
    /// </para>
    /// <para>
    /// The referenced attributes must be already defined in the entity or reference schema.
    /// </para>
    /// </remarks>
    /// </summary>
	T WithSortableAttributeCompound(
		string name,
		params AttributeElement[] attributeElements
	);

	/// <summary>
	/// <remarks>
	/// <para>
	/// Adds new <see cref="ISortableAttributeCompoundSchema"/> to the entity or reference or updates existing.
	/// Method cannot be used for updating attribute elements of existing <see cref="ISortableAttributeCompoundSchema"/>
	/// with particular name, you need to remove it first and then add it again.
	/// </para>
	/// <para>
	/// If you update existing sortable attribute compound all data must be specified again, nothing is preserved.
	/// </para>
	/// <para>
	///	Sortable attribute compounds are used to sort entities or references by multiple attributes at once. evitaDB
	///	requires a pre-sorted index in order to be able to sort entities or references by particular attribute or
	///	combination of attributes, so it can deliver the results as fast as possible. Sortable attribute compounds
	///	are filtered the same way as <see cref="IAttributeSchema"/> attributes - using <see cref="AttributeNatural"/> ordering
	///	constraint.
	/// </para>
	/// <para>
	///	The referenced attributes must be already defined in the entity or reference schema.
	/// </para>
	/// </remarks>
	/// </summary>
	/// <param name="name"></param>
	/// <param name="attributeElements"></param>
	/// <param name="whichIs"></param>
	/// <returns></returns>
	T WithSortableAttributeCompound(
		string name,
		AttributeElement[] attributeElements,
		Action<SortableAttributeCompoundSchemaBuilder>? whichIs
	);

	/// <summary>
	/// Removes specific {@link SortableAttributeCompoundSchemaContract} from the entity or reference schema.
	/// </summary>
	T WithoutSortableAttributeCompound(string name);
}