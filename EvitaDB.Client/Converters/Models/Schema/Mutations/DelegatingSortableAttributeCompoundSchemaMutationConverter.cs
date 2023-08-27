using Client.Converters.Models.Schema.Mutations.SortableAttributeCompounds;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.SortableAttributeCompounds;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations;

public class DelegatingSortableAttributeCompoundSchemaMutationConverter : ISchemaMutationConverter<
    ISortableAttributeCompoundSchemaMutation, GrpcSortableAttributeCompoundSchemaMutation>
{
    public GrpcSortableAttributeCompoundSchemaMutation Convert(ISortableAttributeCompoundSchemaMutation mutation)
    {
        GrpcSortableAttributeCompoundSchemaMutation grpcSortableAttributeCompoundSchemaMutation = new();

        switch (mutation)
        {
            case CreateSortableAttributeCompoundSchemaMutation createSortableAttributeCompoundSchemaMutation:
                grpcSortableAttributeCompoundSchemaMutation.CreateSortableAttributeCompoundSchemaMutation =
                    new CreateSortableAttributeCompoundSchemaMutationConverter().Convert(
                        createSortableAttributeCompoundSchemaMutation);
                break;
            case ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation
                modifySortableAttributeCompoundSchemaDeprecationNoticeMutation:
                grpcSortableAttributeCompoundSchemaMutation
                        .ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation =
                    new ModifySortableAttributeCompoundSchemaDeprecationNoticeMutationConverter().Convert(
                        modifySortableAttributeCompoundSchemaDeprecationNoticeMutation);
                break;
            case ModifySortableAttributeCompoundSchemaDescriptionMutation
                modifySortableAttributeCompoundSchemaDescriptionMutation:
                grpcSortableAttributeCompoundSchemaMutation.ModifySortableAttributeCompoundSchemaDescriptionMutation =
                    new ModifySortableAttributeCompoundSchemaDescriptionMutationConverter().Convert(
                        modifySortableAttributeCompoundSchemaDescriptionMutation);
                break;
            case ModifySortableAttributeCompoundSchemaNameMutation modifySortableAttributeCompoundSchemaNameMutation:
                grpcSortableAttributeCompoundSchemaMutation.ModifySortableAttributeCompoundSchemaNameMutation =
                    new ModifySortableAttributeCompoundSchemaNameMutationConverter().Convert(
                        modifySortableAttributeCompoundSchemaNameMutation);
                break;
            case RemoveSortableAttributeCompoundSchemaMutation removeSortableAttributeCompoundSchemaMutation:
                grpcSortableAttributeCompoundSchemaMutation.RemoveSortableAttributeCompoundSchemaMutation =
                    new RemoveSortableAttributeCompoundSchemaMutationConverter().Convert(
                        removeSortableAttributeCompoundSchemaMutation);
                break;
        }

        return grpcSortableAttributeCompoundSchemaMutation;
    }

    public ISortableAttributeCompoundSchemaMutation Convert(GrpcSortableAttributeCompoundSchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcSortableAttributeCompoundSchemaMutation.MutationOneofCase.CreateSortableAttributeCompoundSchemaMutation =>
                new CreateSortableAttributeCompoundSchemaMutationConverter().Convert(
                    mutation.CreateSortableAttributeCompoundSchemaMutation),
            GrpcSortableAttributeCompoundSchemaMutation.MutationOneofCase
                .ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation =>
                new ModifySortableAttributeCompoundSchemaDeprecationNoticeMutationConverter().Convert(
                    mutation.ModifySortableAttributeCompoundSchemaDeprecationNoticeMutation),
            GrpcSortableAttributeCompoundSchemaMutation.MutationOneofCase
                .ModifySortableAttributeCompoundSchemaDescriptionMutation =>
                new ModifySortableAttributeCompoundSchemaDescriptionMutationConverter().Convert(
                    mutation.ModifySortableAttributeCompoundSchemaDescriptionMutation),
            GrpcSortableAttributeCompoundSchemaMutation.MutationOneofCase
                .ModifySortableAttributeCompoundSchemaNameMutation =>
                new ModifySortableAttributeCompoundSchemaNameMutationConverter().Convert(
                    mutation.ModifySortableAttributeCompoundSchemaNameMutation),
            GrpcSortableAttributeCompoundSchemaMutation.MutationOneofCase.RemoveSortableAttributeCompoundSchemaMutation =>
                new RemoveSortableAttributeCompoundSchemaMutationConverter().Convert(
                    mutation.RemoveSortableAttributeCompoundSchemaMutation),
            _ => throw new NotImplementedException()
        };
    }
}