using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

public interface IEntityBuilder : IEntityEditor<IEntityBuilder>
{
    IEntityMutation? ToMutation();

    SealedEntity ToInstance();

    EntityReference UpsertVia(EvitaClientSession session)
    {
        return session.UpsertEntity(this);
    }
}