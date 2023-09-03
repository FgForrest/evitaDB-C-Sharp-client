using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

public class ModifySortableAttributeCompoundSchemaNameMutation : IEntitySchemaMutation,
    IReferenceSortableAttributeCompoundSchemaMutation
{
    public string Name { get; }
    public string NewName { get; }

    public ModifySortableAttributeCompoundSchemaNameMutation(string name, string newName)
    {
        Name = name;
        NewName = newName;
    }

    public IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        SortableAttributeCompoundSchema existingCompoundSchema = entitySchema!.GetSortableAttributeCompound(Name) ??
                                                                 throw new InvalidSchemaMutationException(
                                                                     "The sortable attribute compound `" + Name +
                                                                     "` is not defined in entity `" +
                                                                     entitySchema.Name + "` schema!"
                                                                 );

        SortableAttributeCompoundSchema? updatedAttributeSchema = Mutate(entitySchema, null, existingCompoundSchema);
        return (this as ISortableAttributeCompoundSchemaMutation).ReplaceSortableAttributeCompoundIfDifferent(
            entitySchema, existingCompoundSchema, updatedAttributeSchema
        );
    }

    public IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        SortableAttributeCompoundSchema existingCompoundSchema = referenceSchema!.GetSortableAttributeCompound(Name) ??
                                                                 throw new InvalidSchemaMutationException(
                                                                     "The sortable attribute compound `" + Name +
                                                                     "` is not defined in entity `" +
                                                                     entitySchema.Name +
                                                                     "` schema for reference with name `" +
                                                                     referenceSchema.Name + "`!"
                                                                 );

        SortableAttributeCompoundSchema? updatedAttributeSchema =
            Mutate(entitySchema, null, existingCompoundSchema);
        return (this as IReferenceSortableAttributeCompoundSchemaMutation).ReplaceSortableAttributeCompoundIfDifferent(
            referenceSchema, existingCompoundSchema, updatedAttributeSchema
        );
    }

    public SortableAttributeCompoundSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema,
        ISortableAttributeCompoundSchema? sortableAttributeCompoundSchema)
    {
        Assert.IsPremiseValid(sortableAttributeCompoundSchema != null,
            "Sortable attribute compound schema is mandatory!");
        return SortableAttributeCompoundSchema.InternalBuild(
            NewName,
            sortableAttributeCompoundSchema!.NameVariants,
            sortableAttributeCompoundSchema.Description,
            sortableAttributeCompoundSchema.DeprecationNotice,
            sortableAttributeCompoundSchema.AttributeElements
        );
    }
}