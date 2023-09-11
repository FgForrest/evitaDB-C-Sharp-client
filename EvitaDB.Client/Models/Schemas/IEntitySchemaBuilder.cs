using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas;

public interface IEntitySchemaBuilder : IEntitySchemaEditor<IEntitySchemaBuilder>
{
    /// <summary>
    /// <remarks>
    /// <para>
    /// Returns <see cref="ModifyEntitySchemaMutation"/> instance that contains array of <see cref="IEntitySchemaMutation"/>
    /// describing what changes occurred in the builder, and which should be applied on the existing <see cref="IEntitySchema"/> version.
    /// </para>
    /// <para>
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    ModifyEntitySchemaMutation? ToMutation();

    /// <summary>
    /// <remarks>
    /// <para>
    /// Returns built "local up-to-date" <see cref="Entity"/> instance that may not represent globally "up-to-date" state
    /// because it is based on the version of the entity known when builder was created.
    /// </para>
    /// <para>
    /// This method is particularly useful for tests.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>instance of created builder that allows its modification</returns>
    EntitySchema ToInstance();

    /// <summary>
    /// The method is a shortcut for calling <see cref="EvitaClientSession.UpdateEntitySchema(ModifyEntitySchemaMutation)"/>
    /// the other way around. Method simplifies the statements, makes them more readable and in combination with
    /// builder pattern usage it's also easier to use.
    /// </summary>
    /// <param name="session">to use for updating the modified (built) schema</param>
    /// <returns>instance of created builder that allows its modification</returns>
    int UpdateVia(EvitaClientSession session)
    {
        return session.UpdateEntitySchema(this);
    }

    /// <summary>
    /// The method is a shortcut for calling <see cref="EvitaClientSession.UpdateEntitySchema(ModifyEntitySchemaMutation)"/>
    /// the other way around. Method simplifies the statements, makes them more readable and in combination with
    /// builder pattern usage it's also easier to use.
    /// </summary>
    /// <param name="session">to use for updating the modified (built) schema</param>
    /// <returns>instance of created builder that allows its modification</returns>
    ISealedEntitySchema UpdateAndFetchVia(EvitaClientSession session)
    {
        return session.UpdateAndFetchEntitySchema(this);
    }
}