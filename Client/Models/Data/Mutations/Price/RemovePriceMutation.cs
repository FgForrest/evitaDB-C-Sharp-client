﻿using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Schemas;
using Client.Utils;

namespace Client.Models.Data.Mutations.Price;

public class RemovePriceMutation : PriceMutation
{
    public RemovePriceMutation(PriceKey priceKey) : base(priceKey)
    {
    }

    public RemovePriceMutation(int priceId, string priceList, Currency currency) : this(new PriceKey(priceId, priceList,
        currency))
    {
    }

    public override IPrice MutateLocal(IEntitySchema entitySchema, IPrice? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException("Cannot remove price that doesn't exist!")
            );
        return new Structure.Price(
            existingValue!.Version + 1,
            existingValue.Key,
            existingValue.InnerRecordId,
            existingValue.PriceWithoutTax,
            existingValue.TaxRate,
            existingValue.PriceWithTax,
            existingValue.Validity,
            existingValue.Sellable,
            true
        );
    }
}