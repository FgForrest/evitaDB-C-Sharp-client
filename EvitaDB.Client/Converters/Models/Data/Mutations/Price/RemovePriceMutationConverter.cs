using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data.Mutations.Price;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Price;

public class RemovePriceMutationConverter : PriceMutationConverter<RemovePriceMutation, GrpcRemovePriceMutation>
{
    public override GrpcRemovePriceMutation Convert(RemovePriceMutation mutation)
    {
        return new GrpcRemovePriceMutation
        {
            PriceId = mutation.PriceKey.PriceId,
            PriceList = mutation.PriceKey.PriceList,
            Currency = EvitaDataTypesConverter.ToGrpcCurrency(mutation.PriceKey.Currency)
        };
    }

    public override RemovePriceMutation Convert(GrpcRemovePriceMutation mutation)
    {
        return new RemovePriceMutation(BuildPriceKey(mutation.PriceId, mutation.PriceList, mutation.Currency));
    }
}