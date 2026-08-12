# evitaDB C# client

The official .NET driver for [evitaDB](https://evitadb.io), talking to the server over gRPC.
This repo: https://github.com/FgForrest/evitaDB-C-Sharp-client · Server: https://github.com/FgForrest/evitaDB

Targets .NET 10; currently tracks evitaDB **2026.2.4** (see `AdvertisedClientVersion` in
`EvitaDB.Client/Interceptors/ClientInterceptor.cs` — that constant is authoritative).

## Source of truth

The **Java implementation is the source of truth** for protocol and behavior — never invent it; find
the Java counterpart and mirror it in idiomatic C# (not 1:1 — records, default interface methods,
`Task` instead of `CompletionStage`). Module mapping (Java → C#):

- `evita_external_api_grpc/shared/src/main/proto` → `EvitaDB.Client/Protos`
- shared gRPC converters (`.../grpc/requestResponse/**`) → `EvitaDB.Client/Converters/**`
- `evita_java_driver` → `EvitaDB.Client/EvitaClient*.cs`
- `evita_api` (contracts, mutations, query DSL) → `EvitaDB.Client/Models`, `EvitaDB.Client/Queries`
- Java driver tests (`evita_test/.../driver/*Test.java`) → `EvitaDB.Test/Tests` (xUnit, same style)

A local clone of the evitaDB repo with all tags is expected (ask the user for its path;
historically `~/www/evita/evitaDB`).

## Build & test

```shell
dotnet build EvitaDB.slnx
dotnet test EvitaDB.Test/EvitaDB.Test.csproj   # needs a Docker daemon (Testcontainers)
```

- `EVITA_IMAGE_TAG` overrides the server image tag (default `2026.2.4`); `EVITA_DEMO_HOST`/`EVITA_DEMO_PORT`
  redirect the demo-dataset suite (default `demo.evitadb.io:5555`, needs network).
- Test parallelization is disabled by design; fixtures re-seed the catalog per test and refresh
  `SetupFixture.CreatedEntities` — cached entities are only comparable within one seeding round.
- Four write tests are skipped due to documented 2026.2.4 server bugs — see `documentation/testing.md`.

## Hard rules

1. **Strict warnings.** `EvitaDB.Client` builds with `TreatWarningsAsErrors` and **no CS0612/CS0618
   exclusion**. Deliberate deprecated-wire-field access gets the narrowest `#pragma` with a reason
   naming the server version boundary (see `documentation/wire-compatibility.md`). Never re-add
   `WarningsNotAsErrors` — a new obsolete-warning is a work item, not noise.
2. **Deprecated wire fields:** fallback-read (new scoped field first, deprecated only when empty) and
   dual-write (both forms), mirroring the Java shared module. Scopes project onto the live scope until
   the C# schema model becomes scope-aware. Helpers live in `EvitaEnumConverter`.
3. **Async core, sync facade.** Async methods (`XxxAsync`, `CancellationToken` last) are the
   implementation calling the generated async stubs; sync methods wrap via `.GetAwaiter().GetResult()`.
   Streaming APIs are `IAsyncEnumerable` (`MoveNextTranslated` pattern). See `documentation/async-api.md`.
4. **Build gRPC stubs on `channel.Invoker`** (interceptor channel), never the raw channel — otherwise
   the `sessionId`/`clientVersion` headers are dropped and calls fail UNAUTHENTICATED.
5. **Protos are pinned** to the targeted evitaDB release tag. One documented local deviation exists:
   message `DataItemMap` is renamed `GrpcDataItemMap` (wire-compatible, C# name-collision fix) —
   preserve it when re-syncing protos.
6. **Tests assert equivalence via the model's `DiffersFrom`** contract, not an assertion library.
   FluentAssertions was removed on purpose (v8+ commercial license) — do not reintroduce it.
7. Don't trust `…WithProgress` streams alone — the server may abort them while the operation
   continues; pair with effect-polling (see `DrainProgressStreamAsync` usage in `EvitaClient`).

## Pitfalls to check before blaming the server

Java→C# equality porting bugs are a recurring latent family here (reference vs structural equality,
record `==` recursion, interface default-method self-delegation, method-group comparison, value-type
arrays missed by `is object[]`). If a comparison behaves oddly, read
`documentation/upgrading-evitadb.md#known-porting-pitfalls` first. Failure triage rule: reproduce or
read the Java driver's handling before classifying anything as a server bug.

## Where to look

- `documentation/` — architecture, async conventions, wire compatibility, testing, upgrade guide.
- `/evitadb-version-upgrade` skill — the step-by-step process for adapting to a new evitaDB release.
- `IMPLEMENTATION_PLAN.md` — history and status of the 2024.4 → 2026.2.4 migration.
