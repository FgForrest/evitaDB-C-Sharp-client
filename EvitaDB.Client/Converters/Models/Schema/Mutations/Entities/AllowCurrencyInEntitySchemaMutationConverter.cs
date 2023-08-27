using Client.Converters.DataTypes;
using Client.Models.Schemas.Mutations.Entities;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Entities;

public class AllowCurrencyInEntitySchemaMutationConverter : ISchemaMutationConverter<AllowCurrencyInEntitySchemaMutation
    , GrpcAllowCurrencyInEntitySchemaMutation>
{
    public GrpcAllowCurrencyInEntitySchemaMutation Convert(AllowCurrencyInEntitySchemaMutation mutation)
    {
        return new GrpcAllowCurrencyInEntitySchemaMutation
        {
            Currencies = {mutation.Currencies.Select(EvitaDataTypesConverter.ToGrpcCurrency)}
        };
    }

    public AllowCurrencyInEntitySchemaMutation Convert(GrpcAllowCurrencyInEntitySchemaMutation mutation)
    {
        return new AllowCurrencyInEntitySchemaMutation(mutation.Currencies.Select(EvitaDataTypesConverter.ToCurrency)
            .ToArray());
    }
}