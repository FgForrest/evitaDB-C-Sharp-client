using Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

public class RemoveSortableAttributeCompoundSchemaMutationConverter : ISchemaMutationConverter<RemoveSortableAttributeCompoundSchemaMutation, GrpcRemoveSortableAttributeCompoundSchemaMutation>
{
    public GrpcRemoveSortableAttributeCompoundSchemaMutation Convert(RemoveSortableAttributeCompoundSchemaMutation mutation)
    {
        return new GrpcRemoveSortableAttributeCompoundSchemaMutation
        {
            Name = mutation.Name
        };
    }

    public RemoveSortableAttributeCompoundSchemaMutation Convert(GrpcRemoveSortableAttributeCompoundSchemaMutation mutation)
    {
        return new RemoveSortableAttributeCompoundSchemaMutation(mutation.Name);
    }
}