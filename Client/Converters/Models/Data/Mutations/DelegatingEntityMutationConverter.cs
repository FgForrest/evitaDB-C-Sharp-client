using Client.Models.Data.Mutations;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations;

public class DelegatingEntityMutationConverter : IEntityMutationConverter<IEntityMutation, GrpcEntityMutation>
{
    public GrpcEntityMutation Convert(IEntityMutation mutation)
    {
        GrpcEntityMutation grpcEntityMutation = new();
        switch (mutation)
        {
            case EntityUpsertMutation entityUpsertMutation: 
                grpcEntityMutation.EntityUpsertMutation = new EntityUpsertMutationConverter().Convert(entityUpsertMutation);
                break;
        }
        return grpcEntityMutation;
    }

    public IEntityMutation Convert(GrpcEntityMutation mutation)
    {
        throw new NotImplementedException();
    }
}