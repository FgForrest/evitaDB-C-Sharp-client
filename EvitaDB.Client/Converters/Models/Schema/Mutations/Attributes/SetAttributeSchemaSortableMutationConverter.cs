using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaSortableMutationConverter : ISchemaMutationConverter<SetAttributeSchemaSortableMutation, GrpcSetAttributeSchemaSortableMutation>
{
    public GrpcSetAttributeSchemaSortableMutation Convert(SetAttributeSchemaSortableMutation mutation)
    {
        GrpcSetAttributeSchemaSortableMutation grpcMutation = new GrpcSetAttributeSchemaSortableMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Sortable = mutation.Sortable
#pragma warning restore CS0612
        };

        if (mutation.Sortable)
        {
            grpcMutation.SortableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcMutation;
    }

    public SetAttributeSchemaSortableMutation Convert(GrpcSetAttributeSchemaSortableMutation mutation)
    {
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
        return new SetAttributeSchemaSortableMutation(mutation.Name,
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.SortableInScopes, mutation.Sortable));
#pragma warning restore CS0612
    }
}