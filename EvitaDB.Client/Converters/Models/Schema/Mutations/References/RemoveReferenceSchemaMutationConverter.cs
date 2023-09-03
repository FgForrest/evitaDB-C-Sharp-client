using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class RemoveReferenceSchemaMutationConverter : ISchemaMutationConverter<RemoveReferenceSchemaMutation, GrpcRemoveReferenceSchemaMutation>
{
    public GrpcRemoveReferenceSchemaMutation Convert(RemoveReferenceSchemaMutation mutation)
    {
        return new GrpcRemoveReferenceSchemaMutation
        {
            Name = mutation.Name
        };
    }

    public RemoveReferenceSchemaMutation Convert(GrpcRemoveReferenceSchemaMutation mutation)
    {
        return new RemoveReferenceSchemaMutation(mutation.Name);
    }
}