using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class ModifyReferenceSortableAttributeCompoundSchemaMutationConverter : ISchemaMutationConverter<
    ModifyReferenceSortableAttributeCompoundSchemaMutation, GrpcModifyReferenceSortableAttributeCompoundSchemaMutation>
{
    private static readonly DelegatingSortableAttributeCompoundSchemaMutationConverter
        SortableAttributeCompoundSchemaMutationConverter =
            new DelegatingSortableAttributeCompoundSchemaMutationConverter();

    public GrpcModifyReferenceSortableAttributeCompoundSchemaMutation Convert(
        ModifyReferenceSortableAttributeCompoundSchemaMutation mutation)
    {
        return new GrpcModifyReferenceSortableAttributeCompoundSchemaMutation
        {
            Name = mutation.Name,
            SortableAttributeCompoundSchemaMutation =
                SortableAttributeCompoundSchemaMutationConverter.Convert(
                    (ISortableAttributeCompoundSchemaMutation) mutation.SortableAttributeCompoundSchemaMutation)
        };
    }

    public ModifyReferenceSortableAttributeCompoundSchemaMutation Convert(
        GrpcModifyReferenceSortableAttributeCompoundSchemaMutation mutation)
    {
        return new ModifyReferenceSortableAttributeCompoundSchemaMutation(mutation.Name,
            (IReferenceSchemaMutation) SortableAttributeCompoundSchemaMutationConverter.Convert(
                mutation.SortableAttributeCompoundSchemaMutation));
    }
}