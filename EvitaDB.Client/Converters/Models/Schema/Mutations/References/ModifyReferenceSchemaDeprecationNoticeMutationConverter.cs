using Client.Models.Schemas.Mutations.References;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaDeprecationNoticeMutationConverter : ISchemaMutationConverter<ModifyReferenceSchemaDeprecationNoticeMutation, GrpcModifyReferenceSchemaDeprecationNoticeMutation>
{
    public GrpcModifyReferenceSchemaDeprecationNoticeMutation Convert(ModifyReferenceSchemaDeprecationNoticeMutation mutation)
    {
        return new GrpcModifyReferenceSchemaDeprecationNoticeMutation
        {
            Name = mutation.Name,
            DeprecationNotice = mutation.DeprecationNotice
        };
    }

    public ModifyReferenceSchemaDeprecationNoticeMutation Convert(GrpcModifyReferenceSchemaDeprecationNoticeMutation mutation)
    {
        return new ModifyReferenceSchemaDeprecationNoticeMutation(mutation.Name, mutation.DeprecationNotice);
    }
}