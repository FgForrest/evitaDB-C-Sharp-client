using Client.Converters.DataTypes;
using Client.Models.Schemas.Mutations.Entities;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Entities;

public class DisallowCurrencyInEntitySchemaMutationConverter : ISchemaMutationConverter<DisallowCurrencyInEntitySchemaMutation, GrpcDisallowCurrencyInEntitySchemaMutation>
{
    public GrpcDisallowCurrencyInEntitySchemaMutation Convert(DisallowCurrencyInEntitySchemaMutation mutation)
    {
        return new GrpcDisallowCurrencyInEntitySchemaMutation
        {
            Currencies = {mutation.Currencies.Select(EvitaDataTypesConverter.ToGrpcCurrency)}
        };
    }

    public DisallowCurrencyInEntitySchemaMutation Convert(GrpcDisallowCurrencyInEntitySchemaMutation mutation)
    {
        return new DisallowCurrencyInEntitySchemaMutation(mutation.Currencies.Select(EvitaDataTypesConverter.ToCurrency)
            .ToArray());
    }
}