using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// <remarks>
/// <para>
/// Interface that simply combines <see cref="IEntitySchemaEditor"/> and <see cref="IEntitySchema"/> entity contracts together.
/// Builder produces either <see cref="IEntityMutation"/> that describes all changes to be made on <see cref="IEntity"/> instance
/// to get it to "up-to-date" state or can provide already built <see cref="IEntity"/> that may not represent globally
/// "up-to-date" state because it is based on the version of the entity known when builder was created.
/// </para>
/// <para>
/// Mutation allows Evita to perform surgical updates on the latest version of the <see cref="IEntity"/> object that
/// is in the database at the time update request arrives.
/// </para>
/// </remarks>
/// </summary>
public interface IEntityBuilder : IEntityEditor<IEntityBuilder>
{
    /// <summary>
    /// Returns object that contains set of <see cref="ILocalMutation"/> instances describing
    /// what changes occurred in the builder and which should be applied on the existing <see cref="IEntity"/> version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race conditions
    /// based on "optimistic locking" mechanism in very granular way.
    /// </summary>
    /// <returns></returns>
    IEntityMutation? ToMutation();

    /// <summary>
    /// <remarks>
    /// <para>
    /// Returns built "local up-to-date" <see cref="IEntity"/> instance that may not represent globally "up-to-date"
    /// state because it is based on the version of the entity known when builder was created.
    /// </para>
    /// <para>
    /// Mutation allows Evita to perform surgical updates on the latest version of the <see cref="IEntity"/> object 
    /// which is in the database at the time update request arrives.
    /// </para>
    /// <para>
    /// This method is particularly useful for tests.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns></returns>
    Entity ToInstance();

    /// <summary>
    /// The method is a shortcut for calling <see cref="EvitaClientSession.UpsertEntity(IEntityBuilder)"/> the other
    /// way around. Method simplifies the statements, makes them more readable and in combination with builder
    /// pattern usage it's also easier to use.
    /// </summary>
    /// <param name="session">to use for upserting the modified (built) entity</param>
    /// <returns>the reference to the updated / created entity</returns>
    EntityReference UpsertVia(EvitaClientSession session)
    {
        return session.UpsertEntity(this);
    }
}