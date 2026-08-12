using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.Scopes;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Scopes;

public class SetEntityScopeMutationConverter : ILocalMutationConverter<SetEntityScopeMutation, GrpcSetEntityScopeMutation>
{
    public GrpcSetEntityScopeMutation Convert(SetEntityScopeMutation mutation)
    {
        return new GrpcSetEntityScopeMutation
        {
            Scope = EvitaEnumConverter.ToGrpcScope(mutation.Scope)
        };
    }

    public SetEntityScopeMutation Convert(GrpcSetEntityScopeMutation mutation)
    {
        return new SetEntityScopeMutation(EvitaEnumConverter.ToScope(mutation.Scope));
    }
}
