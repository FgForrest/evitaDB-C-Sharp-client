using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceAttributeSchemaMutationConverter : ISchemaMutationConverter<ModifyReferenceAttributeSchemaMutation, GrpcModifyReferenceAttributeSchemaMutation>
{
    private static readonly DelegatingAttributeSchemaMutationConverter AttributeSchemaMutationConverter = new DelegatingAttributeSchemaMutationConverter();

    public GrpcModifyReferenceAttributeSchemaMutation Convert(ModifyReferenceAttributeSchemaMutation mutation)
    {
        return new GrpcModifyReferenceAttributeSchemaMutation
        {
            Name = mutation.Name,
            AttributeSchemaMutation = AttributeSchemaMutationConverter.Convert((IAttributeSchemaMutation)mutation.AttributeSchemaMutation)
        };
    }

    public ModifyReferenceAttributeSchemaMutation Convert(GrpcModifyReferenceAttributeSchemaMutation mutation)
    {
        return new ModifyReferenceAttributeSchemaMutation(mutation.Name,
            (IReferenceSchemaMutation) AttributeSchemaMutationConverter.Convert(mutation.AttributeSchemaMutation));
    }
}