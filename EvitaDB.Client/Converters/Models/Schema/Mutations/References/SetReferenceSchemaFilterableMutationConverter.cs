using Client.Models.Schemas.Mutations.References;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.References;

public class SetReferenceSchemaFilterableMutationConverter : ISchemaMutationConverter<SetReferenceSchemaIndexedMutation, GrpcSetReferenceSchemaFilterableMutation>
{
    public GrpcSetReferenceSchemaFilterableMutation Convert(SetReferenceSchemaIndexedMutation mutation)
    {
        return new GrpcSetReferenceSchemaFilterableMutation
        {
            Name = mutation.Name,
            Filterable = mutation.Indexed
        };
    }

    public SetReferenceSchemaIndexedMutation Convert(GrpcSetReferenceSchemaFilterableMutation mutation)
    {
        return new SetReferenceSchemaIndexedMutation(mutation.Name, mutation.Filterable);
    }
}