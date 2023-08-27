using Client.Converters.DataTypes;
using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.Attributes;

public class ModifyAttributeSchemaDefaultValueMutationConverter : ISchemaMutationConverter<ModifyAttributeSchemaDefaultValueMutation, GrpcModifyAttributeSchemaDefaultValueMutation>
{
    public GrpcModifyAttributeSchemaDefaultValueMutation Convert(ModifyAttributeSchemaDefaultValueMutation mutation)
    {
        return new GrpcModifyAttributeSchemaDefaultValueMutation
        {
            Name = mutation.Name,
            DefaultValue = EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.DefaultValue)
        };
    }

    public ModifyAttributeSchemaDefaultValueMutation Convert(GrpcModifyAttributeSchemaDefaultValueMutation mutation)
    {
        return new ModifyAttributeSchemaDefaultValueMutation(mutation.Name,
            EvitaDataTypesConverter.ToEvitaValue(mutation.DefaultValue));
    }
}