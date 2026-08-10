using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Prices;

public abstract class PriceMutation : ILocalMutation<IPrice>
{
    public PriceKey PriceKey { get; }
    public abstract Operation Operation { get; }
    
    protected PriceMutation(PriceKey priceKey)
    {
        PriceKey = priceKey;
    }
    
    public abstract IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue);
}
