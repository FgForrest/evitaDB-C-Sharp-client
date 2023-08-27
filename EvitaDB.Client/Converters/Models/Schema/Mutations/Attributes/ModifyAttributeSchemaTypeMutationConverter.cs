using Client.Converters.DataTypes;
using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class ModifyAttributeSchemaTypeMutationConverter : ISchemaMutationConverter<ModifyAttributeSchemaTypeMutation,
    GrpcModifyAttributeSchemaTypeMutation>
{
    public GrpcModifyAttributeSchemaTypeMutation Convert(ModifyAttributeSchemaTypeMutation mutation)
    {
        return new GrpcModifyAttributeSchemaTypeMutation
        {
            Name = mutation.Name,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(mutation.Type),
            IndexedDecimalPlaces = mutation.IndexedDecimalPlaces
        };
    }

    public ModifyAttributeSchemaTypeMutation Convert(GrpcModifyAttributeSchemaTypeMutation mutation)
    {
        return new ModifyAttributeSchemaTypeMutation(mutation.Name,
            EvitaDataTypesConverter.ToEvitaDataType(mutation.Type), mutation.IndexedDecimalPlaces);
    }
}