﻿using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class RemoveAssociatedDataSchemaMutationConverter : ISchemaMutationConverter<RemoveAssociatedDataSchemaMutation, GrpcRemoveAssociatedDataSchemaMutation>
{
    public GrpcRemoveAssociatedDataSchemaMutation Convert(RemoveAssociatedDataSchemaMutation mutation)
    {
        return new GrpcRemoveAssociatedDataSchemaMutation
        {
            Name = mutation.Name
        };
    }

    public RemoveAssociatedDataSchemaMutation Convert(GrpcRemoveAssociatedDataSchemaMutation mutation)
    {
        return new RemoveAssociatedDataSchemaMutation(mutation.Name);
    }
}