using Client.Converters.DataTypes;
using Client.Exceptions;
using Client.Models.Data.Mutations.Price;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Price;

public class UpsertPriceMutationConverter : PriceMutationConverter<UpsertPriceMutation, GrpcUpsertPriceMutation>
{
    public override GrpcUpsertPriceMutation Convert(UpsertPriceMutation mutation)
    {
        GrpcUpsertPriceMutation grpcUpsertPriceMutation = new()
        {
            PriceId = mutation.PriceKey.PriceId,
            PriceList = mutation.PriceKey.PriceList,
            Currency = EvitaDataTypesConverter.ToGrpcCurrency(mutation.PriceKey.Currency),
            PriceWithoutTax = EvitaDataTypesConverter.ToGrpcBigDecimal(mutation.PriceWithoutTax),
            TaxRate = EvitaDataTypesConverter.ToGrpcBigDecimal(mutation.TaxRate),
            PriceWithTax = EvitaDataTypesConverter.ToGrpcBigDecimal(mutation.PriceWithTax),
            Sellable = mutation.Sellable,
            InnerRecordId = mutation.InnerRecordId,
            Validity = mutation.Validity == null ? null : EvitaDataTypesConverter.ToGrpcDateTimeRange(mutation.Validity)
        };
        
        return grpcUpsertPriceMutation;
    }

    public override UpsertPriceMutation Convert(GrpcUpsertPriceMutation mutation)
    {
        if (mutation.PriceWithoutTax == null || mutation.TaxRate == null || mutation.PriceWithTax == null)
        {
            throw new EvitaInvalidUsageException("Price mutation must have priceWithoutTax, taxRate and priceWithTax.");
        }

        return new UpsertPriceMutation(
            BuildPriceKey(mutation.PriceId, mutation.PriceList, mutation.Currency),
            mutation.InnerRecordId,
            EvitaDataTypesConverter.ToDecimal(mutation.PriceWithoutTax),
            EvitaDataTypesConverter.ToDecimal(mutation.TaxRate),
            EvitaDataTypesConverter.ToDecimal(mutation.PriceWithTax),
            mutation.Validity == null ? null : EvitaDataTypesConverter.ToDateTimeRange(mutation.Validity),
            mutation.Sellable
        );
    }
}