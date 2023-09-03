using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.AssociatedData;

public class RemoveAssociatedDataMutationConverter : AssociatedDataMutationConverter<RemoveAssociatedDataMutation, GrpcRemoveAssociatedDataMutation>
{
    public override GrpcRemoveAssociatedDataMutation Convert(RemoveAssociatedDataMutation mutation)
    {
        GrpcRemoveAssociatedDataMutation grpcRemoveAssociatedDataMutation = new()
        {
            AssociatedDataName = mutation.AssociatedDataKey.AssociatedDataName
        };

        if (mutation.AssociatedDataKey.Localized)
        {
            grpcRemoveAssociatedDataMutation.AssociatedDataLocale = EvitaDataTypesConverter.ToGrpcLocale(mutation.AssociatedDataKey.Locale!);
        }
        
        return grpcRemoveAssociatedDataMutation;
    }

    public override RemoveAssociatedDataMutation Convert(GrpcRemoveAssociatedDataMutation mutation)
    {
        AssociatedDataKey key = BuildAssociatedDataKey(mutation.AssociatedDataName, mutation.AssociatedDataLocale);
        return new RemoveAssociatedDataMutation(key);
    }
}