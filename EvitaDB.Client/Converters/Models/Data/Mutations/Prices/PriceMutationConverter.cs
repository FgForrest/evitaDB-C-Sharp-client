using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Prices;

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