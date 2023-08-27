﻿using Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

public class ModifySortableAttributeCompoundSchemaNameMutationConverter : ISchemaMutationConverter<ModifySortableAttributeCompoundSchemaNameMutation, GrpcModifySortableAttributeCompoundSchemaNameMutation>
{
    public GrpcModifySortableAttributeCompoundSchemaNameMutation Convert(
        ModifySortableAttributeCompoundSchemaNameMutation mutation)
    {
        return new GrpcModifySortableAttributeCompoundSchemaNameMutation
        {
            Name = mutation.Name,
            NewName = mutation.NewName
        };
    }

    public ModifySortableAttributeCompoundSchemaNameMutation Convert(
        GrpcModifySortableAttributeCompoundSchemaNameMutation mutation)
    {
        return new ModifySortableAttributeCompoundSchemaNameMutation(mutation.Name, mutation.NewName);
    }
}