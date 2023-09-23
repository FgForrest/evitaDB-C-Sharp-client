﻿using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaUniqueMutationConverter : ISchemaMutationConverter<SetAttributeSchemaUniqueMutation, GrpcSetAttributeSchemaUniqueMutation>
{
    public GrpcSetAttributeSchemaUniqueMutation Convert(SetAttributeSchemaUniqueMutation mutation)
    {
        return new GrpcSetAttributeSchemaUniqueMutation
        {
            Name = mutation.Name,
            Unique = mutation.Unique
        };
    }

    public SetAttributeSchemaUniqueMutation Convert(GrpcSetAttributeSchemaUniqueMutation mutation)
    {
        return new SetAttributeSchemaUniqueMutation(mutation.Name, mutation.Unique);
    }
}