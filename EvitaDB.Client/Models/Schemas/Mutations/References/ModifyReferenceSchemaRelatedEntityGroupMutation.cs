using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.References;

public class ModifyReferenceSchemaRelatedEntityGroupMutation : AbstractModifyReferenceDataSchemaMutation, IEntitySchemaMutation
{
    public string? ReferencedGroupType { get; }
    public bool ReferencedGroupTypeManaged { get; }

    public ModifyReferenceSchemaRelatedEntityGroupMutation(string name, string? referencedGroupType, bool referencedGroupTypeManaged) : base(name)
    {
        ReferencedGroupType = referencedGroupType;
        ReferencedGroupTypeManaged = referencedGroupTypeManaged;
    }

    public override IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        return ReferenceSchema.InternalBuild(
            Name,
            referenceSchema!.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            referenceSchema.ReferencedEntityType,
            referenceSchema.ReferencedEntityTypeManaged ? new Dictionary<NamingConvention, string?>() : referenceSchema.GetEntityTypeNameVariants(_ => null!),
            referenceSchema.ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            ReferencedGroupType,
            ReferencedGroupTypeManaged || ReferencedGroupType is null ? new Dictionary<NamingConvention, string?>() : NamingConventionHelper.Generate(ReferencedGroupType),
            ReferencedGroupTypeManaged,
            referenceSchema.IsIndexed,
            referenceSchema.IsFaceted,
            referenceSchema.GetAttributes(),
            referenceSchema.GetSortableAttributeCompounds()
        );
    }

    public override IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
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
        IReferenceSchema updatedReferenceSchema = Mutate(entitySchema, theSchema);
        return ReplaceReferenceSchema(entitySchema, theSchema, updatedReferenceSchema);
    }
}