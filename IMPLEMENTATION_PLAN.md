# Implementation Plan — Catching up the C# driver to evitaDB 2026.2.4

Target: port all driver-relevant functionality from evitaDB **v2024.4 → v2026.2.4** to this
repository, add **async counterparts** for all network-layer calls, and upgrade to **.NET 10**.
Reference implementation: the Java driver in `~/www/evita/evitaDB`
(`evita_external_api/evita_external_api_grpc/client`, branch `dev`, tags available for diffing).

---

## 1. Where we actually are (baseline assessment)

- HEAD (`aab5932`) already contains committed 2024.x work: `LOWEST_PRICE` rename, new
  transaction handling / `CommitBehavior`, armeria adaptations, facet-summary formatting.
- On top of that sit **~167 uncommitted files with real content changes** (the reported 662 is
  inflated by 657 file-mode-only changes from the OrbStack mount). They already cover:
  - CDC rename `GrpcChangeDataCapture` → `GrpcChangeCapture` + new `Models/Cdc/*` records
  - `ReflectedReferenceSchema` (+ builder/editor interfaces, `CreateReflectedReferenceSchemaMutation`)
  - New enum conversions (CommitBehavior, CaptureArea, ContainerType, AttributeInheritanceBehavior, …)
  - Session `CatalogId`, mutation-predicate infrastructure, `TransactionMutation`
- **Protos in `EvitaDB.Client/Protos` were already refreshed to a ~2026.x state.** Remaining
  drift vs upstream: 23 files with residual differences and **5 missing files**:
  `GrpcQueryParam.proto`, `GrpcEngineMutation.proto`, `GrpcInfrastrutureMutation.proto` (sic),
  `GrpcEvitaTrafficRecordingAPI.proto`, `GrpcTrafficRecording.proto`.
- Environment note: this Linux workspace has Docker but **no dotnet SDK on PATH** — install
  .NET 10 SDK (or run builds on the macOS side / in a container) before Phase 1.

### Known defects to fix before anything else (Phase 0)
| Defect | Location |
|---|---|
| 5× CS8604 nullability errors (build breaks under `TreatWarningsAsErrors`) | `Converters/DataTypes/ChangeCaptureConverter.cs:128-130,148-149` |
| Constructor validates but never assigns `Area`/`Site` → every builder-produced criteria is empty | `Models/Cdc/ChangeCatalogCaptureCriteria.cs:11-24` |
| 4× `NotImplementedException` stubs | `CreateReflectedReferenceSchemaMutation.cs:47,52`, `MutationEntitySchemaAccessor.cs:35`, `SetReferenceGroupMutation.cs:37`, `DelegatingSortableAttributeCompoundSchemaMutationConverter.cs:71` |
| Orphaned CDC types with no transport (`ChangeSystemCapture*` records reference deleted proto) | `Models/Cdc/ChangeSystemCapture.cs`, `ChangeSystemCaptureRequest.cs` — will get transport in Phase 8 |
| Filename typo | `Models/Schemas/AttribueInheritanceBehavior.cs` → `AttributeInheritanceBehavior.cs` |
| Orphan project GUID block | `EvitaDB.sln` (`{796BD828-…}` has no matching `Project(...)`) |
| 657-file mode churn obscures real diffs | consider `git config core.fileMode false` |

---

## 2. Key cross-cutting decisions

### 2.1 Pin protos to tag `v2026.2.4`
Copy all 34 proto files verbatim from
`evita_external_api/evita_external_api_grpc/shared/src/main/resources/META-INF/io/evitadb/externalApi/grpc/`
at **tag `v2026.2.4`** (not `dev` — dev has moved past the target). Keep upstream file names,
including the misspelled `GrpcInfrastrutureMutation.proto` (import paths depend on it). Add
`option csharp_namespace` adjustments only if the current generation setup requires them.

### 2.2 The `clientVersion` header is a hard prerequisite (wire-shape gate)
The server gates associated-data encoding on the client-advertised version
(`isAtLeast(clientVersion, 2025, 4)`):
- **≥ 2025.4** → associated data arrive as a structured `GrpcDataItem` tree (`root` + `type`)
- **< 2025.4 / absent** → legacy `jsonValue` string

