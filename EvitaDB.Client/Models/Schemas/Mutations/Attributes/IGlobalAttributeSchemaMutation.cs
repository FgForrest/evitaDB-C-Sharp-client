using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public interface IGlobalAttributeSchemaMutation : IAttributeSchemaMutation, ICatalogSchemaMutation
{
    ICatalogSchema ReplaceAttributeIfDifferent(
        ICatalogSchema catalogSchema,
        IGlobalAttributeSchema existingAttributeSchema,
        IGlobalAttributeSchema updatedAttributeSchema
    )
    {
        if (existingAttributeSchema.Equals(updatedAttributeSchema))
        {
            return catalogSchema;
        }

        return CatalogSchema.InternalBuild(
            catalogSchema.Version + 1,
            catalogSchema.Name,
            catalogSchema.NameVariants,
            catalogSchema.Description,
            catalogSchema.CatalogEvolutionModes,
            catalogSchema.GetAttributes().Values.Where(x => updatedAttributeSchema.Name != x.Name)
                .Concat(new []{updatedAttributeSchema})
                .ToDictionary(x=>x.Name, x=>x),
            catalogSchema is CatalogSchema cs ? 
                cs.EntitySchemaAccessor : _ => 
                    throw new NotSupportedException("Mutated schema is not able to provide access to entity schemas!")
        );
    }
}