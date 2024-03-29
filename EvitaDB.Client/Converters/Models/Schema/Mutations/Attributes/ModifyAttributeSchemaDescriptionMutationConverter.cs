﻿using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class ModifyAttributeSchemaDescriptionMutationConverter : ISchemaMutationConverter<ModifyAttributeSchemaDescriptionMutation, GrpcModifyAttributeSchemaDescriptionMutation>
{
    public GrpcModifyAttributeSchemaDescriptionMutation Convert(ModifyAttributeSchemaDescriptionMutation mutation)
    {
        return new GrpcModifyAttributeSchemaDescriptionMutation
        {
            Name = mutation.Name,
            Description = mutation.Description
        };
    }

    public ModifyAttributeSchemaDescriptionMutation Convert(GrpcModifyAttributeSchemaDescriptionMutation mutation)
    {
        return new ModifyAttributeSchemaDescriptionMutation(mutation.Name, mutation.Description);
    }
}