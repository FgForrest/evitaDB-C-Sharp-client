using Client.Models.Data.Mutations;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations;

public class EntityRemoveMutationConverter
{
    public GrpcEntityRemoveMutation Convert(EntityRemoveMutation mutation)
    {
        return new GrpcEntityRemoveMutation
        {
            EntityType = mutation.EntityType,
            EntityPrimaryKey = mutation.EntityPrimaryKey!.Value
        };
    }

    public EntityRemoveMutation Convert(GrpcEntityRemoveMutation mutation)
    {
        return new EntityRemoveMutation(
            mutation.EntityType,
            mutation.EntityPrimaryKey
        );
    }
}