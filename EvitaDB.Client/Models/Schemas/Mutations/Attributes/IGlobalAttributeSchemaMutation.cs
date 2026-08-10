using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public interface IGlobalAttributeSchemaMutation : IAttributeSchemaMutation, ICatalogSchemaMutation
{
    CatalogSchemaWithImpactOnEntitySchemas ReplaceAttributeIfDifferent(
        ICatalogSchema catalogSchema,
        IGlobalAttributeSchema existingAttributeSchema,
        IGlobalAttributeSchema updatedAttributeSchema,
        IEntitySchemaProvider entitySchemaProvider,
        IEntityAttributeSchemaMutation attributeSchemaMutation
    )
    {
        if (existingAttributeSchema.Equals(updatedAttributeSchema))
        {
            return new CatalogSchemaWithImpactOnEntitySchemas(catalogSchema);
        }

        return new CatalogSchemaWithImpactOnEntitySchemas(
                CatalogSchema.InternalBuild(
                    catalogSchema.Version + 1,
                    catalogSchema.Name,
                    catalogSchema.NameVariants,
                    catalogSchema.Description,
                    catalogSchema.CatalogEvolutionModes,
                    catalogSchema.GetAttributes().Values.Where(x => updatedAttributeSchema.Name != x.Name)
                        .Concat([updatedAttributeSchema])
                        .ToDictionary(x=>x.Name, x=>x),
                    entitySchemaProvider
                ),
                entitySchemaProvider
                    .GetEntitySchemas()
                    .Where(z => z is not null)
                    .Cast<IEntitySchema>()
                    .Where(x => x.GetAttributes().ContainsKey(existingAttributeSchema.Name))
                    .Select(it => new ModifyEntitySchemaMutation(
                        it.Name,
                        attributeSchemaMutation
                        )
                    ).ToArray()
        );
    }
}
