using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.References;

public class SetReferenceSchemaIndexedMutation : AbstractModifyReferenceDataSchemaMutation, IEntitySchemaMutation
{
    public bool Indexed { get; }

    public SetReferenceSchemaIndexedMutation(string name, bool indexed) : base(name)
    {
        Indexed = indexed;
    }

    public override IReferenceSchema Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        Assert.IsPremiseValid(referenceSchema != null, "Reference schema is mandatory!");
        if (referenceSchema!.IsIndexed == Indexed)
        {
            return referenceSchema;
        }

        if (!Indexed)
        {
            VerifyNoAttributeRequiresIndex(entitySchema, referenceSchema);
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
            Indexed,
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

    private static void VerifyNoAttributeRequiresIndex(IEntitySchema entitySchema, IReferenceSchema referenceSchema)
    {
        foreach (IAttributeSchema attributeSchema in referenceSchema.GetAttributes().Values)
        {
            if (attributeSchema.Filterable || attributeSchema.Unique || attributeSchema.Sortable)
            {
                string type;
                if (attributeSchema.Filterable)
                {
                    type = "filterable";
                }
                else if (attributeSchema.Unique)
                {
                    type = "unique";
                }
                else
                {
                    type = "sortable";
                }

                throw new InvalidSchemaMutationException(
                    "Cannot make reference schema `" + referenceSchema.Name + "` of entity `" + entitySchema.Name +
                    "` " +
                    "non-indexed if there is a single " + type + " attribute! Found " + type + " attribute " +
                    "definition `" + attributeSchema.Name + "`."
                );
            }
        }
    }
}