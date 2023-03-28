using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations.Catalog;

namespace Client.Models.Schemas;

public interface IEntitySchemaBuilder : IEntitySchemaEditor<IEntitySchemaBuilder>
{
        ModifyEntitySchemaMutation? ToMutation();

    /**
		 * Returns built "local up-to-date" {@link Entity} instance that may not represent globally "up-to-date" state
		 * because it is based on the version of the entity known when builder was created.
		 *
		 * This method is particularly useful for tests.
		 */
        EntitySchema ToInstance();

        /**
         * The method is a shortcut for calling {@link EvitaSessionContract#updateEntitySchema(ModifyEntitySchemaMutation)}
         * the other way around. Method simplifies the statements, makes them more readable and in combination with
         * builder pattern usage it's also easier to use.
         *
         * @param session to use for updating the modified (built) schema
         */
        int UpdateVia(EvitaClientSession session) {
        return session.UpdateEntitySchema(this);
    }

    /**
		 * The method is a shortcut for calling {@link EvitaSessionContract#updateAndFetchEntitySchema(ModifyEntitySchemaMutation)}
		 * the other way around. Method simplifies the statements, makes them more readable and in combination with
		 * builder pattern usage it's also easier to use.
		 *
		 * @param session to use for updating the modified (built) schema
		 */
    EntitySchema UpdateAndFetchVia(EvitaClientSession session) {
        //return session.UpdateAndFetchEntitySchema(this);
        return null;
    }
}