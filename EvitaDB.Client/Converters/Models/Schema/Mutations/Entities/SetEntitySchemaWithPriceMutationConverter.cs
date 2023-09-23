using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class SetEntitySchemaWithPriceMutationConverter : ISchemaMutationConverter<SetEntitySchemaWithPriceMutation, GrpcSetEntitySchemaWithPriceMutation>
{
    public GrpcSetEntitySchemaWithPriceMutation Convert(SetEntitySchemaWithPriceMutation mutation)
    {
        return new GrpcSetEntitySchemaWithPriceMutation
        {
            WithPrice = mutation.WithPrice,
            IndexedPricePlaces = mutation.IndexedPricePlaces
        };
    }

    public SetEntitySchemaWithPriceMutation Convert(GrpcSetEntitySchemaWithPriceMutation mutation)
    {
        return new SetEntitySchemaWithPriceMutation(mutation.WithPrice, mutation.IndexedPricePlaces);
    }
}