using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.Catalogs;

public class ModifyEntitySchemaNameMutation : ILocalCatalogSchemaMutation, IEntitySchemaMutation
{
    public string Name { get; }
    public string NewName { get; }
    public bool OverwriteTarget { get; }

    public ModifyEntitySchemaNameMutation(string name, string newName, bool overwriteTarget)
    {
        Name = name;
        NewName = newName;
        OverwriteTarget = overwriteTarget;
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        return catalogSchema;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.NotNull(
            entitySchema,
            () => new InvalidSchemaMutationException("Entity schema `" + Name + "` doesn't exist!")
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
            entitySchema.WithGeneratedPrimaryKey,
            entitySchema.WithHierarchy,
            entitySchema.WithPrice,
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