using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaGloballyUniqueMutationConverter : ISchemaMutationConverter<SetAttributeSchemaGloballyUniqueMutation, GrpcSetAttributeSchemaGloballyUniqueMutation>
{
    public GrpcSetAttributeSchemaGloballyUniqueMutation Convert(SetAttributeSchemaGloballyUniqueMutation mutation)
    {
        return new GrpcSetAttributeSchemaGloballyUniqueMutation
        {
            Name = mutation.Name,
            UniqueGlobally = mutation.UniqueGlobally
        };
    }

    public SetAttributeSchemaGloballyUniqueMutation Convert(GrpcSetAttributeSchemaGloballyUniqueMutation mutation)
    {
        return new SetAttributeSchemaGloballyUniqueMutation(mutation.Name, mutation.UniqueGlobally);
    }
}