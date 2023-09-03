using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Queries;

namespace EvitaDB.Client.Models;

public class EvitaEntityReferenceResponse : EvitaResponse<EntityReference>
{
    public EvitaEntityReferenceResponse(Query query, IDataChunk<EntityReference> recordPage) : base(query, recordPage)
    {
    }

    public EvitaEntityReferenceResponse(Query query, IDataChunk<EntityReference> recordPage,
        params IEvitaResponseExtraResult[] extraResults) : base(query, recordPage, extraResults)
    {
    }
}