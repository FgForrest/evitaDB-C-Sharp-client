using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Sealed catalog schema is read only form of the schema that contains seal-breaking actions such as opening its
/// contents to write actions using <see cref="ICatalogSchemaBuilder"/> or accepting mutations that create
/// <see cref="ICatalogSchemaMutation"/> objects. All seal breaking actions don't modify <see cref="ISealedCatalogSchema"/> contents,
/// and only create new objects based on it. This keeps this class immutable and thread safe.
/// </summary>
public interface ISealedCatalogSchema : ICatalogSchema
{
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="ICatalogSchemaBuilder"/> that allows modification of the entity internals,
    /// and fabricates new immutable copy of the entity with altered data. Returned <see cref="ICatalogSchemaBuilder"/> is
    /// NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="ICatalogSchemaBuilder"/> doesn't alter contents of <see cref="ISealedCatalogSchema"/> but allows to create new
    /// version based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    ICatalogSchemaBuilder OpenForWrite();
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="ICatalogSchemaBuilder"/> and incorporates the passed array of `schemaMutations`
    /// in the returned <see cref="ICatalogSchemaBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned <see cref="ICatalogSchemaBuilder"/> is
    /// NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="ICatalogSchemaBuilder"/> doesn't alter contents of <see cref="ISealedCatalogSchema"/> but allows to create new
    /// version based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    ICatalogSchemaBuilder OpenForWriteWithMutations(params ILocalCatalogSchemaMutation[] schemaMutations);
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="ICatalogSchemaBuilder"/> and incorporates the passed collection of `schemaMutations`
    /// in the returned <see cref="ICatalogSchemaBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned <see cref="ICatalogSchemaBuilder"/> is
    /// NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="ICatalogSchemaBuilder"/> doesn't alter contents of <see cref="ISealedCatalogSchema"/> but allows to create new
    /// version based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    ICatalogSchemaBuilder OpenForWriteWithMutations(ICollection<ILocalCatalogSchemaMutation> schemaMutations);
}