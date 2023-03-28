using Client.Converters.Models.Mutations;
using Client.Models.Data.Mutations;
using Google.Protobuf;

namespace Client.Converters.Models.Data.Mutations;

public interface IEntityMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : IEntityMutation where TG : IMessage
{
    
}