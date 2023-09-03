using EvitaDB;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations;

namespace EvitaDB.Client.Converters.Models.Data.Mutations;

public class DelegatingEntityMutationConverter : IEntityMutationConverter<IEntityMutation, GrpcEntityMutation>
{
    public GrpcEntityMutation Convert(IEntityMutation mutation)
    {
        GrpcEntityMutation grpcEntityMutation = new();
        switch (mutation)
        {
            case EntityUpsertMutation entityUpsertMutation:
                grpcEntityMutation.EntityUpsertMutation =
                    new EntityUpsertMutationConverter().Convert(entityUpsertMutation);
                break;
            case EntityRemoveMutation entityRemoveMutation:
                grpcEntityMutation.EntityRemoveMutation =
                    new EntityRemoveMutationConverter().Convert(entityRemoveMutation);
                break;
            default:
                throw new EvitaInternalError("This should never happen!");
        }

        return grpcEntityMutation;
    }

    public IEntityMutation Convert(GrpcEntityMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcEntityMutation.MutationOneofCase.EntityUpsertMutation => new EntityUpsertMutationConverter().Convert(
                mutation.EntityUpsertMutation),
            GrpcEntityMutation.MutationOneofCase.EntityRemoveMutation => new EntityRemoveMutationConverter().Convert(
                mutation.EntityRemoveMutation),
            GrpcEntityMutation.MutationOneofCase.None => throw new EvitaInternalError("This should never happen!"),
            _ => throw new EvitaInternalError("This should never happen!")
        };
    }
}