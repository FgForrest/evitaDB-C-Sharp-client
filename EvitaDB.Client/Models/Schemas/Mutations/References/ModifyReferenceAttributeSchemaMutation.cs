using Client.Exceptions;
using Client.Utils;

namespace Client.Models.Schemas.Mutations.References;

public class ModifyReferenceAttributeSchemaMutation : AbstractModifyReferenceDataSchemaMutation, IEntitySchemaMutation
{
    public IReferenceSchemaMutation AttributeSchemaMutation { get; }

    public ModifyReferenceAttributeSchemaMutation(string name, IReferenceSchemaMutation attributeSchemaMutation) :
        base(name)
    {
        Assert.IsTrue(attributeSchemaMutation is IAttributeSchemaMutation,
            "The mutation must implement AttributeSchemaMutation interface!");
        AttributeSchemaMutation = attributeSchemaMutation;
    }

    public override IReferenceSchema? Mutate(IEntitySchema entitySchema, IReferenceSchema? referenceSchema)
    {
        return AttributeSchemaMutation.Mutate(entitySchema, referenceSchema);
    }

    public override IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IReferenceSchema? existingReferenceSchema = entitySchema!.GetReference(Name);
        if (existingReferenceSchema is null)
        {
            // ups, the reference schema is missing
            throw new InvalidSchemaMutationException(
                "The reference `" + Name + "` is not defined in entity `" + entitySchema.Name + "` schema!"
            );
        }

        IReferenceSchema theSchema = existingReferenceSchema;
        IReferenceSchema? updatedSchema = Mutate(entitySchema, theSchema);
        Assert.IsPremiseValid(updatedSchema != null, "Updated reference schema is not expected to be null!");
        return ReplaceReferenceSchema(entitySchema, theSchema, updatedSchema!);
    }
}