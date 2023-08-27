﻿using Client.Models.Schemas.Mutations.References;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.References;

public class SetReferenceSchemaFacetedMutationConverter : ISchemaMutationConverter<SetReferenceSchemaFacetedMutation, GrpcSetReferenceSchemaFacetedMutation>
{
    public GrpcSetReferenceSchemaFacetedMutation Convert(SetReferenceSchemaFacetedMutation mutation)
    {
        return new GrpcSetReferenceSchemaFacetedMutation
        {
            Name = mutation.Name,
            Faceted = mutation.Faceted
        };
    }

    public SetReferenceSchemaFacetedMutation Convert(GrpcSetReferenceSchemaFacetedMutation mutation)
    {
        return new SetReferenceSchemaFacetedMutation(mutation.Name, mutation.Faceted);
    }
}