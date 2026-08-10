using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class ModifyEntitySchemaNameMutation : ILocalCatalogSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string NewName { get; }
    public bool OverwriteTarget { get; }
    public Operation Operation => Operation.Upsert;

    public ModifyEntitySchemaNameMutation(string name, string newName, bool overwriteTarget)
    {
        Name = name;
        NewName = newName;
        OverwriteTarget = overwriteTarget;
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas? Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider)
    {
        if (entitySchemaProvider is MutationEntitySchemaAccessor mutationEntitySchemaAccessor)
        {
            var entitySchema = mutationEntitySchemaAccessor.GetEntitySchema(Name);
            // TODO tpz: solve nullability issue below
            IEntitySchema? alteredSchema = Mutate(catalogSchema!, entitySchema);
            if (alteredSchema is not null)
            {
                mutationEntitySchemaAccessor.ReplaceEntitySchema(Name, alteredSchema);
            }
            else
            {
                throw new EvitaInternalError("Entity schema not found: " + Name);
            }
            
        }
        // do nothing - we alter only the entity schema
        // TODO tpz: solve nullability issue below
        return new ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas(
            catalogSchema!
        );
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.NotNull(
            entitySchema,
            () => new InvalidSchemaException("Entity schema `" + Name + "` doesn't exist!")
            );
        if (NewName.Equals(catalogSchema.Name)) {
            // nothing has changed - we can return existing schema
            return entitySchema;
        }

        return EntitySchema.InternalBuild(
            entitySchema!.Version + 1,
            NewName,
            NamingConventionHelper.Generate(NewName),
            entitySchema.Description,
            entitySchema.DeprecationNotice,
            entitySchema.WithGeneratedPrimaryKey(),
            entitySchema.WithHierarchy(),
            entitySchema.WithPrice(),
            entitySchema.IndexedPricePlaces,
            entitySchema.Locales,
            entitySchema.Currencies,
            entitySchema.Attributes,
            entitySchema.AssociatedData,
            entitySchema.References,
            entitySchema.EvolutionModes,
            entitySchema.GetSortableAttributeCompounds()
        );
    }
}
