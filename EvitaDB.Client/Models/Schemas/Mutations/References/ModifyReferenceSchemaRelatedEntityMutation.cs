using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.References;

public class ModifyReferenceSchemaRelatedEntityMutation : AbstractModifyReferenceDataSchemaMutation, IEntitySchemaMutation
{
    public string ReferencedEntityType { get; }
    public bool ReferencedEntityTypeManaged { get; }

    public ModifyReferenceSchemaRelatedEntityMutation(string name, string referencedEntityType, bool referencedEntityTypeManaged) : base(name)
    {
        ReferencedEntityType = referencedEntityType;
        ReferencedEntityTypeManaged = referencedEntityTypeManaged;
    }

    public override IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        return ReferenceSchema.InternalBuild(
            Name,
            referenceSchema!.NameVariants,
            referenceSchema.Description,
            referenceSchema.DeprecationNotice,
            ReferencedEntityType,
            ReferencedEntityTypeManaged ? new Dictionary<NamingConvention, string>() : NamingConventionHelper.Generate(ReferencedEntityType),
            ReferencedEntityTypeManaged,
            referenceSchema.Cardinality,
            referenceSchema.ReferencedGroupType,
            referenceSchema.ReferencedGroupTypeManaged ? new Dictionary<NamingConvention, string>() : referenceSchema.GetGroupTypeNameVariants(_ => null),
            referenceSchema.ReferencedGroupTypeManaged,
            referenceSchema.Indexed,
            referenceSchema.Faceted,
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