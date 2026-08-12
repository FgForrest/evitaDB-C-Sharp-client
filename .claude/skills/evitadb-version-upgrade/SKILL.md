---
name: evitadb-version-upgrade
description: Adapt the evitaDB C# driver to a newer evitaDB server version - proto sync, converter rework, API porting from the Java driver, and full test verification. Use when the user asks to upgrade/catch up the client to a new evitaDB release or to port protocol changes.
---

# evitaDB version upgrade

You are upgrading this C# driver from the currently supported evitaDB version to a newer target
version. The process is mechanical if executed in the order below. The human-readable companion is
`documentation/upgrading-evitadb.md`; conventions are in `documentation/architecture.md`,
`documentation/async-api.md`, `documentation/wire-compatibility.md` and `documentation/testing.md` —
read `wire-compatibility.md` and `upgrading-evitadb.md` before touching code.

**Golden rule: the Java implementation is the source of truth.** Never invent protocol behavior —
find the Java counterpart and mirror it in C# conventions. Do not commit or push anything without
explicit user approval.

## Prerequisites — establish these first

1. **Local evitaDB repository** with all tags. Ask the user for its path if unknown (historically
   `~/www/evita/evitaDB`). Run `git -C <repo> fetch --tags` if the target tag is missing.
2. **Current version:** read `AdvertisedClientVersion` in
   `EvitaDB.Client/Interceptors/ClientInterceptor.cs`.
3. **Target version:** from the user; confirm the tag exists (`git -C <repo> tag -l 'v<target>*'`)
   and that `evitadb/evitadb:<target>` is pullable.
