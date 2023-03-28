using Client.Converters.Models.Data.Mutations.Attributes;
using Client.Models.Data.Mutations;
using Client.Models.Data.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations;

public class DelegatingLocalMutationConverter : ILocalMutationConverter<ILocalMutation, GrpcLocalMutation>
{
    public GrpcLocalMutation Convert(ILocalMutation mutation)
    {
        GrpcLocalMutation grpcLocalMutation = new();
        switch (mutation)
        {
            case UpsertAttributeMutation upsertAttributeMutation: 
                grpcLocalMutation.UpsertAttributeMutation = new UpsertAttributeMutationConverter().Convert(upsertAttributeMutation);
                break;
        }
        return grpcLocalMutation;
    }

    public ILocalMutation Convert(GrpcLocalMutation mutation)
    {
        throw new NotImplementedException();
    }
}