using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class DisallowLocaleInEntitySchemaMutationConverter : ISchemaMutationConverter<DisallowLocaleInEntitySchemaMutation, GrpcDisallowLocaleInEntitySchemaMutation>
{
    public GrpcDisallowLocaleInEntitySchemaMutation Convert(DisallowLocaleInEntitySchemaMutation mutation)
    {
        return new GrpcDisallowLocaleInEntitySchemaMutation
        {
            Locales = {mutation.Locales.Select(EvitaDataTypesConverter.ToGrpcLocale)}
        };
    }

    public DisallowLocaleInEntitySchemaMutation Convert(GrpcDisallowLocaleInEntitySchemaMutation mutation)
    {
        return new DisallowLocaleInEntitySchemaMutation(mutation.Locales.Select(EvitaDataTypesConverter.ToLocale)
            .ToArray());
    }
}