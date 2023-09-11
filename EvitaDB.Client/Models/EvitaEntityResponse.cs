using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Queries;

namespace EvitaDB.Client.Models;

public class EvitaEntityResponse : EvitaResponse<ISealedEntity>
{
    public EvitaEntityResponse(Query query, IDataChunk<ISealedEntity> recordPage) : base(query, recordPage)
    {
    }

    public EvitaEntityResponse(Query query, IDataChunk<ISealedEntity> recordPage, params IEvitaResponseExtraResult[] extraResults) : base(query, recordPage, extraResults)
    {
    }
}