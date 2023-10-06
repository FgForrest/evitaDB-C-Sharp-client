using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public interface IReferenceAttributeSchemaMutation : IAttributeSchemaMutation, IReferenceSchemaMutation
{
    IReferenceSchema ReplaceAttributeIfDifferent(IReferenceSchema referenceSchema,
        IAttributeSchema existingAttributeSchema, IAttributeSchema updatedAttributeSchema)
    {
        if (existingAttributeSchema.Equals(updatedAttributeSchema)) {
            // we don't need to update entity schema - the associated data already contains the requested change
            return referenceSchema;
        }

        return ReferenceSchema.InternalBuild(
            referenceSchema.Name,
            referenceSchema.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.ReferencedEntityTypeManaged ? new Dictionary<NamingConvention, string?>() : referenceSchema.GetEntityTypeNameVariants(s => null),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.ReferencedGroupTypeManaged ? new Dictionary<NamingConvention, string?>() : referenceSchema.GetGroupTypeNameVariants(s => null),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.IsIndexed,
            referenceSchema.IsFaceted,
            referenceSchema.GetAttributes().Values.Where(x => updatedAttributeSchema.Name != x.Name)
                .Concat(new []{updatedAttributeSchema})
                .ToDictionary(x => x.Name, x => x),
            referenceSchema.GetSortableAttributeCompounds()
        );
    }
}