using Client.Converters.Models.Mutations;
using Client.Models.Data.Mutations;
using Google.Protobuf;

namespace Client.Converters.Models.Data.Mutations;

public interface ILocalMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : ILocalMutation where TG : IMessage
{
    
}