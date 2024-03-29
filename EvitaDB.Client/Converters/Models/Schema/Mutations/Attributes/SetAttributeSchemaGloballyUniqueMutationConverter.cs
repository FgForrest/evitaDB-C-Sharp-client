﻿using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaGloballyUniqueMutationConverter : ISchemaMutationConverter<SetAttributeSchemaGloballyUniqueMutation, GrpcSetAttributeSchemaGloballyUniqueMutation>
{
    public GrpcSetAttributeSchemaGloballyUniqueMutation Convert(SetAttributeSchemaGloballyUniqueMutation mutation)
    {
        return new GrpcSetAttributeSchemaGloballyUniqueMutation
        {
            Name = mutation.Name,
            UniqueGlobally = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally)
        };
    }

    public SetAttributeSchemaGloballyUniqueMutation Convert(GrpcSetAttributeSchemaGloballyUniqueMutation mutation)
    {
        return new SetAttributeSchemaGloballyUniqueMutation(mutation.Name,
            EvitaEnumConverter.ToGlobalAttributeUniquenessType(mutation.UniqueGlobally));
    }
}
