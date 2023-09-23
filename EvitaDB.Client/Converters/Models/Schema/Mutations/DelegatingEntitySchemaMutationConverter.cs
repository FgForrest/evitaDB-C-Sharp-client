using EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;
using EvitaDB.Client.Converters.Models.Schema.Mutations.Entities;
using EvitaDB.Client.Converters.Models.Schema.Mutations.References;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations;

public class DelegatingEntitySchemaMutationConverter : ISchemaMutationConverter<IEntitySchemaMutation, GrpcEntitySchemaMutation>
{
    public GrpcEntitySchemaMutation Convert(IEntitySchemaMutation mutation)
    {
        GrpcEntitySchemaMutation grpcEntitySchemaMutation = new();

        switch (mutation)
        {
            // Associated Data
            case CreateAssociatedDataSchemaMutation createAssociatedDataSchemaMutation:
                grpcEntitySchemaMutation.CreateAssociatedDataSchemaMutation = new CreateAssociatedDataSchemaMutationConverter().Convert(createAssociatedDataSchemaMutation);
                break;
            case ModifyAssociatedDataSchemaDeprecationNoticeMutation modifyAssociatedDataSchemaDeprecationNoticeMutation:
                grpcEntitySchemaMutation.ModifyAssociatedDataSchemaDeprecationNoticeMutation = new ModifyAssociatedDataSchemaDeprecationNoticeMutationConverter().Convert(modifyAssociatedDataSchemaDeprecationNoticeMutation);
                break;
            case ModifyAssociatedDataSchemaDescriptionMutation modifyAssociatedDataSchemaDescriptionMutation:
                grpcEntitySchemaMutation.ModifyAssociatedDataSchemaDescriptionMutation = new ModifyAssociatedDataSchemaDescriptionMutationConverter().Convert(modifyAssociatedDataSchemaDescriptionMutation);
                break;
            case ModifyAssociatedDataSchemaNameMutation modifyAssociatedDataSchemaNameMutation:
                grpcEntitySchemaMutation.ModifyAssociatedDataSchemaNameMutation = new ModifyAssociatedDataSchemaNameMutationConverter().Convert(modifyAssociatedDataSchemaNameMutation);
                break;
            case ModifyAssociatedDataSchemaTypeMutation modifyAssociatedDataSchemaTypeMutation:
                grpcEntitySchemaMutation.ModifyAssociatedDataSchemaTypeMutation = new ModifyAssociatedDataSchemaTypeMutationConverter().Convert(modifyAssociatedDataSchemaTypeMutation);
                break;
            case RemoveAssociatedDataSchemaMutation removeAssociatedDataSchemaMutation:
                grpcEntitySchemaMutation.RemoveAssociatedDataSchemaMutation = new RemoveAssociatedDataSchemaMutationConverter().Convert(removeAssociatedDataSchemaMutation);
                break;
            case SetAssociatedDataSchemaLocalizedMutation setAssociatedDataSchemaLocalizedMutation:
                grpcEntitySchemaMutation.SetAssociatedDataSchemaLocalizedMutation = new SetAssociatedDataSchemaLocalizedMutationConverter().Convert(setAssociatedDataSchemaLocalizedMutation);
                break;
            case SetAssociatedDataSchemaNullableMutation setAssociatedDataSchemaNullableMutation:
                grpcEntitySchemaMutation.SetAssociatedDataSchemaNullableMutation = new SetAssociatedDataSchemaNullableMutationConverter().Convert(setAssociatedDataSchemaNullableMutation);
                break;
            
            // Attributes
            case CreateAttributeSchemaMutation createEntitySchemaMutation:
                grpcEntitySchemaMutation.CreateAttributeSchemaMutation = new CreateAttributeSchemaMutationConverter().Convert(createEntitySchemaMutation);
                break;
            case ModifyAttributeSchemaDefaultValueMutation modifyAttributeSchemaDefaultValueMutation:
                grpcEntitySchemaMutation.ModifyAttributeSchemaDefaultValueMutation = new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(modifyAttributeSchemaDefaultValueMutation);
                break;
            case ModifyAttributeSchemaDeprecationNoticeMutation modifyAttributeSchemaDeprecationNotice:
                grpcEntitySchemaMutation.ModifyAttributeSchemaDeprecationNoticeMutation = new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(modifyAttributeSchemaDeprecationNotice);
                break;
            case ModifyAttributeSchemaDescriptionMutation modifyAttributeSchemaDescription:
                grpcEntitySchemaMutation.ModifyAttributeSchemaDescriptionMutation = new ModifyAttributeSchemaDescriptionMutationConverter().Convert(modifyAttributeSchemaDescription);
                break;
            case ModifyAttributeSchemaNameMutation modifyAttributeSchemaNameMutation:
                grpcEntitySchemaMutation.ModifyAttributeSchemaNameMutation = new ModifyAttributeSchemaNameMutationConverter().Convert(modifyAttributeSchemaNameMutation);
                break;
            case ModifyAttributeSchemaTypeMutation modifyAttributeSchemaTypeMutation:
                grpcEntitySchemaMutation.ModifyAttributeSchemaTypeMutation = new ModifyAttributeSchemaTypeMutationConverter().Convert(modifyAttributeSchemaTypeMutation);
                break;
            case RemoveAttributeSchemaMutation removeAttributeSchemaMutation:
                grpcEntitySchemaMutation.RemoveAttributeSchemaMutation = new RemoveAttributeSchemaMutationConverter().Convert(removeAttributeSchemaMutation);
                break;
            case SetAttributeSchemaFilterableMutation setAttributeSchemaFilterableMutation:
                grpcEntitySchemaMutation.SetAttributeSchemaFilterableMutation = new SetAttributeSchemaFilterableMutationConverter().Convert(setAttributeSchemaFilterableMutation);
                break;
            case SetAttributeSchemaLocalizedMutation setAttributeSchemaLocalizedMutation:
                grpcEntitySchemaMutation.SetAttributeSchemaLocalizedMutation = new SetAttributeSchemaLocalizedMutationConverter().Convert(setAttributeSchemaLocalizedMutation);
                break;
            case SetAttributeSchemaNullableMutation setAttributeSchemaNullableMutation:
                grpcEntitySchemaMutation.SetAttributeSchemaNullableMutation = new SetAttributeSchemaNullableMutationConverter().Convert(setAttributeSchemaNullableMutation);
                break;
            case SetAttributeSchemaSortableMutation setAttributeSchemaSortableMutation:
                grpcEntitySchemaMutation.SetAttributeSchemaSortableMutation = new SetAttributeSchemaSortableMutationConverter().Convert(setAttributeSchemaSortableMutation);
                break;
            case SetAttributeSchemaUniqueMutation setAttributeSchemaUniqueMutation:
                grpcEntitySchemaMutation.SetAttributeSchemaUniqueMutation = new SetAttributeSchemaUniqueMutationConverter().Convert(setAttributeSchemaUniqueMutation);
                break;
            case UseGlobalAttributeSchemaMutation uniqueGlobalAttributeSchemaMutation:
                grpcEntitySchemaMutation.UseGlobalAttributeSchemaMutation = new UseGlobalAttributeSchemaMutationConverter().Convert(uniqueGlobalAttributeSchemaMutation);
                break;
            
            // Entities
            case AllowCurrencyInEntitySchemaMutation allowCurrencyInEntitySchemaMutation:
                grpcEntitySchemaMutation.AllowCurrencyInEntitySchemaMutation = new AllowCurrencyInEntitySchemaMutationConverter().Convert(allowCurrencyInEntitySchemaMutation);
                break;
            case AllowEvolutionModeInEntitySchemaMutation allowEvolutionModeInEntitySchemaMutation:
                grpcEntitySchemaMutation.AllowEvolutionModeInEntitySchemaMutation = new AllowEvolutionModeInEntitySchemaMutationConverter().Convert(allowEvolutionModeInEntitySchemaMutation);
                break;
            case AllowLocaleInEntitySchemaMutation allowLocaleInEntitySchemaMutation:
                grpcEntitySchemaMutation.AllowLocaleInEntitySchemaMutation = new AllowLocaleInEntitySchemaMutationConverter().Convert(allowLocaleInEntitySchemaMutation);
                break;
            case DisallowCurrencyInEntitySchemaMutation disallowCurrencyInEntitySchemaMutation:
                grpcEntitySchemaMutation.DisallowCurrencyInEntitySchemaMutation = new DisallowCurrencyInEntitySchemaMutationConverter().Convert(disallowCurrencyInEntitySchemaMutation);
                break;
            case DisallowEvolutionModeInEntitySchemaMutation disallowEvolutionModeInEntitySchemaMutation:
                grpcEntitySchemaMutation.DisallowEvolutionModeInEntitySchemaMutation = new DisallowEvolutionModeInEntitySchemaMutationConverter().Convert(disallowEvolutionModeInEntitySchemaMutation);
                break;
            case DisallowLocaleInEntitySchemaMutation disallowLocaleInEntitySchemaMutation:
                grpcEntitySchemaMutation.DisallowLocaleInEntitySchemaMutation = new DisallowLocaleInEntitySchemaMutationConverter().Convert(disallowLocaleInEntitySchemaMutation);
                break;
            case ModifyEntitySchemaDeprecationNoticeMutation modifyEntitySchemaDeprecationNoticeMutation:
                grpcEntitySchemaMutation.ModifyEntitySchemaDeprecationNoticeMutation = new ModifyEntitySchemaDeprecationNoticeMutationConverter().Convert(modifyEntitySchemaDeprecationNoticeMutation);
                break;
            case ModifyEntitySchemaDescriptionMutation modifyEntitySchemaDescriptionMutation:
                grpcEntitySchemaMutation.ModifyEntitySchemaDescriptionMutation = new ModifyEntitySchemaDescriptionMutationConverter().Convert(modifyEntitySchemaDescriptionMutation);
                break;
            case SetEntitySchemaWithGeneratedPrimaryKeyMutation setEntitySchemaWithGeneratedPrimaryKeyMutation:
                grpcEntitySchemaMutation.SetEntitySchemaWithGeneratedPrimaryKeyMutation = new SetEntitySchemaWithGeneratedPrimaryKeyMutationConverter().Convert(setEntitySchemaWithGeneratedPrimaryKeyMutation);
                break;
            case SetEntitySchemaWithHierarchyMutation setEntitySchemaWithHierarchyMutation:
                grpcEntitySchemaMutation.SetEntitySchemaWithHierarchyMutation = new SetEntitySchemaWithHierarchyMutationConverter().Convert(setEntitySchemaWithHierarchyMutation);
                break;
            case SetEntitySchemaWithPriceMutation setEntitySchemaWithPriceMutation:
                grpcEntitySchemaMutation.SetEntitySchemaWithPriceMutation = new SetEntitySchemaWithPriceMutationConverter().Convert(setEntitySchemaWithPriceMutation);
                break;
            
            // References
            case CreateReferenceSchemaMutation createReferenceSchemaMutation:
                grpcEntitySchemaMutation.CreateReferenceSchemaMutation = new CreateReferenceSchemaMutationConverter().Convert(createReferenceSchemaMutation);
                break;
            case ModifyReferenceAttributeSchemaMutation modifyReferenceAttributeSchemaMutation:
                grpcEntitySchemaMutation.ModifyReferenceAttributeSchemaMutation = new ModifyReferenceAttributeSchemaMutationConverter().Convert(modifyReferenceAttributeSchemaMutation);
                break;
            case ModifyReferenceSchemaCardinalityMutation modifyReferenceSchemaCardinalityMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaCardinalityMutation = new ModifyReferenceSchemaCardinalityMutationConverter().Convert(modifyReferenceSchemaCardinalityMutation);
                break;
            case ModifyReferenceSchemaDeprecationNoticeMutation modifyReferenceSchemaDeprecationNoticeMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaDeprecationNoticeMutation = new ModifyReferenceSchemaDeprecationNoticeMutationConverter().Convert(modifyReferenceSchemaDeprecationNoticeMutation);
                break;
            case ModifyReferenceSchemaDescriptionMutation modifyReferenceSchemaDescriptionMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaDescriptionMutation = new ModifyReferenceSchemaDescriptionMutationConverter().Convert(modifyReferenceSchemaDescriptionMutation);
                break;
            case ModifyReferenceSchemaNameMutation modifyReferenceSchemaNameMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaNameMutation = new ModifyReferenceSchemaNameMutationConverter().Convert(modifyReferenceSchemaNameMutation);
                break;
            case ModifyReferenceSchemaRelatedEntityGroupMutation modifyReferenceSchemaRelatedEntityGroupMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaRelatedEntityGroupMutation = new ModifyReferenceSchemaRelatedEntityGroupMutationConverter().Convert(modifyReferenceSchemaRelatedEntityGroupMutation);
                break;
            case ModifyReferenceSchemaRelatedEntityMutation modifyReferenceSchemaRelatedEntityMutation:
                grpcEntitySchemaMutation.ModifyReferenceSchemaRelatedEntityMutation = new ModifyReferenceSchemaRelatedEntityMutationConverter().Convert(modifyReferenceSchemaRelatedEntityMutation);
                break;
            case RemoveReferenceSchemaMutation removeReferenceSchemaMutation:
                grpcEntitySchemaMutation.RemoveReferenceSchemaMutation = new RemoveReferenceSchemaMutationConverter().Convert(removeReferenceSchemaMutation);
                break;
            case SetReferenceSchemaFacetedMutation setReferenceSchemaFacetedMutation:
                grpcEntitySchemaMutation.SetReferenceSchemaFacetedMutation = new SetReferenceSchemaFacetedMutationConverter().Convert(setReferenceSchemaFacetedMutation);
                break;
            case SetReferenceSchemaIndexedMutation setReferenceSchemaIndexedMutation:
                grpcEntitySchemaMutation.SetReferenceSchemaIndexedMutation = new SetReferenceSchemaFilterableMutationConverter().Convert(setReferenceSchemaIndexedMutation);
                break;
        }
        
        return grpcEntitySchemaMutation;
    }

