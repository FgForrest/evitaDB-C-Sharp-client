using Client.Models.Schemas.Mutations.Catalog;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Catalog;

public class ModifyCatalogSchemaNameMutationConverter : ISchemaMutationConverter<ModifyCatalogSchemaNameMutation, GrpcModifyCatalogSchemaNameMutation>
{
    public GrpcModifyCatalogSchemaNameMutation Convert(ModifyCatalogSchemaNameMutation mutation)
    {
        return new GrpcModifyCatalogSchemaNameMutation
        {
            CatalogName = mutation.CatalogName,
            NewCatalogName = mutation.NewCatalogName,
            OverwriteTarget = mutation.OverwriteTarget
        };
        
    }

    public ModifyCatalogSchemaNameMutation Convert(GrpcModifyCatalogSchemaNameMutation mutation)
    {
        return new ModifyCatalogSchemaNameMutation(
            mutation.CatalogName, mutation.NewCatalogName, mutation.OverwriteTarget
        );
    }
}