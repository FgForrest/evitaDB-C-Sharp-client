using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Attributes;

public class UpsertAttributeMutationConverter : AttributeMutationConverter<UpsertAttributeMutation, GrpcUpsertAttributeMutation>
{
    public override GrpcUpsertAttributeMutation Convert(UpsertAttributeMutation mutation)
    {
        return new GrpcUpsertAttributeMutation
        {
            AttributeName = mutation.AttributeKey.AttributeName,
            AttributeLocale = mutation.AttributeKey.Localized ? EvitaDataTypesConverter.ToGrpcLocale(mutation.AttributeKey.Locale!) : null,
            AttributeValue = EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.Value)
        };
    }

    public override UpsertAttributeMutation Convert(GrpcUpsertAttributeMutation mutation)
    {
        AttributeKey key = BuildAttributeKey(mutation.AttributeName, mutation.AttributeLocale);
        object targetTypeValue = EvitaDataTypesConverter.ToEvitaValue(mutation.AttributeValue);
        return new UpsertAttributeMutation(key, targetTypeValue);
    }
}