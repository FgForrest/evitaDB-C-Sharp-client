using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaNullableMutationConverter : ISchemaMutationConverter<SetAttributeSchemaNullableMutation, GrpcSetAttributeSchemaNullableMutation>
{
    public GrpcSetAttributeSchemaNullableMutation Convert(SetAttributeSchemaNullableMutation mutation)
    {
        return new GrpcSetAttributeSchemaNullableMutation
        {
            Name = mutation.Name,
            Nullable = mutation.Nullable
        };
    }

    public SetAttributeSchemaNullableMutation Convert(GrpcSetAttributeSchemaNullableMutation mutation)
    {
        return new SetAttributeSchemaNullableMutation(mutation.Name, mutation.Nullable);
    }
}