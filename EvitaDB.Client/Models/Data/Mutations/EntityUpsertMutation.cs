using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Mutations;
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

    public Operation Operation => Operation.Upsert;

    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        MutationPredicateContext context = predicate.Context;
        context.SetEntityType(this.EntityType);
        if (this.EntityPrimaryKey.HasValue)
        {
            context.SetPrimaryKey(this.EntityPrimaryKey.Value);
        }
        context.Advance();
        IEnumerable<ChangeCatalogCapture> entityMutation;
        if (predicate.Test(this))
        {
            entityMutation = [
                ChangeCatalogCapture.DataCapture(context, Operation, content == CaptureContent.Body ? this : null)
            ];
        }
        else
        {
            entityMutation = Array.Empty<ChangeCatalogCapture>();
        }

        if (context.Direction == IMutation.StreamDirection.Forward)
        {
            return entityMutation.Concat(LocalMutations
                    .Where(predicate.Test)
                    .SelectMany(it => it.ToChangeCatalogCapture(predicate, content))
            );
        }

        return this.LocalMutations
            .OrderByDescending(x => x)
            .Where(predicate.Test)
            .SelectMany(y => y.ToChangeCatalogCapture(predicate, content))
            .Concat(entityMutation);
    }
}
