using Client.DataTypes;
using Client.Models.Data.Structure;
using Client.Queries;

namespace Client.Models;

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