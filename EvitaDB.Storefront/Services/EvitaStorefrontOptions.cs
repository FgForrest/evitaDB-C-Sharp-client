namespace EvitaDB.Storefront.Services;

/// <summary>
/// Connection settings, bound from <c>wwwroot/appsettings.json</c>.
/// </summary>
public sealed class EvitaStorefrontOptions
{
    /// <summary>
    /// Host of the evitaDB server. The public demo answers gRPC-Web on port 443 through a front proxy - the
    /// `:5555` the evitaDB documentation advertises is not reachable from a browser on most networks.
    /// </summary>
    public string Host { get; set; } = "demo.evitadb.io";

    public int Port { get; set; } = 443;

    public bool TlsEnabled { get; set; } = true;

    public string Catalog { get; set; } = "evita";
}
