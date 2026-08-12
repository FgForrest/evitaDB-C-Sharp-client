using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class SetEntitySchemaWithPriceMutationConverter : ISchemaMutationConverter<SetEntitySchemaWithPriceMutation, GrpcSetEntitySchemaWithPriceMutation>
{
    public GrpcSetEntitySchemaWithPriceMutation Convert(SetEntitySchemaWithPriceMutation mutation)
    {
        GrpcSetEntitySchemaWithPriceMutation grpcMutation = new()
        {
            WithPrice = mutation.WithPrice,
            IndexedPricePlaces = mutation.IndexedPricePlaces
        };
        if (mutation.WithPrice)
        {
            // the server requires at least one indexing scope when prices are enabled; until the C# model
            // supports entity scopes explicitly, prices are indexed in the live scope
            grpcMutation.IndexedInScopes.Add(GrpcEntityScope.ScopeLive);
        }
        return grpcMutation;
    }

    public SetEntitySchemaWithPriceMutation Convert(GrpcSetEntitySchemaWithPriceMutation mutation)
    {
        return new SetEntitySchemaWithPriceMutation(mutation.WithPrice, mutation.IndexedPricePlaces);
    }
}