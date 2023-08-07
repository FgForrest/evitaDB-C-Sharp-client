using Client.Converters.DataTypes;
using Client.Models.Data;
using Client.Models.Data.Mutations.AssociatedData;
using EvitaDB;
using Google.Protobuf;

namespace Client.Converters.Models.Data.Mutations.AssociatedData;

public abstract class AssociatedDataMutationConverter<TJ, TG> : ILocalMutationConverter<TJ, TG> where TJ : AssociatedDataMutation where TG : IMessage
{
    protected static AssociatedDataKey BuildAssociatedDataKey(string associatedDataName, GrpcLocale associatedDataLocale) {
        if (!associatedDataLocale.IsInitialized()) {
            return new AssociatedDataKey(
                associatedDataName,
                EvitaDataTypesConverter.ToLocale(associatedDataLocale)
            );
        }
        return new AssociatedDataKey(associatedDataName);
    }

    public abstract TG Convert(TJ mutation);

    public abstract TJ Convert(TG mutation);
}