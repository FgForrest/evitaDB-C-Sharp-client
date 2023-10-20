using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Models.Schemas;

public interface IReferenceSchemaEditor<TS> : IReferenceSchema, INamedSchemaWithDeprecationEditor<TS>, 
    IAttributeProviderSchemaEditor<TS, IAttributeSchema, IAttributeSchemaBuilder>, ISortableAttributeCompoundSchemaProviderEditor<TS, IAttributeSchema>
    where TS : IReferenceSchemaEditor<TS>
{
    /// <summary>
    /// Specifies that reference of this type will be related to external entity not maintained in Evita.
    /// </summary>
    /// <param name="groupType">entity type of the reference group</param>
	TS WithGroupType(string groupType);

	/// <summary>
	/// Specifies that reference of this type will be related to another entity maintained in Evita <see cref="Entity.Type"/>.
	/// </summary>
	/// <param name="groupType">entity type of the reference group</param>
	/// <returns>builder to continue with configuration</returns>
	TS WithGroupTypeRelatedToEntity(string groupType);

	/// <summary>
	/// Specifies that this reference will not be grouped to a specific groups. This is default setting for the reference.
	/// </summary>
	/// <returns>builder to continue with configuration</returns>
	TS WithoutGroupType();
	
	/// <summary>
	/// <remarks>
	///	<para>
	///	Contains TRUE if evitaDB should create and maintain searchable index for this reference allowing to filter by
	///	<see cref="ReferenceHaving"/> filtering constraints. Index is also required when reference is <see cref="Faceted()"/>.
	/// </para>
	/// <para>
	/// Do not mark reference as indexed unless you know that you'll need to filter / sort entities by this reference.
	/// Each indexed reference occupies (memory/disk) space in the form of index. When reference is not indexed,
	/// the entity cannot be looked up by reference attributes or relation existence itself, but the data is loaded
	/// alongside other references and is available by calling <see cref="ISealedEntity.GetReferences()"/> method.
	/// </para>
	/// </remarks>
	/// </summary>
	/// <returns>builder to continue with configuration</returns>
	new TS Indexed();

	/// <summary>
	/// Makes reference as non-faceted. This means reference information will be available on entity when loaded but
	/// cannot be used in filtering.
	/// </summary>
	/// <returns>builder to continue with configuration</returns>
	TS NonIndexed();

	/// <summary>
	/// Makes reference faceted. That means that statistics data for this reference should be maintained and this
	/// allowing to get <see cref="FacetStatistics"/> for this reference or use <see cref="FacetHaving"/> filtering query. When
	/// reference is faceted it is also automatically made <see cref="Indexed()"/> as well.
	/// 
	/// Do not mark reference as faceted unless you know that you'll need to filter entities by this reference. Each
	/// indexed reference occupies (memory/disk) space in the form of index.
	/// </summary>
	/// <returns>builder to continue with configuration</returns>
	new TS Faceted();

	/// <summary>
	/// Makes reference as non-faceted. This means reference information will be available on entity when loaded but
	/// cannot be part of the computed facet statistics and filtering by facet query.
	/// </summary>
	/// <returns>builder to continue with configuration</returns>
	TS NonFaceted();
}