using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

public interface IReferenceSortableAttributeCompoundSchemaMutation : ISortableAttributeCompoundSchemaMutation,
    IReferenceSchemaMutation
{
    IReferenceSchema ReplaceSortableAttributeCompoundIfDifferent(
        IReferenceSchema referenceSchema,
        ISortableAttributeCompoundSchema existingSchema,
        ISortableAttributeCompoundSchema updatedSchema
    )
    {
        if (existingSchema.Equals(updatedSchema))
        {
            // we don't need to update entity schema - the associated data already contains the requested change
            return referenceSchema;
        }

        return ReferenceSchema.InternalBuild(
            referenceSchema.Name,
            referenceSchema.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.ReferencedEntityTypeManaged
                ? new Dictionary<NamingConvention, string?>()
                : referenceSchema.GetEntityTypeNameVariants(_ => null!),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.ReferencedGroupTypeManaged
                ? new Dictionary<NamingConvention, string?>()
                : referenceSchema.GetGroupTypeNameVariants(_ => null!),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.IsIndexed,
            referenceSchema.IsFaceted,
            referenceSchema.GetAttributes(),
            referenceSchema.GetSortableAttributeCompounds().Values
                .Where(it => updatedSchema.Name != it.Name)
                .ToDictionary(x => x.Name, x => x)
        );
    }
}