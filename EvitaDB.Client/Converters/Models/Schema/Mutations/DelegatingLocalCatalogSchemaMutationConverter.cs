using EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations;

public class DelegatingLocalCatalogSchemaMutationConverter : ISchemaMutationConverter<ILocalCatalogSchemaMutation, GrpcLocalCatalogSchemaMutation>
{
    
    public GrpcLocalCatalogSchemaMutation Convert(ILocalCatalogSchemaMutation mutation)
    {
        GrpcLocalCatalogSchemaMutation grpcLocalCatalogSchemaMutation = new();
        switch (mutation)
        {
            // catalogs
            case ModifyCatalogSchemaDescriptionMutation modifyCatalogSchemaDescriptionMutation: 
                grpcLocalCatalogSchemaMutation.ModifyCatalogSchemaDescriptionMutation = new ModifyCatalogSchemaDescriptionMutationConverter().Convert(modifyCatalogSchemaDescriptionMutation);
                break;
            case AllowEvolutionModeInCatalogSchemaMutation allowEvolutionModeInCatalogSchemaMutation: 
                grpcLocalCatalogSchemaMutation.AllowEvolutionModeInCatalogSchemaMutation = new AllowEvolutionModeInCatalogSchemaMutationConverter().Convert(allowEvolutionModeInCatalogSchemaMutation);
                break;
            case DisallowEvolutionModeInCatalogSchemaMutation disallowEvolutionModeInCatalogSchemaMutation:
                grpcLocalCatalogSchemaMutation.DisallowEvolutionModeInCatalogSchemaMutation = new DisallowEvolutionModeInCatalogSchemaMutationConverter().Convert(disallowEvolutionModeInCatalogSchemaMutation);
                break;
            
            // global attributes
            case CreateGlobalAttributeSchemaMutation createGlobalAttributeSchemaMutation: 
                grpcLocalCatalogSchemaMutation.CreateGlobalAttributeSchemaMutation = new CreateGlobalAttributeSchemaMutationConverter().Convert(createGlobalAttributeSchemaMutation);
                break;
            case ModifyAttributeSchemaDefaultValueMutation modifyGlobalAttributeSchemaDefaultValueMutation:
                grpcLocalCatalogSchemaMutation.ModifyAttributeSchemaDefaultValueMutation = new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(modifyGlobalAttributeSchemaDefaultValueMutation);
                break;
            case ModifyAttributeSchemaDeprecationNoticeMutation modifyAttributeSchemaDeprecationNoticeMutation:
                grpcLocalCatalogSchemaMutation.ModifyAttributeSchemaDeprecationNoticeMutation = new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(modifyAttributeSchemaDeprecationNoticeMutation);
                break;
            case ModifyAttributeSchemaDescriptionMutation modifyAttributeSchemaDescriptionMutation:
                grpcLocalCatalogSchemaMutation.ModifyAttributeSchemaDescriptionMutation = new ModifyAttributeSchemaDescriptionMutationConverter().Convert(modifyAttributeSchemaDescriptionMutation);
                break;
            case ModifyAttributeSchemaNameMutation modifyAttributeSchemaNameMutation:
                grpcLocalCatalogSchemaMutation.ModifyAttributeSchemaNameMutation = new ModifyAttributeSchemaNameMutationConverter().Convert(modifyAttributeSchemaNameMutation);
                break;
            case ModifyAttributeSchemaTypeMutation modifyAttributeSchemaTypeMutation:
                grpcLocalCatalogSchemaMutation.ModifyAttributeSchemaTypeMutation = new ModifyAttributeSchemaTypeMutationConverter().Convert(modifyAttributeSchemaTypeMutation);
                break;
            case RemoveAttributeSchemaMutation removeAttributeSchemaMutation:
                grpcLocalCatalogSchemaMutation.RemoveAttributeSchemaMutation = new RemoveAttributeSchemaMutationConverter().Convert(removeAttributeSchemaMutation);
                break;
            case SetAttributeSchemaFilterableMutation setAttributeSchemaFilterableMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaFilterableMutation = new SetAttributeSchemaFilterableMutationConverter().Convert(setAttributeSchemaFilterableMutation);
                break;
            case SetAttributeSchemaGloballyUniqueMutation setAttributeSchemaGloballyUniqueMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaGloballyUniqueMutation = new SetAttributeSchemaGloballyUniqueMutationConverter().Convert(setAttributeSchemaGloballyUniqueMutation);
                break;
            case SetAttributeSchemaLocalizedMutation setAttributeSchemaLocalizedMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaLocalizedMutation = new SetAttributeSchemaLocalizedMutationConverter().Convert(setAttributeSchemaLocalizedMutation);
                break;
            case SetAttributeSchemaNullableMutation setAttributeSchemaNullableMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaNullableMutation = new SetAttributeSchemaNullableMutationConverter().Convert(setAttributeSchemaNullableMutation);
                break;
            case SetAttributeSchemaRepresentativeMutation setAttributeSchemaRepresentativeMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaRepresentativeMutation = new SetAttributeSchemaRepresentativeMutationConverter().Convert(setAttributeSchemaRepresentativeMutation);
                break;
            case SetAttributeSchemaSortableMutation setAttributeSchemaSortableMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaSortableMutation = new SetAttributeSchemaSortableMutationConverter().Convert(setAttributeSchemaSortableMutation);
                break;
            case SetAttributeSchemaUniqueMutation setAttributeSchemaUniqueMutation:
                grpcLocalCatalogSchemaMutation.SetAttributeSchemaUniqueMutation = new SetAttributeSchemaUniqueMutationConverter().Convert(setAttributeSchemaUniqueMutation);
                break;
                
            
            // entities
            case CreateEntitySchemaMutation createEntitySchemaMutation: 
                grpcLocalCatalogSchemaMutation.CreateEntitySchemaMutation = new CreateEntitySchemaMutationConverter().Convert(createEntitySchemaMutation);
                break;
            case ModifyEntitySchemaMutation modifyEntitySchemaMutation: 
                grpcLocalCatalogSchemaMutation.ModifyEntitySchemaMutation = new ModifyEntitySchemaMutationConverter().Convert(modifyEntitySchemaMutation);
                break;
            case ModifyEntitySchemaNameMutation modifyEntitySchemaNameMutation: 
                grpcLocalCatalogSchemaMutation.ModifyEntitySchemaNameMutation = new ModifyEntitySchemaNameMutationConverter().Convert(modifyEntitySchemaNameMutation);
                break;
            case RemoveEntitySchemaMutation removeEntitySchemaMutation: 
                grpcLocalCatalogSchemaMutation.RemoveEntitySchemaMutation = new RemoveEntitySchemaMutationConverter().Convert(removeEntitySchemaMutation);
                break;
            default:
                throw new EvitaInternalError("This should never happen!");
        }
        return grpcLocalCatalogSchemaMutation;
    }

