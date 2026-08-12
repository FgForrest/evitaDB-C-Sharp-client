# Architecture

The driver is a faithful port of the evitaDB Java client (`evita_java_driver` plus the shared gRPC
converter module) to C# conventions. When in doubt about intended behavior, the Java implementation in
the [evitaDB repository](https://github.com/FgForrest/evitaDB) is the source of truth — see
[upgrading-evitadb.md](upgrading-evitadb.md) for where each C# type maps to its Java counterpart.

## Layers

```
EvitaClient ──────────────► catalog lifecycle, session factory, top-level schema mutations, CDC (system)
  │
  ├── EvitaClientManagement ► server status, configuration, catalog statistics, tasks, files
  │
  └── EvitaClientSession ───► queries, entity CRUD, schema mutations, transactions, backups, CDC (catalog)
        │
        ├── Converters ─────► gRPC messages ⇄ client model (entities, schemas, mutations, queries, enums)
        ├── Models ─────────► sealed entities, schemas, mutations, query DSL, extra results
        └── Pooling ────────► gRPC channel pool + shared streaming channel
```

### Transport (`Pooling/`, `Interceptors/`)

* `ChannelBuilder` creates gRPC channels using `SocketsHttpHandler` with keep-alive pings configured
  from `EvitaClientConfiguration` (`PingIntervalMilliseconds`, `IdleTimeoutMilliseconds`).
* `ChannelPool` holds `EvitaClientConfiguration.ChannelPoolSize` channels for unary calls;
  `SharedChannelSupplier` provides the dedicated long-lived channel used by server-streaming calls
  (CDC, mutation history, progress streams).

#### Browser hosts (a deliberate C#-only deviation)

The Java driver has no browser target, so the following has **no counterpart to mirror** — it is a
C#-only addition, documented here the way the `GrpcDataItemMap` rename is (see
[wire-compatibility.md](wire-compatibility.md)).

`EvitaClientConfiguration.HttpHandler` lets the caller supply the transport for every channel. When
set, `EvitaClient`:

* does **not** construct a `SocketsHttpHandler` (merely instantiating one throws on `browser-wasm`),
  and therefore applies neither the keep-alive ping nor the idle-timeout tuning — HTTP/2 pings are
  meaningless over the browser's fetch API;
* does **not** build a `ClientCertificateManager` even when `TlsEnabled` is true — the browser owns TLS.

This is what makes Blazor WebAssembly possible via gRPC-Web:

```csharp
new EvitaClientConfiguration.Builder()
    .SetHost("demo.evitadb.io").SetPort(443)
    .SetHttpHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()))
    .SetChannelPoolSize(1)
    .Build();
```

**A browser host must additionally avoid every blocking call**, and three of them sit on the ordinary
read path — they are not obvious, because two are invoked *lazily* during response conversion:

| Blocking call | Async replacement |
| --- | --- |
| `EvitaClient.CreateSession` (also reached from `QueryCatalogAsync`) | `EvitaClient.CreateSessionAsync` |
| `EvitaClientSession.FetchEntitySchema`, on a schema-cache miss while converting a query response | `EvitaClientSession.GetEntitySchemaAsync` — **primes the cache** so the miss never happens |
| `EvitaClientSession.FetchCatalogSchema`, via the lazily invoked catalog-schema supplier | `EvitaClientSession.GetCatalogSchemaAsync` — likewise primes |

The versioned key must match exactly: response conversion looks the schema up by the **entity's
`SchemaVersion`** (`EntityConverter.ToEntities`), not by the entity's own `Version`. Passing the wrong one
makes every lookup miss, which re-fetches the schema on every query — invisible on desktop, fatal here.

Priming is what makes this work: `EvitaEntitySchemaCache.SetEntitySchema` stores a schema under **both**
the versioned key used by response conversion and the "latest" key
(`SetLatestEntitySchema` populates only the latter, which is not enough). A browser host therefore
fetches the catalog schema and the schema of every entity type it will touch — *including the types of
referenced entities* — before its first query. See `EvitaDB.Storefront/Services/EvitaCatalogContext.cs`
for a worked example.
* `ClientInterceptor` attaches three metadata headers to every call:
  * `clientId` — configured application identifier,
  * `clientVersion` — `ClientInterceptor.AdvertisedClientVersion`; the server uses it to gate
    version-dependent wire behavior (see [wire-compatibility.md](wire-compatibility.md)),
  * `sessionId` — taken from the `AsyncLocal`-based `SessionIdHolder`; sessions are not
    catalog-scoped on the wire anymore, the session id alone identifies the session.
