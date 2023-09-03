using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Price;

public class SetPriceInnerRecordHandlingMutation : ILocalMutation<IPrices>
{
    public PriceInnerRecordHandling PriceInnerRecordHandling { get; }
    
    public SetPriceInnerRecordHandlingMutation(PriceInnerRecordHandling priceInnerRecordHandling)
    {
        PriceInnerRecordHandling = priceInnerRecordHandling;
    }
    
    public IPrices MutateLocal(IEntitySchema entitySchema, IPrices? existingValue)
    {
        if (existingValue == null) {
            return new Prices(entitySchema, PriceInnerRecordHandling);
        } if (existingValue.InnerRecordHandling != PriceInnerRecordHandling) {
            return new Prices(
                entitySchema,
                existingValue.Version + 1,
                existingValue.GetPrices().ToList(),
                PriceInnerRecordHandling
            );
        } 
        return existingValue;
    }
}