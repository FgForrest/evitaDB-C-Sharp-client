using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class SetReferenceSchemaFacetedMutationConverter : ISchemaMutationConverter<SetReferenceSchemaFacetedMutation, GrpcSetReferenceSchemaFacetedMutation>
{
    public GrpcSetReferenceSchemaFacetedMutation Convert(SetReferenceSchemaFacetedMutation mutation)
    {
        GrpcSetReferenceSchemaFacetedMutation grpcMutation = new GrpcSetReferenceSchemaFacetedMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Faceted = mutation.Faceted
#pragma warning restore CS0612
        };

        if (mutation.Faceted)
        {
            grpcMutation.FacetedInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcMutation;
    }

    public SetReferenceSchemaFacetedMutation Convert(GrpcSetReferenceSchemaFacetedMutation mutation)
    {
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
        return new SetReferenceSchemaFacetedMutation(mutation.Name,
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.FacetedInScopes, mutation.Faceted));
#pragma warning restore CS0612
    }
}