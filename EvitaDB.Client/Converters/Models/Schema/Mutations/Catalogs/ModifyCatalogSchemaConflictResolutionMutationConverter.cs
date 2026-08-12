using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class ModifyCatalogSchemaConflictResolutionMutationConverter : ISchemaMutationConverter<
    ModifyCatalogSchemaConflictResolutionMutation, GrpcModifyCatalogSchemaConflictResolutionMutation>
{
    public GrpcModifyCatalogSchemaConflictResolutionMutation Convert(
        ModifyCatalogSchemaConflictResolutionMutation mutation)
    {
        GrpcModifyCatalogSchemaConflictResolutionMutation grpcMutation = new();
        GrpcConflictResolution? conflictResolution = EvitaEnumConverter.ToGrpcConflictResolution(mutation.ConflictResolution);
        if (conflictResolution is not null)
        {
            grpcMutation.ConflictResolution = conflictResolution;
        }
        return grpcMutation;
    }

    public ModifyCatalogSchemaConflictResolutionMutation Convert(
        GrpcModifyCatalogSchemaConflictResolutionMutation mutation)
    {
        return new ModifyCatalogSchemaConflictResolutionMutation(
            EvitaEnumConverter.ToConflictResolution(mutation.ConflictResolution)
        );
    }
}
