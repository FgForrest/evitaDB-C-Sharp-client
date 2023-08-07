using Client.Models.Schemas;

namespace Client.Models.Data.Mutations.Price;

public abstract class PriceMutation : ILocalMutation<IPrice>
{
    public PriceKey PriceKey { get; }
    
    protected PriceMutation(PriceKey priceKey)
    {
        PriceKey = priceKey;
    }
    
    public abstract IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue);
}