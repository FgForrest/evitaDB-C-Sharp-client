# evitaDB C# client

[![NuGet](https://img.shields.io/nuget/v/EvitaDB.Client.svg)](https://www.nuget.org/packages/EvitaDB.Client)
[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

The official .NET driver for [evitaDB](https://evitadb.io) — a specialized, fast e-commerce database.
It connects to a remote evitaDB server over its gRPC API and exposes the same session-based programming
model as the [Java client](https://github.com/FgForrest/evitaDB), adapted to C# conventions: strongly
typed query DSL, sealed entity model, builder-based mutations and first-class `async`/`await` support
across the entire network surface.

## Compatibility

| Driver | evitaDB server | .NET |
|---|---|---|
| current | 2026.2.x (the wire protocol also tolerates older servers via built-in fallbacks) | .NET 10 |

## Installation

```shell
dotnet add package EvitaDB.Client
```

## Quickstart

### Connect

```csharp
using EvitaDB.Client;
using EvitaDB.Client.Config;

EvitaClientConfiguration configuration = new EvitaClientConfiguration.Builder()
    .SetHost("demo.evitadb.io")
    .SetPort(5555)
    .SetUseGeneratedCertificate(false)
    .SetUsingTrustedRootCaCertificate(true)
    .Build();

using EvitaClient evita = await EvitaClient.Create(configuration);
```

The builder also exposes mTLS (`SetMtlsEnabled`, client certificate paths), trust for the server's
self-generated certificate (`SetUseGeneratedCertificate`), plain-text mode (`SetTlsEnabled(false)`),
OpenTelemetry tracing (`SetTraceEndpointUrl`) and connection keep-alive tuning
(`SetPingIntervalMilliseconds`, `SetIdleTimeoutMilliseconds`).

### Query

Bring the query DSL into scope with a static import — queries then read almost identically to
[evitaQL](https://evitadb.io/documentation/query/basics) and the Java DSL:

```csharp
using System.Globalization;
using EvitaDB.Client.Models.Data;
using static EvitaDB.Client.Queries.IQueryConstraints;

IList<ISealedEntity> products = evita.QueryCatalog(
    "evita",
    session => session.QueryListOfSealedEntities(
        Query(
            Collection("Product"),
            FilterBy(
                AttributeEquals("status", "ACTIVE"),
                EntityLocaleEquals(new CultureInfo("en"))
            ),
            Require(
                Page(1, 20),
                EntityFetch(
                    AttributeContentAll(),
                    PriceContentRespectingFilter(),
                    ReferenceContentAllWithAttributes()
                )
            )
        )
    ));
```

Every query has three shapes: `QueryOne*` (single record), `QueryList*` (records only) and `Query*`
(full response with paging and extra results such as facet summary, hierarchy statistics or histograms):

```csharp
EvitaResponse<ISealedEntity> response = session.QuerySealedEntity(query);
FacetSummary? facets = response.GetExtraResult<FacetSummary>();
```

### Write

```csharp
evita.UpdateCatalog("evita", session =>
{
    EntityReference reference = session.UpsertEntity(
        session.CreateNewEntity("Product")
            .SetAttribute("name", new CultureInfo("en"), "Cool Product")
            .SetAttribute("code", "cool-product-1"));

    session.DeleteEntity("Product", 42);
});
```

Schemas are defined the same way — catalog and entity schema builders produce mutations that are applied
through the session:

```csharp
evita.DefineCatalog("evita");
evita.UpdateCatalog("evita", session =>
{
    session.GetCatalogSchema().OpenForWrite()
        .WithAttribute<string>("code", thatIs => thatIs.UniqueGlobally())
        .UpdateVia(session);

    if (session.CatalogState == CatalogState.WarmingUp)
    {
        session.GoLiveAndClose();
    }
});
```

### Async

Every network call has an async counterpart accepting a `CancellationToken` — the sync methods are thin
facades over the same async core:

```csharp
ISealedEntity? entity = await evita.QueryCatalogAsync(
    "evita",
    session => session.GetEntityAsync("Product", 1, EntityFetchAll().Requirements!),
    cancellationToken);

await evita.UpdateCatalogAsync(
    "evita",
    session => session.UpsertEntityAsync(entityMutation));
```

### Transactions & commit progress

A read-write session on a live catalog is transactional. Besides the plain `Close()`, the driver exposes
the server's three commit phases as awaitable tasks:

```csharp
EvitaClientSession session = evita.CreateReadWriteSession("evita");
await session.UpsertEntityAsync(mutation);

CommitProgress progress = session.CloseNowWithProgress();
await progress.OnConflictResolved;   // conflicts checked
await progress.OnWalAppended;        // durably written to the write-ahead log
await progress.OnChangesVisible;     // visible to other sessions
```

### Change data capture

Catalog and system change streams are exposed as `IAsyncEnumerable`, kept alive by server heartbeats:

```csharp
await foreach (ChangeCatalogCapture capture in session.RegisterChangeCatalogCaptureAsync(request, ct))
{
    Console.WriteLine(capture.Operation);
}
```

The write-ahead-log history is available through `GetMutationsHistory` / `GetMutationsHistoryAsync`.

### Management

`evita.Management()` provides server status, configuration, catalog statistics, long-running task
tracking and file access. Catalog backup/restore and archival entity scopes (`ArchiveEntity` /
`RestoreEntity`) are available on the session and client.

## Building from source

```shell
dotnet restore EvitaDB.slnx
dotnet build EvitaDB.slnx
dotnet test EvitaDB.Test/EvitaDB.Test.csproj
```

The test suite starts disposable evitaDB containers via
[Testcontainers](https://dotnet.testcontainers.org/) — a running Docker daemon is required.

| Environment variable | Purpose | Default |
|---|---|---|
| `EVITA_IMAGE_TAG` | Docker tag of `evitadb/evitadb` the integration tests run against | `2026.2.4` |
| `EVITA_DEMO_HOST` / `EVITA_DEMO_PORT` | Server with the demo dataset for the read-only demo query suite | `demo.evitadb.io` / `5555` |

## Repository layout

| Path | Content |
|---|---|
| `EvitaDB.Client` | The driver itself (published as the `EvitaDB.Client` NuGet package) |
| `EvitaDB.Client/Protos` | gRPC protocol definitions, pinned to the targeted evitaDB release |
| `EvitaDB.Test` | xUnit integration test suite (Testcontainers + demo dataset) |
| `EvitaDB.QueryValidator` | Standalone tool that validates and evaluates evitaQL snippets (used by the evitaDB documentation pipeline) |
| `documentation/` | Developer documentation — architecture, conventions, upgrade guides |

Developer documentation for contributors — including the architecture overview and the process for
adapting the driver to newer evitaDB versions — lives in [`documentation/`](documentation/).

## Releases

Releases are produced by the [`release.yml`](.github/workflows/release.yml) workflow: every push to
`master` builds and tests the solution, derives the next semantic version from commit messages
(`feat:` → minor, `(breaking)` → major), publishes a GitHub release with the query validator binaries
and pushes the `EvitaDB.Client` package to NuGet.org via
[Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) (OIDC —
no long-lived API keys).

## License

[Apache 2.0](LICENSE) — © FG Forrest, a.s.
