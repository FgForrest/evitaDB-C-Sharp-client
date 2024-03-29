﻿using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.AssociatedData;

public class UpsertAssociatedDataMutationConverter : AssociatedDataMutationConverter<UpsertAssociatedDataMutation, GrpcUpsertAssociatedDataMutation>
{
    public override GrpcUpsertAssociatedDataMutation Convert(UpsertAssociatedDataMutation mutation)
    {
        GrpcUpsertAssociatedDataMutation grpcUpsertAssociatedDataMutation = new()
        {
            AssociatedDataName = mutation.AssociatedDataKey.AssociatedDataName,
            AssociatedDataValue = EvitaDataTypesConverter.ToGrpcEvitaAssociatedDataValue(mutation.Value)
        };
        
        if (mutation.AssociatedDataKey.Localized)
        {
            grpcUpsertAssociatedDataMutation.AssociatedDataLocale = EvitaDataTypesConverter.ToGrpcLocale(mutation.AssociatedDataKey.Locale!);
        }
        
        return grpcUpsertAssociatedDataMutation;
    }

    public override UpsertAssociatedDataMutation Convert(GrpcUpsertAssociatedDataMutation mutation)
    {
        AssociatedDataKey key = BuildAssociatedDataKey(mutation.AssociatedDataName, mutation.AssociatedDataLocale);
        object targetTypeValue = EvitaDataTypesConverter.ToEvitaValue(mutation.AssociatedDataValue);
        return new UpsertAssociatedDataMutation(key, targetTypeValue);
    }
}