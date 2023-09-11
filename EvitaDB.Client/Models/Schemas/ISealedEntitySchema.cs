using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Sealed catalog schema is read only form of the schema that contains seal-breaking actions such as opening its
/// contents to write actions using <see cref="IEntitySchemaBuilder"/> or accepting mutations that create
/// <see cref="IEntitySchemaMutation"/> objects. All seal breaking actions don't modify <see cref="ISealedEntitySchema"/> contents,
/// and only create new objects based on it. This keeps this class immutable and thread safe.
/// </summary>
public interface ISealedEntitySchema : IEntitySchema
{
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntitySchemaBuilder"/> that allows modification of the entity internals,
    /// and fabricates new immutable copy of the entity with altered data. Returned <see cref="IEntitySchemaBuilder"/> is
    /// NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntitySchemaBuilder"/> doesn't alter contents of <see cref="ISealedEntitySchema"/> but allows to create new version
    /// based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    IEntitySchemaBuilder OpenForWrite();
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntitySchemaBuilder"/> and incorporates the passed array of `schemaMutations`
    /// in the returned <see cref="IEntitySchemaBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned <see cref="IEntitySchemaBuilder"/> is NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntitySchemaBuilder"/> doesn't alter contents of <see cref="ISealedEntitySchema"/> but allows to create new version
    /// based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="schemaMutations">array of mutations from which an entity schema builder is to be created</param>
    /// <returns>instance of created builder that allows its modification</returns>
    IEntitySchemaBuilder WithMutations(params IEntitySchemaMutation[] schemaMutations);
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntitySchemaBuilder"/> and incorporates the passed collection of `schemaMutations`
    /// in the returned <see cref="IEntitySchemaBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned <see cref="IEntitySchemaBuilder"/> is NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntitySchemaBuilder"/> doesn't alter contents of <see cref="ISealedEntitySchema"/> but allows to create new version
    /// based on the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="schemaMutations">collection of mutations from which an entity schema builder is to be created</param>
    /// <returns>instance of created builder that allows its modification</returns>
    IEntitySchemaBuilder WithMutations(ICollection<IEntitySchemaMutation> schemaMutations);
}