using Client.Exceptions;
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