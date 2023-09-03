using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;

public class ModifyEntitySchemaDeprecationNoticeMutationConverter : ISchemaMutationConverter<ModifyEntitySchemaDeprecationNoticeMutation, GrpcModifyEntitySchemaDeprecationNoticeMutation>
{
    public GrpcModifyEntitySchemaDeprecationNoticeMutation Convert(ModifyEntitySchemaDeprecationNoticeMutation mutation)
    {
        return new GrpcModifyEntitySchemaDeprecationNoticeMutation
        {
            DeprecationNotice = mutation.DeprecationNotice
        };
    }

    public ModifyEntitySchemaDeprecationNoticeMutation Convert(GrpcModifyEntitySchemaDeprecationNoticeMutation mutation)
    {
        return new ModifyEntitySchemaDeprecationNoticeMutation(mutation.DeprecationNotice);
    }
}