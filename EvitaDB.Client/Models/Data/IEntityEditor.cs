using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.ExtraResults;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow creating / updating or removing information in <see cref="ISealedEntity"/> instance.
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IEntity"/> interface.
/// </summary>
/// <typeparam name="TW">entity or its data editors type</typeparam>
public interface IEntityEditor<out TW> : IEntity, IAttributesEditor<TW>, IAssociatedDataEditor<TW>, IPricesEditor<TW>
    where TW : IEntityEditor<TW> //TODO: OTHER EDITORS
{
    /// <summary>
    /// Sets hierarchy information of the entity. Hierarchy information allows to compose hierarchy tree composed of
    /// entities of the same type. Referenced entity is always entity of the same type. Referenced entity must be already
    /// present in the evitaDB and must also have hierarchy placement set.
    /// </summary>
    /// <param name="parentPrimaryKey">new parent entity primary key</param>
    /// <returns>self (builder pattern)</returns>
    TW SetParent(int parentPrimaryKey);

    /// <summary>
    /// Removes existing parent of the entity. If there are other entities, that refer transitively via
    /// <see cref="IEntity.Parent"/> this entity their will become "orphans" and their parent needs to be removed
    /// as well, or it must be "rewired" to another parent.
    /// </summary>
    /// <returns>self (builder pattern)</returns>
    TW RemoveParent();

    /// <summary>
    /// <remarks>
    /// <para>
    /// Method creates or updates reference of the entity. Reference represents relation to another Evita entity or may
    /// be also any external source. The exact target entity is defined in <see cref="IReferenceSchema.ReferencedEntityType"/> and
    /// <see cref="IReferenceSchema.ReferencedEntityTypeManaged"/>.
    /// </para>
    /// <para>
    /// This method expects that the <see cref="IReferenceSchema"/> of passed `referenceName` is already present in
    /// the <see cref="IEntitySchema"/>.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedPrimaryKey">primary key of the reference</param>
    /// <exception cref="ReferenceNotKnownException">when reference doesn't exist in entity schema</exception>
    /// <returns>self (builder pattern)</returns>
    TW SetReference(string referenceName, int referencedPrimaryKey);

    /// <summary>
    /// <remarks>
    /// <para>
    /// Method creates or updates reference of the entity. Reference represents relation to another Evita entity or may
    /// be also any external source. The exact target entity is defined in <see cref="IReferenceSchema.ReferencedEntityType"/> and
    /// <see cref="IReferenceSchema.ReferencedEntityTypeManaged"/>. Third argument accepts a delegate function, that allows to set
    /// additional information on the reference such as its <see cref="IReference.GetAttributeValues()"/> or grouping information.
    /// </para>
    /// <para>
    /// This method expects that the <see cref="IReferenceSchema"/> of passed `referenceName` is already present in
    /// the <see cref="IEntitySchema"/>.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedPrimaryKey">primary key of the reference</param>
    /// <param name="whichIs">additional information about the reference</param>
    /// <exception cref="ReferenceNotKnownException">when reference doesn't exist in entity schema</exception>
    /// <returns>self (builder pattern)</returns>
    TW SetReference(
        string referenceName,
        int referencedPrimaryKey,
        Action<IReferenceBuilder>? whichIs
    );

    /// <summary>
    /// <remarks>
    ///	<para>
    /// Method creates or updates reference of the entity. Reference represents relation to another Evita entity or may
    /// be also any external source. The exact target entity is defined in <see cref="IReferenceSchema.ReferencedEntityType"/> and
    /// <see cref="IReferenceSchema.ReferencedEntityTypeManaged"/>.
    /// </para>
    /// <list type="bullet">
    ///		<listheader>
    ///			<description>
    ///				If no <see cref="IReferenceSchema"/> exists yet - new one is created. New reference will have these properties automatically set up:
    ///			</description>
    ///		</listheader>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.Indexed"/> TRUE - you'll be able to filter by presence of this reference (but this setting also consumes more memory)
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.Faceted"/> FALSE - reference data will not be part of the <see cref="FacetSummary"/>
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedEntityTypeManaged"/> TRUE if there already is entity with matching `referencedEntityType` in current catalog, otherwise FALSE
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedGroupType"/> - not defined
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedGroupTypeManaged"/> FALSE
    ///			</description>
    /// 	</item>
    /// </list>
    /// <para>
    /// If you need to change this defaults you need to fetch the reference schema by calling fetching <see cref="IEntitySchema"/>
    /// from the catalog and accessing it by <see cref="IEntitySchema.GetReference(string)"/>, open it for write and update it in
    /// evitaDB instance via <see cref="IEntitySchemaBuilder.UpdateVia(EvitaClientSession)"/>.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedEntityType">entity type of the reference</param>
    /// <param name="cardinality">cardinality of the reference</param>
    /// <param name="referencedPrimaryKey">primary key of the reference</param>
    /// <returns>self (builder pattern)</returns>
    TW SetReference(
        string referenceName,
        string referencedEntityType,
        Cardinality cardinality,
        int referencedPrimaryKey
    );

    /// <summary>
    /// <remarks>
    ///	<para>
    /// Method creates or updates reference of the entity. Reference represents relation to another Evita entity or may
    /// be also any external source. The exact target entity is defined in <see cref="IReferenceSchema.ReferencedEntityType"/> and
    /// <see cref="IReferenceSchema.ReferencedEntityTypeManaged"/>. Third argument accepts consumer, that allows to set additional
    /// information on the reference such as its <see cref="IReference.GetAttributeValues()"/> or grouping information.
    /// </para>
    /// <list type="bullet">
    ///		<listheader>
    ///			<description>
    ///				If no <see cref="IReferenceSchema"/> exists yet - new one is created. New reference will have these properties automatically set up:
    ///			</description>
    ///		</listheader>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.Indexed"/> TRUE - you'll be able to filter by presence of this reference (but this setting also consumes more memory)
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.Faceted"/> FALSE - reference data will not be part of the <see cref="FacetSummary"/>
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedEntityTypeManaged"/> TRUE if there already is entity with matching `referencedEntityType` in current catalog, otherwise FALSE
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedGroupType"/> - as defined in `whichIs` lambda
    ///			</description>
    /// 	</item>
    ///		<item>
    /// 		<description>
    ///				<see cref="IReferenceSchema.ReferencedGroupTypeManaged"/> TRUE if there already is entity with matching
    ///			</description>
    /// 	</item>
    ///     <item>
    /// 		<description>
    ///				<see cref="IReference.Group"/> in current catalog, otherwise FALSE
    ///			</description>
    /// 	</item>
    /// </list>
    /// <para>
    /// If you need to change this defaults you need to fetch the reference schema by calling fetching <see cref="IEntitySchema"/>
    /// from the catalog and accessing it by <see cref="IEntitySchema.GetReference(string)"/>, open it for write and update it in
    /// evitaDB instance via <see cref="IEntitySchemaBuilder.UpdateVia(EvitaClientSession)"/>.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedEntityType">entity type of the reference</param>
    /// <param name="cardinality">cardinality of the reference</param>
    /// <param name="referencedPrimaryKey">primary key of the reference</param>
    /// <param name="whichIs">additional information about the reference</param>
    /// <returns>self (builder pattern)</returns>
    TW SetReference(
        string referenceName,
        string referencedEntityType,
        Cardinality cardinality,
        int referencedPrimaryKey,
        Action<IReferenceBuilder>? whichIs
    );

    /// <summary>
    /// Removes existing reference of specified name and primary key.
    /// </summary>
    /// <param name="referenceName">name of the reference</param>
    /// <param name="referencedPrimaryKey">primary key of the reference</param>
    /// <exception cref="ReferenceNotKnownException">when reference doesn't exist in entity schema</exception>
    /// <returns>self (builder pattern)</returns>
    TW RemoveReference(string referenceName, int referencedPrimaryKey);
}