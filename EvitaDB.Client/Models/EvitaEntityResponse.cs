using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries;

namespace EvitaDB.Client.Models;

public class EvitaEntityResponse : EvitaResponse<SealedEntity>
{
    public EvitaEntityResponse(Query query, IDataChunk<SealedEntity> recordPage) : base(query, recordPage)
    {
    }

    public EvitaEntityResponse(Query query, IDataChunk<SealedEntity> recordPage, params IEvitaResponseExtraResult[] extraResults) : base(query, recordPage, extraResults)
    {
    }
}