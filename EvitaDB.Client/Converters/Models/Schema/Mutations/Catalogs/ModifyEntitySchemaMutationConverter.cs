using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Catalogs;

public class ModifyEntitySchemaMutationConverter : ISchemaMutationConverter<ModifyEntitySchemaMutation, GrpcModifyEntitySchemaMutation>
{
    private static readonly DelegatingEntitySchemaMutationConverter EntitySchemaMutationConverter = new();

    public GrpcModifyEntitySchemaMutation Convert(ModifyEntitySchemaMutation mutation)
    {
        List<GrpcEntitySchemaMutation> entitySchemaMutations = mutation.SchemaMutations
            .Select(EntitySchemaMutationConverter.Convert)
            .ToList();

        return new GrpcModifyEntitySchemaMutation
            {
                EntityType = mutation.EntityType,
                EntitySchemaMutations = {entitySchemaMutations}
            };
    }

    public ModifyEntitySchemaMutation Convert(GrpcModifyEntitySchemaMutation mutation)
    {
        IEntitySchemaMutation[] entitySchemaMutations = mutation.EntitySchemaMutations
            .Select(EntitySchemaMutationConverter.Convert)
            .ToArray();

        return new ModifyEntitySchemaMutation(
            mutation.EntityType,
            entitySchemaMutations
        );
    }
}