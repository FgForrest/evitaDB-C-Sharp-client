using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaFilterableMutationConverter : ISchemaMutationConverter<SetAttributeSchemaFilterableMutation, GrpcSetAttributeSchemaFilterableMutation>
{
    public GrpcSetAttributeSchemaFilterableMutation Convert(SetAttributeSchemaFilterableMutation mutation)
    {
        GrpcSetAttributeSchemaFilterableMutation grpcMutation = new GrpcSetAttributeSchemaFilterableMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Filterable = mutation.Filterable
#pragma warning restore CS0612
        };

        if (mutation.Filterable)
        {
            grpcMutation.FilterableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcMutation;
    }

    public SetAttributeSchemaFilterableMutation Convert(GrpcSetAttributeSchemaFilterableMutation mutation)
    {
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
        return new SetAttributeSchemaFilterableMutation(mutation.Name,
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.FilterableInScopes, mutation.Filterable));
#pragma warning restore CS0612
    }
}