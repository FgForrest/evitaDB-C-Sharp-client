using Client.Converters.Models.Mutations;
using Client.Models.Schemas.Mutations;
using Google.Protobuf;

namespace Client.Converters.Models.Schema.Mutations;

public interface ISchemaMutationConverter<TJ, TG> : IMutationConverter<TJ, TG> where TJ : ISchemaMutation where TG : IMessage
{
    
}