using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.SortableAttributeCompounds;

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
            referenceSchema.GetSortableAttributeCompounds().Values
                .Where(it => updatedSchema.Name != it.Name)
                .ToDictionary(x => x.Name, x => x)
        );
    }
}