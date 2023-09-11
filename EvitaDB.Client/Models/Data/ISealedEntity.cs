using EvitaDB.Client.Models.Data.Mutations;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Sealed entity is read only form of entity that contains seal-breaking actions such as opening its contents to write
/// actions using <see cref="IEntityBuilder"/> or accepting mutations that create <see cref="IEntityMutation"/> objects. All seal
/// breaking actions don't modify <see cref="ISealedEntity"/> contents and only create new objects based on it. This keeps this
/// class immutable and thread safe.
/// </summary>
public interface ISealedEntity : IEntity
{
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntityBuilder"/> that allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned instance is NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntityBuilder"/> doesn't alter contents of <see cref="ISealedEntity"/> but allows to create new version based on
    /// the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    IEntityBuilder OpenForWrite();
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntityBuilder"/> and incorporates the passed array of `localMutations`
    /// in the returned <see cref="IEntityBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned instance is NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntityBuilder"/> doesn't alter contents of <see cref="ISealedEntity"/> but allows to create new version based on
    /// the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="mutations">array of mutations from which an entity builder is to be created</param>
    /// <returns>instance of created builder that allows its modification</returns>
    IEntityBuilder WithMutations(params ILocalMutation[] mutations);
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// Opens entity for update - returns <see cref="IEntityBuilder"/> and incorporates the passed collection of `localMutations`
    /// in the returned <see cref="IEntityBuilder"/> right away. The builder allows modification of the entity internals and
    /// fabricates new immutable copy of the entity with altered data. Returned instance is NOT THREAD SAFE.
    /// </para>
    /// <para>
    /// <see cref="IEntityBuilder"/> doesn't alter contents of <see cref="ISealedEntity"/> but allows to create new version based on
    /// the version that is represented by this sealed entity.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="mutations">collection of mutations from which an entity builder is to be created</param>

    /// <returns>instance of created builder that allows its modification</returns>
    IEntityBuilder WithMutations(ICollection<ILocalMutation> mutations);
}