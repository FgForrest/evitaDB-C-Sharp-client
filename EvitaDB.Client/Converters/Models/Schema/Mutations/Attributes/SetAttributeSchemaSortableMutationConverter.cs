using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaSortableMutationConverter : ISchemaMutationConverter<SetAttributeSchemaSortableMutation, GrpcSetAttributeSchemaSortableMutation>
{
    public GrpcSetAttributeSchemaSortableMutation Convert(SetAttributeSchemaSortableMutation mutation)
    {
        return new GrpcSetAttributeSchemaSortableMutation
        {
            Name = mutation.Name,
            Sortable = mutation.Sortable
        };
    }

    public SetAttributeSchemaSortableMutation Convert(GrpcSetAttributeSchemaSortableMutation mutation)
    {
        return new SetAttributeSchemaSortableMutation(mutation.Name, mutation.Sortable);
    }
}