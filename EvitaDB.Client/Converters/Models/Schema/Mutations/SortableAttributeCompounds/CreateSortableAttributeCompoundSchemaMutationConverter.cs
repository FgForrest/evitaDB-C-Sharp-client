using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.SortableAttributeCompounds;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;

public class CreateSortableAttributeCompoundSchemaMutationConverter : ISchemaMutationConverter<
    CreateSortableAttributeCompoundSchemaMutation, GrpcCreateSortableAttributeCompoundSchemaMutation>
{
    public GrpcCreateSortableAttributeCompoundSchemaMutation Convert(
        CreateSortableAttributeCompoundSchemaMutation mutation)
    {
        return new GrpcCreateSortableAttributeCompoundSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            AttributeElements = {EntitySchemaConverter.ToGrpcAttributeElement(mutation.AttributeElements).ToArray()}
        };
    }

    public CreateSortableAttributeCompoundSchemaMutation Convert(
        GrpcCreateSortableAttributeCompoundSchemaMutation mutation)
    {
        return new CreateSortableAttributeCompoundSchemaMutation(mutation.Name, mutation.Description,
            mutation.DeprecationNotice, EntitySchemaConverter.ToAttributeElement(mutation.AttributeElements).ToArray());
    }
}