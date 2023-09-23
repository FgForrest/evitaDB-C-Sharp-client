using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

public class ModifySortableAttributeCompoundSchemaDeprecationNoticeMutationConverter : ISchemaMutationConverter<ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation, GrpcModifySortableAttributeCompoundSchemaDeprecationNoticeMutation>
{
    public GrpcModifySortableAttributeCompoundSchemaDeprecationNoticeMutation Convert(
        ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation mutation)
    {
        return new GrpcModifySortableAttributeCompoundSchemaDeprecationNoticeMutation
        {
            Name = mutation.Name,
            DeprecationNotice = mutation.DeprecationNotice
        };
    }

    public ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation Convert(
        GrpcModifySortableAttributeCompoundSchemaDeprecationNoticeMutation mutation)
    {
        return new ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation(mutation.Name,
            mutation.DeprecationNotice);
    }
}