* Every call site funnels through `ExecuteWith*Service(Async)` helpers that acquire/release a channel
  and translate `RpcException` into evitaDB exception types (`TranslateRpcException`). Status
  `InvalidArgument` maps to `EvitaInvalidUsageException`, `Unauthenticated` closes the session and
  surfaces `InstanceTerminatedException`, everything else becomes `EvitaInternalError`.

### Client (`EvitaClient`)

* Created via `await EvitaClient.Create(configuration)` — the factory verifies server compatibility
  (`VerifyServerCompatibilityAsync`) before returning.
* Owns catalog lifecycle operations. The 2026 protocol made most of them **long-running server
  operations** exposed as progress streams (`…WithProgress` RPCs). The client drains these streams via
  `DrainProgressStreamAsync`; because the server can abort a progress stream while the operation
  continues, effect-polling fallbacks (`WaitForCatalogNamesConditionAsync`, `WaitForCatalogStateAsync`)
  confirm the outcome instead of trusting the stream alone.
* `_active` is instance state; closing one client must not affect other client instances sharing the
  process.

### Session (`EvitaClientSession`)

* Sessions are created against a catalog with `SessionTraits` flags (read-write, dry-run, binary).
* The query API mirrors Java: `QueryOne`/`QueryList`/`Query` with typed convenience wrappers
  (`QueryOneSealedEntity`, `QueryListOfEntityReferences`, …), all with async counterparts.
* A read-write session on a live catalog opens a transaction lazily on the first mutating call
  (`CreateAndInitTransaction`); `ExecuteInTransactionIfPossible(Async)` wraps the mutation logic.
* Closing a transactional session exposes the server's three commit phases through
  `CommitProgress` (`OnConflictResolved`, `OnWalAppended`, `OnChangesVisible` —
  `TaskCompletionSource`-backed tasks completed from the `CloseWithProgress` stream). `CloseWhen`
  selects the phase to await according to the configured `CommitBehavior`. A rollback faults all
  phase tasks — this matches the Java contract (exceptional completion).
* Entity schemas are cached per catalog in `EvitaEntitySchemaCache` and invalidated by schema-mutating
  calls and by version information piggy-backed on responses.

### Change data capture

CDC streams are `IAsyncEnumerable<T>` (`RegisterChangeCatalogCaptureAsync`,
`RegisterSystemChangeCaptureAsync`, `GetMutationsHistoryAsync`), reading gRPC server streams on the
shared streaming channel. The first server message of a capture registration is the acknowledgement and
is consumed internally; heartbeats keep the stream alive. gRPC .NET's pull-based reader acts as the
credit mechanism, so there is no explicit `request(n)` protocol like in Java's Flow-based publisher.
`GetMutationsHistory` (sync) is a lazy blocking facade over the async stream — mirroring the Java
driver's `Stream<ChangeCatalogCapture>` return type. Capture body conversion is best-effort: mutation
types the client does not model yet degrade to header-only captures instead of crashing the stream.

### Converters (`Converters/`)

Converters are static classes mirroring the Java shared module
(`evita_external_api_grpc/shared/.../requestResponse/…`):

* `EntityConverter`, `EntitySchemaConverter`, `CatalogSchemaConverter` — data & schema DTOs,
* `DelegatingEntityMutationConverter`, `DelegatingLocalMutationConverter` + per-mutation converters,
* `ResponseConverter` — query responses and extra results,
* `EvitaEnumConverter` — all enum conversions plus the scoped-field fallback helpers,
* `EvitaDataTypesConverter` — evita data types (ranges, big decimals, complex data objects, …),
* `ChangeCaptureConverter` — CDC requests/responses.

Conversion of deprecated wire fields follows a strict policy described in
[wire-compatibility.md](wire-compatibility.md).

## Equality & comparison contracts

The model implements Java's `differsFrom` contract as `DiffersFrom` default interface methods
(`IEntity`, `IReference`, `IPrice`, `IAttributes`, `IAssociatedData`). Class implementations delegate to
the static helper `IEntity.AnyEntityDataDifferBetween` — never to `(this as IEntity).DiffersFrom(...)`,
which dispatches back to the class override and recurses infinitely.

When porting Java equality code, watch for the recurring bug family documented in
[upgrading-evitadb.md](upgrading-evitadb.md#known-porting-pitfalls): Java's structural
`Map.equals`/`Arrays.equals`/`Set.equals` versus C# reference-equality defaults, and `this == other`
inside a C# `record`'s strongly-typed `Equals` (recurses through the synthesized `operator ==` —
use `ReferenceEquals`).
