using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaFilterableMutationConverter : ISchemaMutationConverter<SetAttributeSchemaFilterableMutation, GrpcSetAttributeSchemaFilterableMutation>
{
    public GrpcSetAttributeSchemaFilterableMutation Convert(SetAttributeSchemaFilterableMutation mutation)
    {
        return new GrpcSetAttributeSchemaFilterableMutation
        {
            Name = mutation.Name,
            Filterable = mutation.Filterable
        };
    }

    public SetAttributeSchemaFilterableMutation Convert(GrpcSetAttributeSchemaFilterableMutation mutation)
    {
        return new SetAttributeSchemaFilterableMutation(mutation.Name, mutation.Filterable);
    }
}