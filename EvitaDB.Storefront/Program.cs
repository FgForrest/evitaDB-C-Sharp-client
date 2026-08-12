using EvitaDB.Storefront;
using EvitaDB.Storefront.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Read wwwroot/appsettings.json by key rather than through ConfigurationBinder.Get<T>(), so the app does not
// depend on the Microsoft.Extensions.Configuration.Binder package being pulled in.
EvitaStorefrontOptions options = new();
options.Host = builder.Configuration["Evita:Host"] ?? options.Host;
options.Catalog = builder.Configuration["Evita:Catalog"] ?? options.Catalog;
if (int.TryParse(builder.Configuration["Evita:Port"], out int port))
{
    options.Port = port;
}
if (bool.TryParse(builder.Configuration["Evita:TlsEnabled"], out bool tlsEnabled))
{
    options.TlsEnabled = tlsEnabled;
}

builder.Services.AddSingleton(options);
builder.Services.AddSingleton<EvitaCatalogContext>();
builder.Services.AddSingleton<StorefrontState>();
builder.Services.AddSingleton<ProductCatalogService>();

WebAssemblyHost host = builder.Build();

// Connect, open the session and prime the schema caches before the first component renders. Doing this here
// rather than lazily keeps a failure legible: the UI shows the reason instead of every page erroring
// separately. EvitaCatalogContext captures the failure rather than throwing, so the app still starts.
EvitaCatalogContext catalog = host.Services.GetRequiredService<EvitaCatalogContext>();
await catalog.InitializeAsync();

await host.RunAsync();