4. **Java module map** (Java → C#):
   - `evita_external_api/evita_external_api_grpc/shared/src/main/proto` → `EvitaDB.Client/Protos`
   - shared converters `.../externalApi/grpc/requestResponse/**` → `EvitaDB.Client/Converters/**`
   - `evita_java_driver` → `EvitaDB.Client/EvitaClient*.cs`
   - `evita_api` contracts/mutations/query DSL → `EvitaDB.Client/Models`, `EvitaDB.Client/Queries`
   - driver tests `evita_test/evita_functional_tests/.../driver/*Test.java` → `EvitaDB.Test/Tests`

## Phase 1 — Scope

```shell
git -C <repo> diff v<current>..v<target> --stat -- evita_external_api/evita_external_api_grpc/shared/src/main/proto
git -C <repo> diff v<current>..v<target> --stat -- evita_java_driver
```

Read the full proto diff (not just --stat) message by message and build a written worklist
classifying every change: new RPC / new message or field / field deprecated in favor of a scoped
successor / new enum value / rename. Present the worklist to the user as the plan before editing.

## Phase 2 — Protos

1. Copy the target tag's proto files over `EvitaDB.Client/Protos`.
2. **Re-apply documented local deviations** — grep the previous protos for explanatory comments
   first. Known one: message `DataItemMap` is locally renamed `GrpcDataItemMap` (wire-compatible,
   avoids C# name collision). Losing such renames breaks the build or, worse, the wire.
3. Bump `AdvertisedClientVersion` to the target version — the server gates wire behavior on this
   header (e.g. structured associated data needs ≥ 2025.4). Also bump
   `DefaultImageVersion` in `EvitaDB.Test/SetupFixture.cs`.
4. Build: `dotnet build`. The client builds with `TreatWarningsAsErrors` and **no CS0612/CS0618
   exclusion** — every newly deprecated field still touched by code is now a build error. That error
   list IS the converter worklist for Phase 4. Never blanket-suppress; never re-add
   `WarningsNotAsErrors`.

## Phase 3 — Enums (early, they fail at runtime)

New proto enum values missing from C# model enums throw at runtime deep inside response conversion,
not at compile time. For every enum in the proto diff: extend the model enum and both switch
directions in `EvitaDB.Client/Converters/Models/EvitaEnumConverter.cs`. Watch for semantic renames
(e.g. `FIRST_OCCURRENCE` → `LOWEST_PRICE`) — map by meaning, verify against the Java
`EvitaEnumConverter`, never by ordinal.

## Phase 4 — Converters

For each build error / changed message, open the Java converter counterpart and mirror it:

- **Deprecated field with scoped successor:** fallback-read (new field first, deprecated only when
  the new list is empty) + dual-write (both forms), per `documentation/wire-compatibility.md`. Reuse
  the scoped helpers in `EvitaEnumConverter` (`ToScopedBooleanFlag`, `ToReferenceIndexedFlag`, scoped
  uniqueness overloads); add new helpers in the same style when a new fallback chain appears.
- The C# schema model projects scopes onto the **live scope** (a `true` flag ⇄ `[ScopeLive]`) until
  full scope support is modeled.
- Wrap each deliberate deprecated-field access in the narrowest `#pragma warning disable CS0612`
  with the reason format used across the codebase (`...read as fallback for servers older than
  <version>` / `...dual-written for servers older than <version>`).

## Phase 5 — API surface

- **New RPCs:** async core calling the generated async stub + sync facade via
  `.GetAwaiter().GetResult()`, `CancellationToken` last parameter — see
  `documentation/async-api.md`. Route through the existing `ExecuteWith*Service(Async)` helpers.
  Build stubs on `channel.Invoker` (the interceptor channel), NEVER the raw channel — raw channels
  drop the `sessionId`/`clientVersion` headers and fail UNAUTHENTICATED.
- **Streaming RPCs:** `IAsyncEnumerable<T>` + the `MoveNextTranslated` pattern; check whether the
  first server message is an acknowledgement to consume internally (CDC registration is).
- **Long-running catalog operations** (`…WithProgress`): drain the progress stream but don't trust
  it — servers abort streams while operations continue; pair with effect-polling fallbacks (see
  `DrainProgressStreamAsync` / `WaitForCatalogNamesConditionAsync` usage).
- **New contracts/constraints/mutations:** port from `evita_api`, keeping C# conventions.

## Phase 6 — Tests

1. Mirror new/changed Java driver tests into the xUnit suites, following existing style
   (`documentation/testing.md`). Extend `EvitaClientAsyncTest` for new async surface with real logic.
2. Run: `dotnet test EvitaDB.Test/EvitaDB.Test.csproj` (needs Docker; image tag override:
   `EVITA_IMAGE_TAG`). The demo suite needs network to `demo.evitadb.io` or an
   `EVITA_DEMO_HOST`/`EVITA_DEMO_PORT` override.
3. Re-check the four `[Fact(Skip=...)]` tests in `EvitaClientWriteTest` (catalog/collection
   rename+replace — known server bugs as of 2026.2.4): try un-skipping against the new server; keep
   skipped with updated notes if still broken.
4. **Failure triage — client bug vs server bug:** before working around any failure, check how the
   Java driver handles the same scenario (read its code/tests; a JDWP debugging plugin may be
   available for stepping through a live Java server). Only classify as a server bug with evidence
   the Java driver would hit it too; then skip the test with the root cause documented in
   `documentation/testing.md`.
5. **Comparison failures smell like porting bugs, not data bugs.** If an equality/`DiffersFrom`
   assertion fails oddly (stack overflow, always-equal, always-different), check the known
   Java→C# equality pitfalls in `documentation/upgrading-evitadb.md#known-porting-pitfalls`
   (reference vs structural equality, record `==` recursion, interface default-method
   self-delegation, method-group comparison, value-type arrays) before suspecting the server.

## Phase 7 — Finalize

- Zero-warning build (`dotnet build` → `0 Warning(s), 0 Error(s)`), full green test run (modulo
  documented skips and environment-blocked demo tests — report both explicitly).
- Update: README compatibility table, `documentation/wire-compatibility.md` (new version boundaries,
  new intentional gaps), `documentation/testing.md` (skips), `IMPLEMENTATION_PLAN.md` if present.
- Verify `.github/workflows/release.yml` still matches the project (SDK version, project paths).
- List GitHub issues the upgrade resolves; report everything to the user. Do not commit unless the
  user approves.
