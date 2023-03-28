using Client.DataTypes;
using Client.Models.Data.Structure;
using Client.Queries;

namespace Client.Models;

public class EvitaEntityResponse : EvitaResponse<SealedEntity>
{
    public EvitaEntityResponse(Query query, IDataChunk<SealedEntity> recordPage) : base(query, recordPage)
    {
    }

    public EvitaEntityResponse(Query query, IDataChunk<SealedEntity> recordPage, params IEvitaResponseExtraResult[] extraResults) : base(query, recordPage, extraResults)
    {
    }
}