Plan: send `clientVersion` (semver format `YYYY.M[.patch]`) on every call **and implement the
`GrpcDataItem` reader/writer** (Phase 4). Until Phase 4 lands, the interim builds must advertise
a version `< 2025.4` to stay on the JSON path — make the advertised version a single constant.
Also port the client-newer-than-server guard (`IncompatibleClientException`).

### 2.3 Sessions are no longer catalog-scoped
- `catalogName` metadata header is gone; session registry keyed by session UUID only
- `GetSessionById(catalogName, uuid)` → `GetSessionById(uuid)`
- `catalogName` moves into `GrpcCloseRequest` / `GrpcCloseWithProgressRequest` bodies
- `GrpcEvitaSessionResponse.catalogId` → already exposed as `EvitaClientSession.CatalogId`

### 2.4 Async design (TASK.md requirement) — async core, sync facade
Today the client has effectively **zero async surface** (only `EvitaClient.Create`); every RPC
goes through blocking stubs via `ExecuteWithEvitaService` (`EvitaClient.cs:474`,
`EvitaClientSession.cs:142`), whose value is the shared `RpcException` → domain-exception
translation.

To avoid duplication, invert the layering:
1. Make the executor async-native:
   `Task<T> ExecuteWithEvitaServiceAsync<TStub,T>(supplier, stubBuilder, Func<TStub, Task<T>> logic, CancellationToken ct)`
   containing the single copy of channel acquisition + exception translation (translate from
   `RpcException` both directly and unwrapped from `AggregateException`).
2. Implement every public operation as the `…Async` variant calling the generated async stubs
   (`.QueryAsync(...)`, `.GetEntityAsync(...)`, …) with `CancellationToken` parameters
   (default `default`).
3. Keep the existing sync methods as thin facades over the async ones (single
   `.GetAwaiter().GetResult()` chokepoint in the executor, not per-method).
4. Server-streaming RPCs get `IAsyncEnumerable<T>` shapes; sync counterparts only where they
   already exist today (e.g. `GetMutationHistory` returning a materialized list).
5. Mirror the Java three-channel deadline split: unary (short timeout), streaming (long
   timeout), CDC (no deadline, heartbeat-driven). Do **not** set deadlines on CDC calls.

### 2.5 Follow C# conventions, not 1:1 Java
Per CLAUDE.md: same functionality, C# idioms — `Task`/`ValueTask` instead of
`CompletableFuture`, `IAsyncEnumerable`/`System.Threading.Channels` instead of Reactive
Streams `Flow.Publisher`, `IProgress<int>`/events instead of `IntConsumer` listeners,
records for DTOs, xUnit tests following the existing style.

---

## 3. Phases

Ordering rationale: fix the build → land the toolchain (so every later line is written against
final analyzers/packages) → sync protos + transport (everything depends on the wire) → async
infra (so new surface is written async-first once) → read path → write path → query language →
management → CDC. Each phase ends green (`dotnet build` + relevant tests).

### Phase 0 — Repo hygiene & build fix (small)
- [ ] Fix the 5 CS8604 sites in `ChangeCaptureConverter.cs` (null-guard the `repeated`-field initializers)
- [ ] Fix `ChangeCatalogCaptureCriteria` non-assigning constructor
- [ ] Rename `AttribueInheritanceBehavior.cs`; remove orphan sln GUID block
- [ ] `git config core.fileMode false`; commit the current WIP as a baseline commit on `catching-up`
- [ ] Decide fate of the 4 `NotImplementedException` stubs: implement now if trivial, else tag with issue refs (reflected-reference ones complete in Phase 5)

### Phase 1 — .NET 10 + toolchain + CI (small/medium, mechanical)
- [ ] `net8.0` → `net10.0` in all three csprojs; drop `<LangVersion>12</LangVersion>` (use TFM default)
- [ ] Package bumps: `Google.Protobuf` 3.24.4 → current, `Grpc.Net.Client` 2.57 → current, `Grpc.Tools` 2.58 → current, `Microsoft.NET.Test.Sdk` 17.3.2 → ≥17.12, `Testcontainers` 3.9 → 4.x, `xunit` stack, `OpenTelemetry.*` (get off the `1.6.0-beta.3` pin + `NoWarn=NU5104`), `System.CodeDom`, `Microsoft.CodeAnalysis.CSharp` (QueryValidator)
- [ ] Warning-cleanup pass — new SDK analyzers will fire under `TreatWarningsAsErrors=true`
- [ ] CI: `dotnet-version: '10.0.x'` in both workflows; fix hardcoded `net8.0` publish paths in `release.yml` zip steps (or decouple via `-p:PublishDir=`); `actions/checkout@v4+`, `setup-dotnet@v4`, `release-drafter@v6`, replace archived `upload-release-asset@v1` with `gh release upload`; add `pull_request`/`catching-up` trigger to `dev-test.yml`
- [ ] Verify proto regeneration works under new `Grpc.Tools`

