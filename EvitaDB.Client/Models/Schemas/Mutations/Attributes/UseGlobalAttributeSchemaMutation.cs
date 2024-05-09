using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class UseGlobalAttributeSchemaMutation : IEntityAttributeSchemaMutation
{
    public string Name { get; }

    public UseGlobalAttributeSchemaMutation(string name)
    {
        Name = name;
    }

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        return (TS) catalogSchema!.GetAttribute(Name)! ?? throw new EvitaInvalidUsageException(
            "No global attribute with name `" + Name + "` found in catalog `" + catalogSchema.Name + "`.");
    }

    public IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IGlobalAttributeSchema newAttributeSchema = Mutate(catalogSchema, (IGlobalAttributeSchema?) null, typeof(IGlobalAttributeSchema));
        Assert.NotNull(
            newAttributeSchema,
            () => new InvalidSchemaMutationException(
                "The attribute `" + Name + "` is not defined in catalog `" + catalogSchema.Name + "` schema!"
            )
            );
        IAttributeSchema? existingAttributeSchema = entitySchema!.GetAttribute(Name);
        if (existingAttributeSchema == null) {
            return EntitySchema.InternalBuild(
                entitySchema.Version + 1,
                entitySchema.Name,
                entitySchema.NameVariants,
                entitySchema.Description,
                entitySchema.DeprecationNotice,
                entitySchema.WithGeneratedPrimaryKey(),
                entitySchema.WithHierarchy(),
                entitySchema.WithPrice(),
                entitySchema.IndexedPricePlaces,
                entitySchema.Locales,
                entitySchema.Currencies,
                entitySchema.GetAttributes().Values.Concat(new []{newAttributeSchema}).ToDictionary(x=>x.Name, x=>x),
                entitySchema.AssociatedData,
                entitySchema.References,
                entitySchema.EvolutionModes,
                entitySchema.GetSortableAttributeCompounds()
            );
        }

        if (existingAttributeSchema.Equals(newAttributeSchema)) {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The attribute `" + Name + "` already exists in entity `" + entitySchema.Name + "` schema and" +
            " has different definition. To alter existing attribute schema you need to use different mutations."
        );
    }
}
