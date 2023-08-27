using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.SortableAttributeCompounds;

public class CreateSortableAttributeCompoundSchemaMutation : IEntitySchemaMutation,
    IReferenceSortableAttributeCompoundSchemaMutation
{
    public string Name { get; }
    public string? Description { get; }
    public string? DeprecationNotice { get; }
    public AttributeElement[] AttributeElements { get; }

    public CreateSortableAttributeCompoundSchemaMutation(string name, string? description, string? deprecationNotice,
        AttributeElement[] attributeElements)
    {
        Name = name;
        Description = description;
        DeprecationNotice = deprecationNotice;
        AttributeElements = attributeElements;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        SortableAttributeCompoundSchema newCompoundSchema = Mutate(entitySchema, null, null);
        SortableAttributeCompoundSchema? existingCompoundSchema = entitySchema.GetSortableAttributeCompound(Name);
        if (existingCompoundSchema == null)
        {
            return EntitySchema.InternalBuild(
                entitySchema.Version + 1,
                entitySchema.Name,
                entitySchema.NameVariants,
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
                entitySchema.GetSortableAttributeCompounds().Values
                    .Concat(new[] {newCompoundSchema})
                    .ToDictionary(x => x.Name, x => x)
            );
        }

        if (existingCompoundSchema.Equals(newCompoundSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return entitySchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The sortable attribute compound `" + Name + "` already exists in entity `" + entitySchema.Name +
            "` schema and it has different definition. To alter existing sortable attribute compound schema you" +
            " need to use different mutations."
        );
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        SortableAttributeCompoundSchema? newCompoundSchema = Mutate(entitySchema, referenceSchema, null);
        SortableAttributeCompoundSchema? existingCompoundSchema = referenceSchema!.GetSortableAttributeCompound(Name);
        if (existingCompoundSchema == null)
        {
            return ReferenceSchema.InternalBuild(
                referenceSchema.Name,
                referenceSchema.NameVariants,
                referenceSchema.Description,
                referenceSchema.DeprecationNotice,
                referenceSchema.ReferencedEntityType,
                referenceSchema.ReferencedEntityTypeManaged
                    ? new Dictionary<NamingConvention, string>()
                    : referenceSchema.GetEntityTypeNameVariants(s => null),
                referenceSchema.ReferencedEntityTypeManaged,
                referenceSchema.Cardinality,
                referenceSchema.ReferencedGroupType,
                referenceSchema.ReferencedGroupTypeManaged
                    ? new Dictionary<NamingConvention, string>()
                    : referenceSchema.GetGroupTypeNameVariants(s => null),
                referenceSchema.ReferencedGroupTypeManaged,
                referenceSchema.Indexed,
                referenceSchema.Faceted,
                referenceSchema.GetAttributes(),
                referenceSchema.GetSortableAttributeCompounds().Values.Concat(new[] {newCompoundSchema})
                    .ToDictionary(x => x.Name, x => x)
            );
        }

        if (existingCompoundSchema.Equals(newCompoundSchema))
        {
            // the mutation must have been applied previously - return the schema we don't need to alter
            return referenceSchema;
        }

        // ups, there is conflict in attribute settings
        throw new InvalidSchemaMutationException(
            "The sortable attribute compound `" + Name + "` already exists in entity `" + entitySchema.Name + "`" +
            " reference `" + referenceSchema.Name + "` schema and" +
            " it has different definition. To alter existing sortable attribute compound schema you need to use" +
            " different mutations."
        );
    }

    public SortableAttributeCompoundSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? sortableAttributeCompoundSchema)
    {
        return SortableAttributeCompoundSchema.InternalBuild(
            Name, Description, DeprecationNotice,
            AttributeElements.ToList()
        );
    }
}