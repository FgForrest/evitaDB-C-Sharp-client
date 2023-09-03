using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSchemaCardinalityMutationConverter : ISchemaMutationConverter<ModifyReferenceSchemaCardinalityMutation, GrpcModifyReferenceSchemaCardinalityMutation>
{
    public GrpcModifyReferenceSchemaCardinalityMutation Convert(ModifyReferenceSchemaCardinalityMutation mutation)
    {
        return new GrpcModifyReferenceSchemaCardinalityMutation
        {
            Name = mutation.Name,
            Cardinality = EvitaEnumConverter.ToGrpcCardinality(mutation.Cardinality)
        };
    }

    public ModifyReferenceSchemaCardinalityMutation Convert(GrpcModifyReferenceSchemaCardinalityMutation mutation)
    {
        return new ModifyReferenceSchemaCardinalityMutation(mutation.Name,
            EvitaEnumConverter.ToCardinality(mutation.Cardinality)!.Value);
    }
}