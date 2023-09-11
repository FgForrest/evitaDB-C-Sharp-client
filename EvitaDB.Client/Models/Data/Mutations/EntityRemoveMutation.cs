using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Mutations.Entities;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using EvitaDB.Client.Models.Data.Mutations.Reference;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations;

public class EntityRemoveMutation : IEntityMutation
{
    public string EntityType { get; }

    private readonly int? _entityPrimaryKey;

    public int? EntityPrimaryKey
    {
        get => _entityPrimaryKey;
        set => throw new EvitaInvalidUsageException(
            "Updating primary key when entity is being removed is not possible!");
    }

    public EntityRemoveMutation(string entityType, int entityPrimaryKey)
    {
        EntityType = entityType;
        _entityPrimaryKey = entityPrimaryKey;
    }

    public EntityExistence Expects()
    {
        return EntityExistence.MayExist;
    }

    public Entity Mutate(IEntitySchema entitySchema, Entity? entity)
    {
        Assert.NotNull(entity, "Entity must not be null in order to be removed!");
        if (entity!.Dropped)
        {
            return entity;
        }
        return Entity.MutateEntity(entitySchema, entity, ComputeLocalMutationsForEntityRemoval(entity));
    }

    public List<ILocalMutation> ComputeLocalMutationsForEntityRemoval(ISealedEntity entity)
    {
        return (entity.ParentAvailable() && entity.Parent is not null
                ? new []{entity.Parent.Value}
                : Array.Empty<int>())
            .Select(_ => new RemoveParentMutation())
            .Concat(
                entity.GetReferences()
                    .Where(x => x.Dropped == false)
                    .SelectMany(it => it.GetAttributeValues()
                        .Where(x => x.Dropped == false)
                        .Select(x => new ReferenceAttributeMutation(
                            it.ReferenceKey,
                            new RemoveAttributeMutation(x.Key)
                        ))
                        .Concat<ILocalMutation>(new[] {new RemoveReferenceMutation(it.ReferenceKey)})
                    )
            )
            .Concat(
                entity.GetAttributeValues()
                    .Where(x => x.Dropped == false)
                    .Select(x => x.Key)
                    .Select(key => new RemoveAttributeMutation(key))
            )
            .Concat(
                entity.GetAssociatedDataValues()
                    .Where(x => x.Dropped == false)
                    .Select(x => x.Key)
                    .Select(key => new RemoveAssociatedDataMutation(key))
            )
            .Concat(
                new[] {new SetPriceInnerRecordHandlingMutation(PriceInnerRecordHandling.None)}
            )
            .Concat(
                (entity.PricesAvailable() ? entity.GetPrices() : Enumerable.Empty<IPrice>())
                .Where(x => x.Dropped == false)
                .Select(it => new RemovePriceMutation(it.Key))
            )
            .Where(it => it is not null)
            .ToList();
    }
}