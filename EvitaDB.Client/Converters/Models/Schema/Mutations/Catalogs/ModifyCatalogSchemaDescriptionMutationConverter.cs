using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class ModifyCatalogSchemaDescriptionMutationConverter : ISchemaMutationConverter<
    ModifyCatalogSchemaDescriptionMutation, GrpcModifyCatalogSchemaDescriptionMutation>
{
    public GrpcModifyCatalogSchemaDescriptionMutation Convert(ModifyCatalogSchemaDescriptionMutation mutation)
    {
        return new GrpcModifyCatalogSchemaDescriptionMutation {Description = mutation.Description};
    }

    public ModifyCatalogSchemaDescriptionMutation Convert(GrpcModifyCatalogSchemaDescriptionMutation mutation)
    {
        return new ModifyCatalogSchemaDescriptionMutation(mutation.Description);
    }
}