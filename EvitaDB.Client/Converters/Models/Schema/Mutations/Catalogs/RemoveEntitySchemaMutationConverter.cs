﻿using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class RemoveEntitySchemaMutationConverter : ISchemaMutationConverter<RemoveEntitySchemaMutation, GrpcRemoveEntitySchemaMutation>
{
    public GrpcRemoveEntitySchemaMutation Convert(RemoveEntitySchemaMutation mutation)
    {
        return new GrpcRemoveEntitySchemaMutation {Name = mutation.Name};
    }

    public RemoveEntitySchemaMutation Convert(GrpcRemoveEntitySchemaMutation mutation)
    {
        return new RemoveEntitySchemaMutation(mutation.Name);
    }
}