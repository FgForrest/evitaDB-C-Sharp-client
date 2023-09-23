using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas;

public interface ICatalogSchemaBuilder : ICatalogSchemaEditor<ICatalogSchemaBuilder>
{
    ModifyCatalogSchemaMutation? ToMutation();

    ICatalogSchema ToInstance();

    void UpdateViaNewSession(EvitaClient evita)
    {
        using EvitaClientSession session = evita.CreateReadWriteSession(Name);
        session.UpdateCatalogSchema(this);
    }

    void UpdateAndFetchViaNewSession(EvitaClient evita)
    {
        using EvitaClientSession session = evita.CreateReadWriteSession(Name);
        session.UpdateAndFetchCatalogSchema(this);
    }

    void UpdateVia(EvitaClientSession session)
    {
        session.UpdateCatalogSchema(this);
    }

    ISealedCatalogSchema UpdateAndFetchVia(EvitaClientSession session)
    {
        return session.UpdateAndFetchCatalogSchema(this);
    }
}