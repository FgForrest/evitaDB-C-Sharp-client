using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.AssociatedData;

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