using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Prices;

public abstract class PriceMutation : ILocalMutation<IPrice>
{
    public PriceKey PriceKey { get; }
    
    protected PriceMutation(PriceKey priceKey)
    {
        PriceKey = priceKey;
    }
    
    public abstract IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue);
}