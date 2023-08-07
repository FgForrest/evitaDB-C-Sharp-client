using Client.Converters.DataTypes;
using Client.Exceptions;
using Client.Models.Data;
using Client.Models.Data.Mutations.Price;
using EvitaDB;
using Google.Protobuf;

namespace Client.Converters.Models.Data.Mutations.Price;

public abstract class PriceMutationConverter<TJ, TG> : ILocalMutationConverter<TJ, TG> where TJ : PriceMutation where TG : IMessage
{
    protected static PriceKey BuildPriceKey(int priceId, string priceList, GrpcCurrency currency) {
        if (!currency.IsInitialized()) {
            throw new EvitaInvalidUsageException("Currency is required!");
        }
        return new PriceKey(priceId, priceList, EvitaDataTypesConverter.ToCurrency(currency));
    }

    public abstract TG Convert(TJ mutation);

    public abstract TJ Convert(TG mutation);
}