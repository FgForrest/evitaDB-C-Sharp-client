using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class ModifyAssociatedDataSchemaTypeMutationConverter : ISchemaMutationConverter<
    ModifyAssociatedDataSchemaTypeMutation, GrpcModifyAssociatedDataSchemaTypeMutation>
{
    public GrpcModifyAssociatedDataSchemaTypeMutation Convert(ModifyAssociatedDataSchemaTypeMutation mutation)
    {
        return new GrpcModifyAssociatedDataSchemaTypeMutation
        {
            Name = mutation.Name,
            Type = EvitaDataTypesConverter.ToGrpcEvitaAssociatedDataDataType(mutation.Type)
        };
    }

    public ModifyAssociatedDataSchemaTypeMutation Convert(GrpcModifyAssociatedDataSchemaTypeMutation mutation)
    {
        return new ModifyAssociatedDataSchemaTypeMutation(mutation.Name,
            EvitaDataTypesConverter.ToEvitaDataType(mutation.Type));
    }
}