using Client.Converters.DataTypes;
using Client.Models.Data;
using Client.Models.Data.Mutations.Attributes;
using EvitaDB;
using Google.Protobuf;

namespace Client.Converters.Models.Data.Mutations.Attributes;

public abstract class AttributeMutationConverter<TJ, TG> : ILocalMutationConverter<TJ, TG> where TJ : AttributeMutation where TG : IMessage
{
    protected static AttributeKey BuildAttributeKey(string attributeName, GrpcLocale attributeLocale) {
        if (!attributeLocale.IsInitialized()) {
            return new AttributeKey(
                attributeName,
                EvitaDataTypesConverter.ToLocale(attributeLocale)
            );
        }
        return new AttributeKey(attributeName);
    }

    public abstract TG Convert(TJ mutation);

    public abstract TJ Convert(TG mutation);
}