using Client.Converters.Models.Schema.Mutations.Attributes;
using Client.Models.Schemas.Mutations;
using Client.Models.Schemas.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations;

public class DelegatingAttributeSchemaMutationConverter : ISchemaMutationConverter<IAttributeSchemaMutation, GrpcAttributeSchemaMutation>
{
    public GrpcAttributeSchemaMutation Convert(IAttributeSchemaMutation mutation)
    {
        GrpcAttributeSchemaMutation grpcAttributeSchemaMutation = new();

        switch (mutation)
        {
            case CreateAttributeSchemaMutation createAttributeSchemaMutation:
                grpcAttributeSchemaMutation.CreateAttributeSchemaMutation = new CreateAttributeSchemaMutationConverter().Convert(createAttributeSchemaMutation);
                break;
            case ModifyAttributeSchemaDefaultValueMutation modifyAttributeSchemaDefaultValueMutation:
                grpcAttributeSchemaMutation.ModifyAttributeSchemaDefaultValueMutation = new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(modifyAttributeSchemaDefaultValueMutation);
                break;
            case ModifyAttributeSchemaDeprecationNoticeMutation modifyAttributeSchemaDeprecationNoticeMutation:
                grpcAttributeSchemaMutation.ModifyAttributeSchemaDeprecationNoticeMutation = new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(modifyAttributeSchemaDeprecationNoticeMutation);
                break;
            case ModifyAttributeSchemaDescriptionMutation modifyAttributeSchemaDescriptionMutation:
                grpcAttributeSchemaMutation.ModifyAttributeSchemaDescriptionMutation = new ModifyAttributeSchemaDescriptionMutationConverter().Convert(modifyAttributeSchemaDescriptionMutation);
                break;
            case ModifyAttributeSchemaNameMutation modifyAttributeSchemaNameMutation:
                grpcAttributeSchemaMutation.ModifyAttributeSchemaNameMutation = new ModifyAttributeSchemaNameMutationConverter().Convert(modifyAttributeSchemaNameMutation);
                break;
            case ModifyAttributeSchemaTypeMutation modifyAttributeSchemaTypeMutation:
                grpcAttributeSchemaMutation.ModifyAttributeSchemaTypeMutation = new ModifyAttributeSchemaTypeMutationConverter().Convert(modifyAttributeSchemaTypeMutation);
                break;
            case RemoveAttributeSchemaMutation removeAttributeSchemaMutation:
                grpcAttributeSchemaMutation.RemoveAttributeSchemaMutation = new RemoveAttributeSchemaMutationConverter().Convert(removeAttributeSchemaMutation);
                break;
            case SetAttributeSchemaFilterableMutation setAttributeSchemaFilterableMutation:
                grpcAttributeSchemaMutation.SetAttributeSchemaFilterableMutation = new SetAttributeSchemaFilterableMutationConverter().Convert(setAttributeSchemaFilterableMutation);
                break;
            case SetAttributeSchemaLocalizedMutation setAttributeSchemaLocalizedMutation:
                grpcAttributeSchemaMutation.SetAttributeSchemaLocalizedMutation = new SetAttributeSchemaLocalizedMutationConverter().Convert(setAttributeSchemaLocalizedMutation);
                break;
            case SetAttributeSchemaNullableMutation setAttributeSchemaNullableMutation:
                grpcAttributeSchemaMutation.SetAttributeSchemaNullableMutation = new SetAttributeSchemaNullableMutationConverter().Convert(setAttributeSchemaNullableMutation);
                break;
            case SetAttributeSchemaSortableMutation setAttributeSchemaSortableMutation:
                grpcAttributeSchemaMutation.SetAttributeSchemaSortableMutation = new SetAttributeSchemaSortableMutationConverter().Convert(setAttributeSchemaSortableMutation);
                break;
            case SetAttributeSchemaUniqueMutation setAttributeSchemaUniqueMutation:
                grpcAttributeSchemaMutation.SetAttributeSchemaUniqueMutation = new SetAttributeSchemaUniqueMutationConverter().Convert(setAttributeSchemaUniqueMutation);
                break;
            case UseGlobalAttributeSchemaMutation useGlobalAttributeSchemaMutation:
                grpcAttributeSchemaMutation.UseGlobalAttributeSchemaMutation = new UseGlobalAttributeSchemaMutationConverter().Convert(useGlobalAttributeSchemaMutation);
                break;
        }
        
        return grpcAttributeSchemaMutation;
    }

    public IAttributeSchemaMutation Convert(GrpcAttributeSchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcAttributeSchemaMutation.MutationOneofCase.CreateAttributeSchemaMutation => new CreateAttributeSchemaMutationConverter().Convert(mutation.CreateAttributeSchemaMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDefaultValueMutation => new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(mutation.ModifyAttributeSchemaDefaultValueMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDeprecationNoticeMutation => new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyAttributeSchemaDeprecationNoticeMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDescriptionMutation => new ModifyAttributeSchemaDescriptionMutationConverter().Convert(mutation.ModifyAttributeSchemaDescriptionMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.ModifyAttributeSchemaNameMutation => new ModifyAttributeSchemaNameMutationConverter().Convert(mutation.ModifyAttributeSchemaNameMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.ModifyAttributeSchemaTypeMutation => new ModifyAttributeSchemaTypeMutationConverter().Convert(mutation.ModifyAttributeSchemaTypeMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.RemoveAttributeSchemaMutation => new RemoveAttributeSchemaMutationConverter().Convert(mutation.RemoveAttributeSchemaMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.SetAttributeSchemaFilterableMutation => new SetAttributeSchemaFilterableMutationConverter().Convert(mutation.SetAttributeSchemaFilterableMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.SetAttributeSchemaLocalizedMutation => new SetAttributeSchemaLocalizedMutationConverter().Convert(mutation.SetAttributeSchemaLocalizedMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.SetAttributeSchemaNullableMutation => new SetAttributeSchemaNullableMutationConverter().Convert(mutation.SetAttributeSchemaNullableMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.SetAttributeSchemaSortableMutation => new SetAttributeSchemaSortableMutationConverter().Convert(mutation.SetAttributeSchemaSortableMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.SetAttributeSchemaUniqueMutation => new SetAttributeSchemaUniqueMutationConverter().Convert(mutation.SetAttributeSchemaUniqueMutation),
            GrpcAttributeSchemaMutation.MutationOneofCase.UseGlobalAttributeSchemaMutation => new UseGlobalAttributeSchemaMutationConverter().Convert(mutation.UseGlobalAttributeSchemaMutation),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}