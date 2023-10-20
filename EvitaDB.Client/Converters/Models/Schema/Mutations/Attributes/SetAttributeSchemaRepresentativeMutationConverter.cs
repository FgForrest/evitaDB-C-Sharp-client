using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaRepresentativeMutationConverter : ISchemaMutationConverter<SetAttributeSchemaRepresentativeMutation, GrpcSetAttributeSchemaRepresentativeMutation>
{
    public GrpcSetAttributeSchemaRepresentativeMutation Convert(SetAttributeSchemaRepresentativeMutation mutation)
    {
        return new GrpcSetAttributeSchemaRepresentativeMutation
        {
            Name = mutation.Name,
            Representative = mutation.Representative
        };
    }

    public SetAttributeSchemaRepresentativeMutation Convert(GrpcSetAttributeSchemaRepresentativeMutation mutation)
    {
        return new SetAttributeSchemaRepresentativeMutation(
            mutation.Name, mutation.Representative
        );
    }
}