    public ILocalCatalogSchemaMutation Convert(GrpcLocalCatalogSchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyCatalogSchemaDescriptionMutation => new ModifyCatalogSchemaDescriptionMutationConverter().Convert(mutation.ModifyCatalogSchemaDescriptionMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.AllowEvolutionModeInCatalogSchemaMutation => new AllowEvolutionModeInCatalogSchemaMutationConverter().Convert(mutation.AllowEvolutionModeInCatalogSchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.DisallowEvolutionModeInCatalogSchemaMutation => new DisallowEvolutionModeInCatalogSchemaMutationConverter().Convert(mutation.DisallowEvolutionModeInCatalogSchemaMutation),
            
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.CreateGlobalAttributeSchemaMutation => new CreateGlobalAttributeSchemaMutationConverter().Convert(mutation.CreateGlobalAttributeSchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDefaultValueMutation => new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(mutation.ModifyAttributeSchemaDefaultValueMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDeprecationNoticeMutation => new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyAttributeSchemaDeprecationNoticeMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyAttributeSchemaDescriptionMutation => new ModifyAttributeSchemaDescriptionMutationConverter().Convert(mutation.ModifyAttributeSchemaDescriptionMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyAttributeSchemaNameMutation => new ModifyAttributeSchemaNameMutationConverter().Convert(mutation.ModifyAttributeSchemaNameMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyAttributeSchemaTypeMutation => new ModifyAttributeSchemaTypeMutationConverter().Convert(mutation.ModifyAttributeSchemaTypeMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.RemoveAttributeSchemaMutation => new RemoveAttributeSchemaMutationConverter().Convert(mutation.RemoveAttributeSchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaFilterableMutation => new SetAttributeSchemaFilterableMutationConverter().Convert(mutation.SetAttributeSchemaFilterableMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaGloballyUniqueMutation => new SetAttributeSchemaGloballyUniqueMutationConverter().Convert(mutation.SetAttributeSchemaGloballyUniqueMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaLocalizedMutation => new SetAttributeSchemaLocalizedMutationConverter().Convert(mutation.SetAttributeSchemaLocalizedMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaNullableMutation => new SetAttributeSchemaNullableMutationConverter().Convert(mutation.SetAttributeSchemaNullableMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaRepresentativeMutation => new SetAttributeSchemaRepresentativeMutationConverter().Convert(mutation.SetAttributeSchemaRepresentativeMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaSortableMutation => new SetAttributeSchemaSortableMutationConverter().Convert(mutation.SetAttributeSchemaSortableMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.SetAttributeSchemaUniqueMutation => new SetAttributeSchemaUniqueMutationConverter().Convert(mutation.SetAttributeSchemaUniqueMutation),
            
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.CreateEntitySchemaMutation => new CreateEntitySchemaMutationConverter().Convert(mutation.CreateEntitySchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyEntitySchemaMutation => new ModifyEntitySchemaMutationConverter().Convert(mutation.ModifyEntitySchemaMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.ModifyEntitySchemaNameMutation => new ModifyEntitySchemaNameMutationConverter().Convert(mutation.ModifyEntitySchemaNameMutation),
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.RemoveEntitySchemaMutation => new RemoveEntitySchemaMutationConverter().Convert(mutation.RemoveEntitySchemaMutation),
            
            GrpcLocalCatalogSchemaMutation.MutationOneofCase.None => throw new InvalidSchemaMutationException("Mutation is not defined!"),
            _ => throw new EvitaInternalError("This should never happen!")
        };
    }
}
