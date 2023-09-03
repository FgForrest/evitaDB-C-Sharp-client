using EvitaDB.Client.Models.Mutations;
using Google.Protobuf;

namespace EvitaDB.Client.Converters.Models.Mutations;

public interface IMutationConverter<TJ, TG> where TJ : IMutation where TG : IMessage
{
    TG Convert(TJ mutation);
    TJ Convert(TG mutation);
}