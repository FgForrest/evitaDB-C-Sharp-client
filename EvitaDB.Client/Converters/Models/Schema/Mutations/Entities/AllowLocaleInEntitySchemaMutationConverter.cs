using Client.Converters.DataTypes;
using Client.Models.Schemas.Mutations.Entities;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Entities;

public class AllowLocaleInEntitySchemaMutationConverter : ISchemaMutationConverter<AllowLocaleInEntitySchemaMutation, GrpcAllowLocaleInEntitySchemaMutation>
{
    public GrpcAllowLocaleInEntitySchemaMutation Convert(AllowLocaleInEntitySchemaMutation mutation)
    {
        return new GrpcAllowLocaleInEntitySchemaMutation
        {
            Locales = {mutation.Locales.Select(EvitaDataTypesConverter.ToGrpcLocale)}
        };
    }

    public AllowLocaleInEntitySchemaMutation Convert(GrpcAllowLocaleInEntitySchemaMutation mutation)
    {
        return new AllowLocaleInEntitySchemaMutation(
            mutation.Locales.Select(EvitaDataTypesConverter.ToLocale).ToArray());
    }
}