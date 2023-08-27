using Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaDeprecationNoticeMutationConverter : ISchemaMutationConverter<ModifyAssociatedDataSchemaDeprecationNoticeMutation, GrpcModifyAssociatedDataSchemaDeprecationNoticeMutation>
{
    public GrpcModifyAssociatedDataSchemaDeprecationNoticeMutation Convert(
        ModifyAssociatedDataSchemaDeprecationNoticeMutation mutation)
    {
        return new GrpcModifyAssociatedDataSchemaDeprecationNoticeMutation
        {
            Name = mutation.Name,
            DeprecationNotice = mutation.DeprecationNotice
        };
    }

    public ModifyAssociatedDataSchemaDeprecationNoticeMutation Convert(
        GrpcModifyAssociatedDataSchemaDeprecationNoticeMutation mutation)
    {
        return new ModifyAssociatedDataSchemaDeprecationNoticeMutation(mutation.Name, mutation.DeprecationNotice);
    }
}