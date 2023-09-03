using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaDescriptionMutation : AbstractModifyAssociatedDataSchemaMutation, IEntitySchemaMutation
{
    public string? Description { get; }
    
    public ModifyAssociatedDataSchemaDescriptionMutation(string name, string? description) : base(name)
    {
        Description = description;
    }
    public override IEntitySchema? Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
    {
        Assert.IsPremiseValid(entitySchema != null, "Entity schema is mandatory!");
        IAssociatedDataSchema? existingAssociatedDataSchema = entitySchema!.GetAssociatedData(Name);
        if (existingAssociatedDataSchema is null) {
            // ups, the associated data is missing
            throw new InvalidSchemaMutationException(
                "The associated data `" + Name + "` is not defined in entity `" + entitySchema.Name + "` schema!"
            );
        }

        IAssociatedDataSchema theSchema = existingAssociatedDataSchema;
        IAssociatedDataSchema? updatedAssociatedDataSchema = Mutate(theSchema);
        return ReplaceAssociatedDataIfDifferent(entitySchema, theSchema, updatedAssociatedDataSchema);
    }

    public override IAssociatedDataSchema? Mutate(IAssociatedDataSchema? associatedDataSchema)
    {
        Assert.IsPremiseValid(associatedDataSchema != null, "Associated data schema is mandatory!");
        return AssociatedDataSchema.InternalBuild(
            associatedDataSchema!.Name,
            Description,
            associatedDataSchema.DeprecationNotice,
            associatedDataSchema.Localized,
            associatedDataSchema.Nullable,
            associatedDataSchema.Type
        );
    }
}