### Phase 2 — Proto sync + transport & configuration layer (medium)
- [ ] Copy all 34 protos from tag `v2026.2.4` (adds the 5 missing files; resolves the 23 residual drifts; `GrpcQueryParam` moves to its own file — import-only, field numbers preserved)
- [ ] Enum/message renames ripple into C#: `GrpcSetReferenceSchemaFilterableMutation` → `…IndexedMutation`, `GrpcAttributeSchemaType.GLOBAL/ENTITY/REFERENCE` → `*_SCHEMA`, `GrpcCommitBehavior.WAIT_FOR_INDEX_PROPAGATION` → `WAIT_FOR_CHANGES_VISIBLE`
- [ ] `ClientSessionInterceptor`: drop `catalogName` header, add `clientVersion` (§2.2), keep `sessionId`/`clientId`
- [ ] Version compatibility check on connect (`ServerStatus` + SemVer compare → `IncompatibleClientException`; SNAPSHOT → warning)
- [ ] `EvitaClientConfiguration` restructure mirroring Java 2026.1: nested `ConnectionOptions` (host, port **5555**, systemApiPort **5555**, `pingIntervalMillis=30000`, `idleTimeoutMillis=300000`), `TlsOptions` (`tlsEnabled` toggle for h2c/`FORCE_NO_TLS` endpoints, mTLS, generated-cert trust), `TimeoutOptions` (unary 5 s, streaming 300 s), plus `Retry`, `TrackedTaskLimit`, `ChangeCaptureQueueSize`. Keep old flat ctor/properties as `[Obsolete]` facades
- [ ] Channel setup: HTTP/2 keepalive ping (`SocketsHttpHandler.KeepAlivePingDelay`, `KeepAlivePingPolicy.Always`) — Armeria kills idle connections otherwise; h2c support for `tlsEnabled=false`; three channel kinds (unary/streaming/CDC) with the deadline policy of §2.4
- [ ] Retry rules: always-on retry for provably-unsent requests; opt-in broader retry (`Retry=true`) for timeouts/503/504/429; `IsTransportFailure` classification (CANCELLED/UNAVAILABLE/DEADLINE_EXCEEDED → session lost, recreate, don't retry)
- [ ] `ClientCertificateManager`: verify bootstrap against Armeria single-port layout (`http://host:systemApiPort/system/`, `server.crt`/`client.crt`/`client.key`, per-server-name cache dir)
- [ ] New exceptions: `IncompatibleClientException`, `TransportException`, `TaskFailedException`, `PublisherClosedByClientException`, `EvitaClientPoolSaturatedException` (as applicable)
- [ ] Rewire `EvitaService`: `ServerStatus` moved to `EvitaManagementService` (stub minimal client for now), add `IsReady`; **`Update` RPC is deleted** — temporarily route `EvitaClient.Update` to `ApplyMutation(GrpcEngineMutation)` (full engine-mutation model in Phase 5)
- [ ] Point test docker image at `evitadb/evitadb:2026.2.4` (and make the tag overridable via env var, e.g. `EVITA_IMAGE_TAG`); smoke-test: create session, query, upsert, close

### Phase 3 — Async network layer (medium; TASK.md requirement)
- [ ] Async-native `ExecuteWithEvitaServiceAsync` + sync facade per §2.4, in both `EvitaClient` and `EvitaClientSession`
- [ ] `…Async(…, CancellationToken)` counterparts for every public network-touching method (sessions, queries, upserts, deletes, schema ops, catalog ops)
- [ ] `IAsyncEnumerable<T>` for server-streaming responses
- [ ] Java-parity conveniences: `QueryCatalogAsync`, `UpdateCatalogAsync` (returning `Task<T>`; `CommitProgress` overload lands in Phase 5)
- [ ] Tests: async variants of a representative subset of read/write tests + cancellation test

### Phase 4 — Read path / wire model (medium/large)
- [ ] `GrpcDataItem` tree ↔ `ComplexDataObject` converter (both directions) — then flip advertised `clientVersion` to the real version ≥ 2025.4 (§2.2)
- [ ] `Scope` enum (`Live`/`Archived`) + `GrpcEntityScope` conversions; `GetEntity` overloads with `Scope[]`; `GrpcEntityRequest.scopes`
- [ ] `GrpcSealedEntity` new fields: `scope`, `accompanyingPrices`, `priceForSaleMin/Max`, `referenceOffsetAndLimits`; `GrpcReference.internalPrimaryKey`
- [ ] `PricesContract` parity: accompanying prices, price-range-for-sale APIs, `GrpcPrice.indexed` (read `indexed`, honor deprecated `sellable`)
- [ ] `GrpcEntityReference.referenceVersion`; `GrpcPaginatedList.lastPageNumber`
- [ ] Extra results: `GrpcReferenceGroupStatistics` (replaces deprecated `facetGroupStatistics`), `GrpcFacetStatistics.hasSense`, histogram `relativeFrequency` + `min/maxReferencedEntity`, `QueryTelemetry.startedAt`
- [ ] Data types: `ReferencedEntityPredecessor` in the value marshaller (`GrpcEvitaDataType=22`); verify `ContainerType`/`IChainableType` conversions
- [ ] Schema read model: `GrpcNameVariant` lists, all `*InScopes` collections (attribute unique/filterable/sortable, reference `scopedIndexTypes`/`scopedIndexedComponents` — implement the **newest** form, the intermediate `indexedInScopes` is already deprecated), entity `hierarchyIndexedInScopes`/`priceIndexedInScopes`, `catalogSchemaVersion`, conflict-resolution fields, sortable-compound `indexedInScopes`+`inherited`; contract-level `IsXInScope/InAnyScope` helpers per Java contracts
- [ ] `GetCatalogSchema` request change (`Empty` → `GrpcGetCatalogSchemaRequest{nameVariants}`); `GrpcEntitySchemaRequest.nameVariants`
- [ ] Schema cache: catalog-schema-version-aware invalidation (mirror `EvitaEntitySchemaCache` — uses the new `catalogVersion`/`catalogSchemaVersion` response fields; saves a round-trip per query)
- [ ] Wrap `GetCatalogState`, `GetCatalogVersionAt` (time travel), `GetEntityCollectionSize`-adjacent gaps

### Phase 5 — Write path (large)
- [ ] `Progress<T>` abstraction (`PercentCompleted`, `Task<T> OnCompletion`, progress event) over `…WithProgress` server streams
- [ ] `CommitProgress` with three stages (`OnConflictResolved`/`OnWalAppended`/`OnChangesVisible` ← `GrpcTransactionPhase`); `CloseWithProgress`, `GoLiveAndCloseWithProgress`, `CloseWhen(CommitBehavior)`, `CloseNow`
- [ ] `GrpcCloseRequest{catalogName, rollback}` population; `catalogSchemaVersion` in close/go-live responses
- [ ] Engine mutations: `GrpcEngineMutation` model (12 arms) + `EvitaClient.ApplyMutation`/`ApplyMutationWithProgress`; catalog lifecycle ops with progress variants (rename/replace/delete, make-mutable/immutable/alive, duplicate, activate/deactivate, `GetProgress` late-join); `DefineCatalog` actually calling the RPC path it should
- [ ] `ArchiveEntity`/`RestoreEntity` (+ fetch variants), `SetEntityScopeMutation`, session `ApplyMutation(entityMutation)`
- [ ] **Reference internal PKs**: `internalPrimaryKey` on all 5 reference mutations; handle `GrpcUpsertEntityResponse.entityReferenceWithAssignedPrimaryKeys` (arm 3) and propagate remapping into builders — ignoring it silently corrupts subsequent reference mutations; duplicate-cardinality enums (`*_WITH_DUPLICATES`)
- [ ] `DeleteEntityAndItsHierarchy` → `deletedEntityPrimaryKeys`
- [ ] Schema mutations catch-up: conflict-resolution mutations (attribute/associated-data/reference overrides, catalog/entity level), `SetReferenceSchemaIndexedMutation` (renamed), `SetReferenceSchemaBucketedMutation`, scoped fields on all create/set mutations, sortable-attribute-compound mutation family, `ModifyReflectedReferenceAttributeInheritanceSchemaMutation`; complete the reflected-reference `NotImplementedException` stubs
- [ ] Builders: reflect new schema capabilities in `IEntitySchemaEditor`/`IReferenceSchemaEditor` chains (scoped uniqueness/filterability/sortability, reflected references)

### Phase 6 — Query language (large, self-contained)
`EvitaDB.Client/Queries/**` is untouched since 2024.4. For each new constraint: class +
`IQueryConstraints` factory + `PrettyPrintingVisitor` serialization + `GrpcQueryParam` arm
(new arms 22–27, 122). From the Java `evita_query` diff:
- [ ] Head: `Head`, `Label` (head-constraint container infra — today `Collection` is the only head)
- [ ] Filter: `EntityScope`, `FilterInScope`, `FacetIncludingChildren(-Except)`, `HierarchyAnyHaving`, `EntityPrimaryKeyBetween/GreaterThan(Equals)/LessThan(Equals)`, `GroupHaving`, `HistogramHaving`; remove `IndexUsingConstraint` marker if present
- [ ] Order: `PriceDiscount`, `Segment`/`Segments`/`SegmentLimit`, `OrderInScope`, `PickFirstByEntityProperty`, `TraverseByEntityProperty` (+`TraversalMode`)
- [ ] Require: `RequireInScope`, `Spacing`+`SpacingGap` (expression carried **as string** — no client-side evaluator needed), `ManagedReferencesBehaviour`, `AccompanyingPriceContent`, `DefaultAccompanyingPriceLists`, `FacetCalculationRules`, `FacetGroupsExclusivity` (+`FacetRelationType`, `FacetGroupRelationLevel`), `ReferenceSummary(OfReference)`, `ReferenceHistogramStatistics`
- [ ] Enum extensions: `HistogramBehavior.Equalized(Optimized)`, `StatisticsBase.CompleteFilterExcludingSelfInUserFilter`, `FacetStatisticsDepth.StatisticsNone`
- [ ] Tests: constraint serialization round-trips + live queries against demo dataset (segment, spacing/gap, scope) — also satisfies issue #8's documentation examples

### Phase 7 — Management API & long-running tasks (medium)
- [ ] `EvitaClientManagement` implementing all 16 `EvitaManagementService` RPCs; expose as `EvitaClient.Management()`
- [ ] Models: `TaskStatus`, `FileForFetch`, `CatalogStatistics`, server status (health problems, readiness, `api` map for endpoint discovery), engine settings, reserved keywords
- [ ] Un-park the commented enum conversions in `EvitaEnumConverter.cs:656-763` (`HealthProblem`, `Readiness`, `TaskSimplifiedState`, `TaskTrait` — protos already exist)
- [ ] `ClientTaskTracker`: polls `GetTaskStatuses` (interval + `TrackedTaskLimit`), completes `Task<FileForFetch>`-shaped handles, cancel-on-dispose → server `CancelTask`
- [ ] Backup/restore: session `BackupCatalog`/`FullBackupCatalog` (+`WithProgress`), management `RestoreCatalog` (client-streaming upload), `RestoreCatalogUnary`, `RestoreCatalogFromServerFile`, `FetchFile` (server-streaming download), `DeleteFile`, `ListFilesToFetch`
- [ ] Tests: backup → restore round-trip against Testcontainers instance; task polling

### Phase 8 — CDC / change capture (medium, protocol-sensitive)
The credit/ACK protocol is load-bearing (server can stall otherwise) — port it exactly:
- [ ] `IChangeCapturePublisher<T>` over `System.Threading.Channels` with explicit credit accounting — **not** a naive `await foreach`: first message must be `ACKNOWLEDGEMENT` (uuid + heartbeat; subscription is live only after ACK); request `ChangeCaptureQueueSize` credits up front, top up 1 per consumed message; `HEARTBEAT` messages tracked for gap detection (`index` monotonicity warning), dispatched on a serializing executor off the gRPC inbound thread; heartbeats reset the streaming deadline; `Close()` cancels the call, distinguish client- vs server-initiated (`PublisherClosedByClientException`)
- [ ] `RegisterChangeCatalogCapture` (session) and `RegisterSystemChangeCapture` (evita service) — gives the orphaned `ChangeSystemCapture*` records their transport
- [ ] New CDC model types: `ChangeCaptureBody`, `ChangeSystemCaptureCriteria`, `HostSystemEvent`, `SystemCaptureArea`, `ChangeCatalogCaptureRecords`; `CaptureContent` → `ChangeCaptureContent` rename
- [ ] WAL access: `GetMutationsHistoryPage(Forward)`, `GetMutationsHistory(Forward/Reversed)` as `IAsyncEnumerable<ChangeCatalogCapture>`, `GetTransactionOverview`
- [ ] Tests: register capture, mutate, assert received captures; heartbeat continuity

### Phase 9 — Deferred / optional
- Traffic recording service (`GrpcEvitaTrafficRecordingService`, 8 RPCs) — **the Java driver
  itself does not implement it** (consumed by evitaLab); protos generated in Phase 2, client
  optional
- `QueryOneUnsafe`/`QueryListUnsafe`/`QueryUnsafe` — unused by the Java driver, tooling-only
- Binary sessions (`CreateBinaryReadOnlySession`/`…Write…`)
- Client-side expression evaluation (expressions cross the wire as strings; evaluator is
  validation sugar only)

### Phase 10 — Test & release finalization
- [ ] Full test pass against `evitadb/evitadb:2026.2.4` (Testcontainers) + demo-dataset suite
- [ ] Runtime-overridable docker tag (env var) rather than compile-time const
- [ ] Async counterparts covered by tests mirroring existing sync tests where valuable
- [ ] README/version bump; NuGet package metadata; verify `release.yml` end-to-end
- [ ] Close issues #6, #7, #8, #10 with references

---

## 4. Sizing & sequencing summary

| Phase | Scope | Size | Depends on |
|---|---|---|---|
| 0 | Build fix + hygiene | S | — |
| 1 | .NET 10 + packages + CI | S/M | 0 |
| 2 | Protos + transport/config | M | 1 |
| 3 | Async layer | M | 2 |
| 4 | Read path | M/L | 2 (3 for shape) |
| 5 | Write path | L | 4 |
| 6 | Query language | L | 2 (parallelizable with 4/5) |
| 7 | Management + tasks | M | 3 |
| 8 | CDC | M | 3 |
| 9 | Optional | — | — |
| 10 | Tests + release | M | all |

Phase 6 is independent of 4/5 (query language is string-serialized) and can run in parallel.
Phases 7 and 8 only need the transport + async layers.

## 5. Risks & open questions

1. **Wire-format gate**: until `GrpcDataItem` lands (Phase 4), the advertised `clientVersion`
   must stay `< 2025.4` or associated data silently arrive empty. Single constant, flipped once.
2. **Reference internal-PK remapping** (Phase 5) is easy to "implement" by ignoring — but that
   silently corrupts later reference mutations. Needs a dedicated test with duplicated references.
3. **CDC backpressure protocol** must match the Java semantics (ACK-before-live, credit top-up,
   heartbeat off the inbound thread) or streams stall.
4. **Armeria keepalive**: without HTTP/2 ping the server drops idle connections
   (`ClosedSessionException` class of bugs) — set keepalive in Phase 2, test long-idle sessions.
5. **Breaking public API of the NuGet package** (config restructure, renamed enum values,
   `GetSessionById` signature): acceptable for a major version bump; keep `[Obsolete]` shims
   where cheap.
6. **`TreatWarningsAsErrors` + new SDK** will front-load a warning-cleanup cost in Phase 1.
7. **No dotnet SDK in this Linux workspace** — install .NET 10 SDK before starting (Docker is
   present for Testcontainers).
8. Should the driver keep writing deprecated wire fields (`sellable`, flat `unique/filterable/
   sortable`) for compatibility with older servers, or target ≥ 2026.x servers only?
   Java keeps deprecated fields readable but writes the new forms — recommend the same.
