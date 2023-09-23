using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyCatalogSchemaDescriptionMutation : ILocalCatalogSchemaMutation
{
    public string? Description { get; }

    public ModifyCatalogSchemaDescriptionMutation(string? description)
    {
        Description = description;
    }
    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.NotNull(
            catalogSchema,
            () => new InvalidSchemaMutationException("Catalog doesn't exist!")
            );
        if (Equals(Description, catalogSchema!.Description)) {
            // nothing has changed - we can return existing schema
            return catalogSchema;
        }

        return CatalogSchema.InternalBuild(
            catalogSchema.Version + 1,
            catalogSchema.Name,
            catalogSchema.NameVariants,
            Description,
            catalogSchema.CatalogEvolutionModes,
            catalogSchema.GetAttributes(),
            entityType => throw new NotSupportedException("Mutated catalog schema can't provide access to entity schemas!"));
    }
}