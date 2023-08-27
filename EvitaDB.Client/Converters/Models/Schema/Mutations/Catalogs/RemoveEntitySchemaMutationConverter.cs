using Client.Models.Schemas.Mutations.Catalogs;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Catalogs;

public class RemoveEntitySchemaMutationConverter : ISchemaMutationConverter<RemoveEntitySchemaMutation, GrpcRemoveEntitySchemaMutation>
{
    public GrpcRemoveEntitySchemaMutation Convert(RemoveEntitySchemaMutation mutation)
    {
        return new GrpcRemoveEntitySchemaMutation {Name = mutation.Name};
    }

    public RemoveEntitySchemaMutation Convert(GrpcRemoveEntitySchemaMutation mutation)
    {
        return new RemoveEntitySchemaMutation(mutation.Name);
    }
}