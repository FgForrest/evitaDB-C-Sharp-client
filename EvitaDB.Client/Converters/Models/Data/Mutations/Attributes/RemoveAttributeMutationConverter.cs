using Client.Converters.DataTypes;
using Client.Models.Data;
using Client.Models.Data.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Attributes;

public class RemoveAttributeMutationConverter : AttributeMutationConverter<RemoveAttributeMutation, GrpcRemoveAttributeMutation>
{
    public override GrpcRemoveAttributeMutation Convert(RemoveAttributeMutation mutation)
    {
        GrpcRemoveAttributeMutation grpcRemoveAttributeMutation = new()
        {
            AttributeName = mutation.AttributeKey.AttributeName
        };
        
        if (mutation.AttributeKey.Localized)
        {
            grpcRemoveAttributeMutation.AttributeLocale = EvitaDataTypesConverter.ToGrpcLocale(mutation.AttributeKey.Locale!);
        }

        return grpcRemoveAttributeMutation;
    }

    public override RemoveAttributeMutation Convert(GrpcRemoveAttributeMutation mutation)
    {
        AttributeKey key = BuildAttributeKey(mutation.AttributeName, mutation.AttributeLocale);
        return new RemoveAttributeMutation(key);
    }
}