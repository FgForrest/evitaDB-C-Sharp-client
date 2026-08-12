# Testing

## Layout

| Class | Fixture | What it covers |
|---|---|---|
| `EvitaClientReadTest` | `SetupFixture` (Testcontainers) | Read path: query trio, entity fetching/enrichment, extra results, CDC |
| `EvitaClientWriteTest` | `SetupFixture` | Catalog/collection lifecycle, schema definition, entity CRUD, transactions |
| `EvitaClientAsyncTest` | `SetupFixture` | Async mirrors of the most valuable read/write scenarios + `CommitProgress` + cancellation |
| `EvitaClientDemoQueryTest` | `DemoSetupFixture` | Read-only queries against the public demo dataset |
| `EvitaDataTypesTest` | — | Data type formatting |

Test parallelization is disabled assembly-wide (`CollectionBehavior(DisableTestParallelization = true)`)
— tests share containers and a mutating catalog.

## Fixtures

* `SetupFixture` starts an `evitadb/evitadb` container per suite and seeds the `testCatalog` dataset
  (`DataManipulationUtil.DeleteCreateAndSetupCatalog`). **Each test acquires a client via
  `GetClient()`, which re-seeds the catalog and refreshes the shared `CreatedEntities` cache** — the
  seed data contains random values (GUID codes, timestamps), so cached entities are only comparable to
  server data from the same seeding round.
* The container disables the REST/GraphQL/lab endpoints (`EVITA_ARGS`) because their server-side schema
  refreshers crash on the rapid catalog delete+recreate cycle these tests use (server bug) and poison
  the engine's event pipeline.
* `DemoSetupFixture` connects to `demo.evitadb.io:5555` by default; override with `EVITA_DEMO_HOST` /
  `EVITA_DEMO_PORT` when the public instance is unreachable (the suite then needs any server that hosts
  the demo dataset).

## Environment variables

| Variable | Purpose | Default |
|---|---|---|
| `EVITA_IMAGE_TAG` | Docker tag of `evitadb/evitadb` used by `SetupFixture` (e.g. `canary`) | `2026.2.4` |
| `EVITA_DEMO_HOST` / `EVITA_DEMO_PORT` | Demo-dataset server for `DemoSetupFixture` | `demo.evitadb.io` / `5555` |

## Equivalence assertions

Entity comparisons use the model's own contract — `IEntity.DiffersFrom` (Java's `differsFrom`) — not an
assertion library:

```csharp
Assert.False(cachedEntity.DiffersFrom(fetchedEntity));   // structurally equivalent
Assert.True(cachedEntity.DiffersFrom(limitedEntity));    // partial fetch differs
```

`DiffersFrom` skips containers that were not fetched on **both** sides (attributes, prices, references,
associated data), compares parents by primary key only, and is version-sensitive. When a test needs to
compare entities whose reference versions legitimately diverge (the 2026 server folds reference versions
into the group's `referenceVersion`), compare fields explicitly instead.

`FluentAssertions` was removed deliberately (v8+ requires a commercial license) — do not reintroduce it.

## Known skips

Four write tests are `[Fact(Skip = ...)]` due to confirmed evitaDB 2026.2.4 server bugs (they would fail
with the Java driver too):

* `ShouldRenameCatalog` / `ShouldReplaceCatalog` — rename/replace after transactional writes persists to
  the WAL but is not installed into the live view until server restart.
* `ShouldRenameCollection` / `ShouldReplaceCollection` — structural collection operations inside
  transactions are silently rolled back at close.

Re-check these against each new server release and un-skip when fixed.

## Running

```shell
dotnet test EvitaDB.Test/EvitaDB.Test.csproj                     # full suite (needs Docker + network)
dotnet test --filter FullyQualifiedName~EvitaClientReadTest       # one suite
EVITA_IMAGE_TAG=canary dotnet test                                # against a different server build
```
