﻿using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaLocalizedMutationConverter : ISchemaMutationConverter<SetAttributeSchemaLocalizedMutation, GrpcSetAttributeSchemaLocalizedMutation>
{
    public GrpcSetAttributeSchemaLocalizedMutation Convert(SetAttributeSchemaLocalizedMutation mutation)
    {
        return new GrpcSetAttributeSchemaLocalizedMutation
        {
            Name = mutation.Name,
            Localized = mutation.Localized
        };
    }

    public SetAttributeSchemaLocalizedMutation Convert(GrpcSetAttributeSchemaLocalizedMutation mutation)
    {
        return new SetAttributeSchemaLocalizedMutation(mutation.Name, mutation.Localized);
    }
}