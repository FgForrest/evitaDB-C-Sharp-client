﻿using Client.Models.Schemas.Mutations.Entities;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Entities;

public class ModifyEntitySchemaDescriptionMutationConverter : ISchemaMutationConverter<ModifyEntitySchemaDescriptionMutation, GrpcModifyEntitySchemaDescriptionMutation>
{
    public GrpcModifyEntitySchemaDescriptionMutation Convert(ModifyEntitySchemaDescriptionMutation mutation)
    {
        return new GrpcModifyEntitySchemaDescriptionMutation
        {
            Description = mutation.Description
        };
    }

    public ModifyEntitySchemaDescriptionMutation Convert(GrpcModifyEntitySchemaDescriptionMutation mutation)
    {
        return new ModifyEntitySchemaDescriptionMutation(mutation.Description);
    }
}