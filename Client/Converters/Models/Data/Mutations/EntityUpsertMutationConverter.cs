using Client.Models.Data.Mutations;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations;

public class EntityUpsertMutationConverter : IEntityMutationConverter<EntityUpsertMutation, GrpcEntityUpsertMutation>
{
    private static readonly DelegatingLocalMutationConverter EntityLocalMutationConverter = new();

    public GrpcEntityUpsertMutation Convert(EntityUpsertMutation mutation)
    {
        List<GrpcLocalMutation> grpcLocalMutations = mutation.LocalMutations
            .Select(m => EntityLocalMutationConverter.Convert(m))
            .ToList();
        
        return new GrpcEntityUpsertMutation
        {
            EntityType = mutation.EntityType,
            EntityExistence = EvitaEnumConverter.ToGrpcEntityExistence(mutation.EntityExistence),
            Mutations = { grpcLocalMutations },
            EntityPrimaryKey = mutation.EntityPrimaryKey
        };
    }

    public EntityUpsertMutation Convert(GrpcEntityUpsertMutation mutation)
    {
        return new EntityUpsertMutation(
            mutation.EntityType,
            mutation.EntityPrimaryKey,
            EvitaEnumConverter.ToEntityExistence(mutation.EntityExistence),
            mutation.Mutations
                .Select(x=>EntityLocalMutationConverter.Convert(x))
                .ToList()
        );
    }
}