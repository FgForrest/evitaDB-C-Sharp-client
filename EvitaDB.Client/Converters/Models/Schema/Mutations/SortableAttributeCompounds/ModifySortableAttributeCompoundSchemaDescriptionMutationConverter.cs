using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

public class ModifySortableAttributeCompoundSchemaDescriptionMutationConverter : ISchemaMutationConverter<ModifySortableAttributeCompoundSchemaDescriptionMutation, GrpcModifySortableAttributeCompoundSchemaDescriptionMutation>
{
    public GrpcModifySortableAttributeCompoundSchemaDescriptionMutation Convert(
        ModifySortableAttributeCompoundSchemaDescriptionMutation mutation)
    {
        return new GrpcModifySortableAttributeCompoundSchemaDescriptionMutation
        {
            Name = mutation.Name,
            Description = mutation.Description
        };
    }

    public ModifySortableAttributeCompoundSchemaDescriptionMutation Convert(
        GrpcModifySortableAttributeCompoundSchemaDescriptionMutation mutation)
    {
        return new ModifySortableAttributeCompoundSchemaDescriptionMutation(mutation.Name, mutation.Description);
    }
}