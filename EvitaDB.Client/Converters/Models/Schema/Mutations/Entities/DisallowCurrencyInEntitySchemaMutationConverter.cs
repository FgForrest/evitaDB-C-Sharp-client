using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

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