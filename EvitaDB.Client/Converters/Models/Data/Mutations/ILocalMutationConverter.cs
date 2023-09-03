using EvitaDB.Client.Converters.Models.Mutations;
using EvitaDB.Client.Models.Data.Mutations;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Data.Mutations;

public interface ILocalMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : ILocalMutation where TG : IMessage
{
    
}