using EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

namespace EvitaDB.Client.Models.Schemas;

public interface ICatalogSchemaBuilder : ICatalogSchemaEditor<ICatalogSchemaBuilder>
{
    ModifyCatalogSchemaMutation? ToMutation();

    ICatalogSchema ToInstance();

    void UpdateViaNewSession(EvitaClient evita)
    {
        using EvitaClientSession session = evita.CreateReadWriteSession(Name);
        try
        {
            session.UpdateCatalogSchema(this);
        }
        finally
        {
            session.Close();
        }
    }

    void UpdateAndFetchViaNewSession(EvitaClient evita)
    {
        using EvitaClientSession session = evita.CreateReadWriteSession(Name);
        try
        {
            session.UpdateAndFetchCatalogSchema(this);
        }
        finally
        {
            session.Close();
        }
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