using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.References;

public class SetReferenceSchemaFacetedMutation : AbstractModifyReferenceDataSchemaMutation, IEntitySchemaMutation
{
    public bool Faceted { get; }

    public SetReferenceSchemaFacetedMutation(string name, bool faceted) : base(name)
    {
        Faceted = faceted;
    }

    public override IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        if (referenceSchema!.Faceted == Faceted)
        {
            return referenceSchema;
        }

        return ReferenceSchema.InternalBuild(
            Name,
            referenceSchema.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.ReferencedEntityTypeManaged
                ? new Dictionary<NamingConvention, string>()
                : referenceSchema.GetEntityTypeNameVariants(_ => null),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.ReferencedGroupTypeManaged
                ? new Dictionary<NamingConvention, string>()
                : referenceSchema.GetGroupTypeNameVariants(_ => null),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.Indexed,
            Faceted,
            referenceSchema.GetAttributes(),
            referenceSchema.GetSortableAttributeCompounds()
        );
    }

    public override IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IReferenceSchema? existingReferenceSchema = entitySchema!.GetReference(Name);
        if (existingReferenceSchema is null)
        {
            // ups, the associated data is missing
            throw new InvalidSchemaMutationException(
                "The reference `" + Name + "` is not defined in entity `" + entitySchema.Name + "` schema!"
            );
        }

        IReferenceSchema theSchema = existingReferenceSchema;
        IReferenceSchema? updatedReferenceSchema = Mutate(entitySchema, theSchema);
        return ReplaceReferenceSchema(entitySchema, theSchema, updatedReferenceSchema);
    }
}