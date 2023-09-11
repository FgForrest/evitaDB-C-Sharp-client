using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations;

public class EntityUpsertMutation : IEntityMutation
{
    public string EntityType { get; }
    public int? EntityPrimaryKey { get; set; }

    public EntityExistence EntityExistence { get; }

    public ICollection<ILocalMutation> LocalMutations { get; }

    public EntityUpsertMutation(
        string entityType,
        int? entityPrimaryKey,
        EntityExistence entityExistence,
        ICollection<ILocalMutation> localMutations
    )
    {
        EntityPrimaryKey = entityPrimaryKey;
        EntityType = entityType;
        EntityExistence = entityExistence;
        LocalMutations = localMutations;
    }

    public EntityUpsertMutation(
        string entityType,
        int? entityPrimaryKey,
        EntityExistence entityExistence,
        params ILocalMutation[] localMutations
    )
    {
        EntityPrimaryKey = entityPrimaryKey;
        EntityType = entityType;
        EntityExistence = entityExistence;
        LocalMutations = localMutations.ToList();
    }

    public EntityExistence Expects() => EntityExistence;

    public Entity Mutate(IEntitySchema entitySchema, Entity? entity)
    {
        entity ??= new Entity(EntityType, EntityPrimaryKey);
        return Entity.MutateEntity(
            entitySchema,
            entity,
            LocalMutations
        );
    }
}