using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class UseGlobalAttributeSchemaMutationConverter : ISchemaMutationConverter<UseGlobalAttributeSchemaMutation, GrpcUseGlobalAttributeSchemaMutation>
{
    public GrpcUseGlobalAttributeSchemaMutation Convert(UseGlobalAttributeSchemaMutation mutation)
    {
        return new GrpcUseGlobalAttributeSchemaMutation
        {
            Name = mutation.Name
        };
    }

    public UseGlobalAttributeSchemaMutation Convert(GrpcUseGlobalAttributeSchemaMutation mutation)
    {
        return new UseGlobalAttributeSchemaMutation(mutation.Name);
    }
}