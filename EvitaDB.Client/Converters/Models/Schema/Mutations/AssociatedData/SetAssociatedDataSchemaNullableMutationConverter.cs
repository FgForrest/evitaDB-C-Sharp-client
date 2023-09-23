using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class SetAssociatedDataSchemaNullableMutationConverter : ISchemaMutationConverter<SetAssociatedDataSchemaNullableMutation, GrpcSetAssociatedDataSchemaNullableMutation>
{
    public GrpcSetAssociatedDataSchemaNullableMutation Convert(SetAssociatedDataSchemaNullableMutation mutation)
    {
        return new GrpcSetAssociatedDataSchemaNullableMutation
        {
            Name = mutation.Name,
            Nullable = mutation.Nullable
        };
    }

    public SetAssociatedDataSchemaNullableMutation Convert(GrpcSetAssociatedDataSchemaNullableMutation mutation)
    {
        return new SetAssociatedDataSchemaNullableMutation(mutation.Name, mutation.Nullable);
    }
}