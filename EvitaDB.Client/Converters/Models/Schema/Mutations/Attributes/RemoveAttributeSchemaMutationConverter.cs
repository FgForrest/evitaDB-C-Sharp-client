using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class RemoveAttributeSchemaMutationConverter : ISchemaMutationConverter<RemoveAttributeSchemaMutation, GrpcRemoveAttributeSchemaMutation>
{
    public GrpcRemoveAttributeSchemaMutation Convert(RemoveAttributeSchemaMutation mutation)
    {
        return new GrpcRemoveAttributeSchemaMutation
        {
            Name = mutation.Name
        };
    }

    public RemoveAttributeSchemaMutation Convert(GrpcRemoveAttributeSchemaMutation mutation)
    {
        return new RemoveAttributeSchemaMutation(mutation.Name);
    }
}