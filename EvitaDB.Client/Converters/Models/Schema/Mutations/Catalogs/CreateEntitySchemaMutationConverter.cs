using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class CreateEntitySchemaMutationConverter : ISchemaMutationConverter<CreateEntitySchemaMutation, GrpcCreateEntitySchemaMutation>
{
    public GrpcCreateEntitySchemaMutation Convert(CreateEntitySchemaMutation mutation)
    {
        return new GrpcCreateEntitySchemaMutation
        {
            EntityType = mutation.Name
        };
    }

    public CreateEntitySchemaMutation Convert(GrpcCreateEntitySchemaMutation mutation)
    {
        return new CreateEntitySchemaMutation(mutation.EntityType);
    }
}