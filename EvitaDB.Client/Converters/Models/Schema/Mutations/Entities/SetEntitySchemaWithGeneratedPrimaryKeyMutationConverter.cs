using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class SetEntitySchemaWithGeneratedPrimaryKeyMutationConverter : ISchemaMutationConverter<SetEntitySchemaWithGeneratedPrimaryKeyMutation, GrpcSetEntitySchemaWithGeneratedPrimaryKeyMutation>
{
    public GrpcSetEntitySchemaWithGeneratedPrimaryKeyMutation Convert(SetEntitySchemaWithGeneratedPrimaryKeyMutation mutation)
    {
        return new GrpcSetEntitySchemaWithGeneratedPrimaryKeyMutation
        {
            WithGeneratedPrimaryKey = mutation.WithGeneratedPrimaryKey
        };
    }

    public SetEntitySchemaWithGeneratedPrimaryKeyMutation Convert(GrpcSetEntitySchemaWithGeneratedPrimaryKeyMutation mutation)
    {
        return new SetEntitySchemaWithGeneratedPrimaryKeyMutation(mutation.WithGeneratedPrimaryKey);
    }
}