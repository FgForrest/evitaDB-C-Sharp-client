using EvitaDB;
using EvitaDB.Client.Converters.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Data.Mutations;

public class DelegatingAttributeMutationConverter : ILocalMutationConverter<AttributeMutation, GrpcAttributeMutation>
{
    public GrpcAttributeMutation Convert(AttributeMutation mutation)
    {
        GrpcAttributeMutation grpcAttributeMutation = new();
        switch (mutation)
        {
            case ApplyDeltaAttributeMutation applyDeltaAttributeMutation:
                grpcAttributeMutation.ApplyDeltaAttributeMutation =
                    new ApplyDeltaMutationConverter().Convert(applyDeltaAttributeMutation);
                break;
            case RemoveAttributeMutation removeAttributeMutation:
                grpcAttributeMutation.RemoveAttributeMutation =
                    new RemoveAttributeMutationConverter().Convert(removeAttributeMutation);
                break;
            case UpsertAttributeMutation upsertAttributeMutation:
                grpcAttributeMutation.UpsertAttributeMutation =
                    new UpsertAttributeMutationConverter().Convert(upsertAttributeMutation);
                break;
            default:
                throw new EvitaInternalError("This should never happen!");
        }
        return grpcAttributeMutation;
    }

    public AttributeMutation Convert(GrpcAttributeMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcAttributeMutation.MutationOneofCase.ApplyDeltaAttributeMutation => new ApplyDeltaMutationConverter().Convert(
                mutation.ApplyDeltaAttributeMutation),
            GrpcAttributeMutation.MutationOneofCase.RemoveAttributeMutation => new RemoveAttributeMutationConverter().Convert(
                mutation.RemoveAttributeMutation),
            GrpcAttributeMutation.MutationOneofCase.UpsertAttributeMutation => new UpsertAttributeMutationConverter().Convert(
                mutation.UpsertAttributeMutation),
            GrpcAttributeMutation.MutationOneofCase.None => throw new EvitaInternalError("This should never happen!"),
            _ => throw new EvitaInternalError("This should never happen!")
        };
    }
}