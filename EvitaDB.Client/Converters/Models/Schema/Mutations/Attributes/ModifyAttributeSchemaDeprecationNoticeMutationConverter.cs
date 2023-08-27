using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class ModifyAttributeSchemaDeprecationNoticeMutationConverter : ISchemaMutationConverter<ModifyAttributeSchemaDeprecationNoticeMutation, GrpcModifyAttributeSchemaDeprecationNoticeMutation>
{
    public GrpcModifyAttributeSchemaDeprecationNoticeMutation Convert(ModifyAttributeSchemaDeprecationNoticeMutation mutation)
    {
        return new GrpcModifyAttributeSchemaDeprecationNoticeMutation
        {
            Name = mutation.Name,
            DeprecationNotice = mutation.DeprecationNotice
        };
    }

    public ModifyAttributeSchemaDeprecationNoticeMutation Convert(GrpcModifyAttributeSchemaDeprecationNoticeMutation mutation)
    {
        return new ModifyAttributeSchemaDeprecationNoticeMutation(mutation.Name, mutation.DeprecationNotice);
    }
}