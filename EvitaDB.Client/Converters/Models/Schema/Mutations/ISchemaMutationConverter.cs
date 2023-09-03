using EvitaDB.Client.Converters.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations;

public interface ISchemaMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : ISchemaMutation where TG : IMessage
{
    
}