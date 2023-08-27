using Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

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