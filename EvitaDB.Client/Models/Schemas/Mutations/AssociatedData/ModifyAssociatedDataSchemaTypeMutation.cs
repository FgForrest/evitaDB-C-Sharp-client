﻿using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaTypeMutation : AbstractModifyAssociatedDataSchemaMutation, IEntitySchemaMutation
{
    public Type Type { get; }
    
    public ModifyAssociatedDataSchemaTypeMutation(string name, Type type) : base(name)
    {
        Type = type;
    }
    public override IEntitySchema Mutate(ICatalogSchema catalogSchema, IEntitySchema? entitySchema)
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
        IAssociatedDataSchema updatedAssociatedDataSchema = Mutate(theSchema);
        return ReplaceAssociatedDataIfDifferent(entitySchema, theSchema, updatedAssociatedDataSchema);
    }

    public override IAssociatedDataSchema Mutate(IAssociatedDataSchema? associatedDataSchema)
    {
        Assert.IsPremiseValid(associatedDataSchema != null, "Associated data schema is mandatory!");
        return AssociatedDataSchema.InternalBuild(
            Name,
            associatedDataSchema!.Description,
            associatedDataSchema.DeprecationNotice,
            associatedDataSchema.Localized(),
            associatedDataSchema.Nullable(),
            Type
        );
    }
}
