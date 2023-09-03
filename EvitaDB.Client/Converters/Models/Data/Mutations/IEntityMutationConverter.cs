using EvitaDB.Client.Converters.Models.Mutations;
using EvitaDB.Client.Models.Data.Mutations;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Data.Mutations;

public interface IEntityMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : IEntityMutation where TG : IMessage
{
    
}