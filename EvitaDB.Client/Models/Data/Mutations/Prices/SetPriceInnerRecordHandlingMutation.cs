﻿using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Prices;

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
            return new Structure.Prices(entitySchema, PriceInnerRecordHandling);
        } if (existingValue.InnerRecordHandling != PriceInnerRecordHandling) {
            return new Structure.Prices(
                entitySchema,
                existingValue.Version + 1,
                existingValue.GetPrices().ToList(),
                PriceInnerRecordHandling
            );
        } 
        return existingValue;
    }
}