using Client.Converters.DataTypes;
using Client.Models.Data;
using Client.Models.Data.Mutations.AssociatedData;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.AssociatedData;

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