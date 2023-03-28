using Client.Models.Data.Mutations;
using Client.Models.Data.Structure;

namespace Client.Models.Data;

public interface IEntityBuilder : IEntityEditor<IEntityBuilder>
{
    IEntityMutation? ToMutation();

    SealedEntity ToInstance();

    EntityReference UpsertVia(EvitaClientSession session)
    {
        return session.UpsertEntity(this);
    }
}