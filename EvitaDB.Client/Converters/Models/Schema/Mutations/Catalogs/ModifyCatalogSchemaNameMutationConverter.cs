using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

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