    public IEntitySchemaMutation Convert(GrpcEntitySchemaMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcEntitySchemaMutation.MutationOneofCase.CreateAssociatedDataSchemaMutation => new CreateAssociatedDataSchemaMutationConverter().Convert(mutation.CreateAssociatedDataSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAssociatedDataSchemaDeprecationNoticeMutation => new ModifyAssociatedDataSchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyAssociatedDataSchemaDeprecationNoticeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAssociatedDataSchemaDescriptionMutation => new ModifyAssociatedDataSchemaDescriptionMutationConverter().Convert(mutation.ModifyAssociatedDataSchemaDescriptionMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAssociatedDataSchemaNameMutation => new ModifyAssociatedDataSchemaNameMutationConverter().Convert(mutation.ModifyAssociatedDataSchemaNameMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAssociatedDataSchemaTypeMutation => new ModifyAssociatedDataSchemaTypeMutationConverter().Convert(mutation.ModifyAssociatedDataSchemaTypeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.RemoveAssociatedDataSchemaMutation => new RemoveAssociatedDataSchemaMutationConverter().Convert(mutation.RemoveAssociatedDataSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAssociatedDataSchemaLocalizedMutation => new SetAssociatedDataSchemaLocalizedMutationConverter().Convert(mutation.SetAssociatedDataSchemaLocalizedMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAssociatedDataSchemaNullableMutation => new SetAssociatedDataSchemaNullableMutationConverter().Convert(mutation.SetAssociatedDataSchemaNullableMutation),
            
            GrpcEntitySchemaMutation.MutationOneofCase.CreateAttributeSchemaMutation => new CreateAttributeSchemaMutationConverter().Convert(mutation.CreateAttributeSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAttributeSchemaDefaultValueMutation => new ModifyAttributeSchemaDefaultValueMutationConverter().Convert(mutation.ModifyAttributeSchemaDefaultValueMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAttributeSchemaDeprecationNoticeMutation => new ModifyAttributeSchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyAttributeSchemaDeprecationNoticeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAttributeSchemaDescriptionMutation => new ModifyAttributeSchemaDescriptionMutationConverter().Convert(mutation.ModifyAttributeSchemaDescriptionMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAttributeSchemaNameMutation => new ModifyAttributeSchemaNameMutationConverter().Convert(mutation.ModifyAttributeSchemaNameMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyAttributeSchemaTypeMutation => new ModifyAttributeSchemaTypeMutationConverter().Convert(mutation.ModifyAttributeSchemaTypeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.RemoveAttributeSchemaMutation => new RemoveAttributeSchemaMutationConverter().Convert(mutation.RemoveAttributeSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAttributeSchemaFilterableMutation => new SetAttributeSchemaFilterableMutationConverter().Convert(mutation.SetAttributeSchemaFilterableMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAttributeSchemaLocalizedMutation => new SetAttributeSchemaLocalizedMutationConverter().Convert(mutation.SetAttributeSchemaLocalizedMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAttributeSchemaNullableMutation => new SetAttributeSchemaNullableMutationConverter().Convert(mutation.SetAttributeSchemaNullableMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAttributeSchemaSortableMutation => new SetAttributeSchemaSortableMutationConverter().Convert(mutation.SetAttributeSchemaSortableMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetAttributeSchemaUniqueMutation => new SetAttributeSchemaUniqueMutationConverter().Convert(mutation.SetAttributeSchemaUniqueMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.UseGlobalAttributeSchemaMutation => new UseGlobalAttributeSchemaMutationConverter().Convert(mutation.UseGlobalAttributeSchemaMutation),
            
            GrpcEntitySchemaMutation.MutationOneofCase.AllowCurrencyInEntitySchemaMutation => new AllowCurrencyInEntitySchemaMutationConverter().Convert(mutation.AllowCurrencyInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.AllowEvolutionModeInEntitySchemaMutation => new AllowEvolutionModeInEntitySchemaMutationConverter().Convert(mutation.AllowEvolutionModeInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.AllowLocaleInEntitySchemaMutation => new AllowLocaleInEntitySchemaMutationConverter().Convert(mutation.AllowLocaleInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.DisallowCurrencyInEntitySchemaMutation => new DisallowCurrencyInEntitySchemaMutationConverter().Convert(mutation.DisallowCurrencyInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.DisallowEvolutionModeInEntitySchemaMutation => new DisallowEvolutionModeInEntitySchemaMutationConverter().Convert(mutation.DisallowEvolutionModeInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.DisallowLocaleInEntitySchemaMutation => new DisallowLocaleInEntitySchemaMutationConverter().Convert(mutation.DisallowLocaleInEntitySchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyEntitySchemaDeprecationNoticeMutation => new ModifyEntitySchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyEntitySchemaDeprecationNoticeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyEntitySchemaDescriptionMutation => new ModifyEntitySchemaDescriptionMutationConverter().Convert(mutation.ModifyEntitySchemaDescriptionMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetEntitySchemaWithGeneratedPrimaryKeyMutation => new SetEntitySchemaWithGeneratedPrimaryKeyMutationConverter().Convert(mutation.SetEntitySchemaWithGeneratedPrimaryKeyMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetEntitySchemaWithHierarchyMutation => new SetEntitySchemaWithHierarchyMutationConverter().Convert(mutation.SetEntitySchemaWithHierarchyMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetEntitySchemaWithPriceMutation => new SetEntitySchemaWithPriceMutationConverter().Convert(mutation.SetEntitySchemaWithPriceMutation),
            
            GrpcEntitySchemaMutation.MutationOneofCase.CreateReferenceSchemaMutation => new CreateReferenceSchemaMutationConverter().Convert(mutation.CreateReferenceSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceAttributeSchemaMutation => new ModifyReferenceAttributeSchemaMutationConverter().Convert(mutation.ModifyReferenceAttributeSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaCardinalityMutation => new ModifyReferenceSchemaCardinalityMutationConverter().Convert(mutation.ModifyReferenceSchemaCardinalityMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaDeprecationNoticeMutation => new ModifyReferenceSchemaDeprecationNoticeMutationConverter().Convert(mutation.ModifyReferenceSchemaDeprecationNoticeMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaDescriptionMutation => new ModifyReferenceSchemaDescriptionMutationConverter().Convert(mutation.ModifyReferenceSchemaDescriptionMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaNameMutation => new ModifyReferenceSchemaNameMutationConverter().Convert(mutation.ModifyReferenceSchemaNameMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaRelatedEntityGroupMutation => new ModifyReferenceSchemaRelatedEntityGroupMutationConverter().Convert(mutation.ModifyReferenceSchemaRelatedEntityGroupMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.ModifyReferenceSchemaRelatedEntityMutation => new ModifyReferenceSchemaRelatedEntityMutationConverter().Convert(mutation.ModifyReferenceSchemaRelatedEntityMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.RemoveReferenceSchemaMutation => new RemoveReferenceSchemaMutationConverter().Convert(mutation.RemoveReferenceSchemaMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetReferenceSchemaFacetedMutation => new SetReferenceSchemaFacetedMutationConverter().Convert(mutation.SetReferenceSchemaFacetedMutation),
            GrpcEntitySchemaMutation.MutationOneofCase.SetReferenceSchemaIndexedMutation => new SetReferenceSchemaFilterableMutationConverter().Convert(mutation.SetReferenceSchemaIndexedMutation),
            _ => throw new EvitaInternalError("This should never happen!")
        };
    }
}