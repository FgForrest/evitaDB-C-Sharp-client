# Wire compatibility

## Protocol pinning

The `.proto` files in `EvitaDB.Client/Protos` are copied from the evitaDB repository
(`evita_external_api/evita_external_api_grpc/shared/src/main/proto`) at the release tag the driver
targets (currently `v2026.2.4`). They are compiled by `Grpc.Tools` at build time
(`GrpcServices="Client"`).

Local deviations from upstream protos must be wire-compatible and documented with a comment in the
proto file. Currently there is exactly one: the message `DataItemMap` is renamed to `GrpcDataItemMap`
(same fields, same tags) because the original name collides with the C# model class `DataItemMap` in
the generated code's namespace.

## Client version advertising

`ClientInterceptor.AdvertisedClientVersion` (currently `"2026.2.4"`) is sent as the `clientVersion`
header on every call. The server uses it to decide which wire representation to produce — e.g.
structured associated data (`GrpcDataItem` tree in the `root` field) is only sent to clients
advertising ≥ 2025.4; older clients get the legacy JSON string. **Bump this constant when adopting a
new protocol version**, otherwise the server keeps serving legacy shapes.

## Deprecated wire fields: dual-write / fallback-read

evitaDB evolves the protocol by deprecating fields in place (`[deprecated = true]`) and adding scoped
successors (e.g. `filterable` → `filterableInScopes`, `GrpcReferenceSchema.indexed` →
`indexedInScopes` → `scopedIndexTypes`). The Java shared module keeps both forms alive; this driver
mirrors that policy exactly:

* **Read:** prefer the new field; fall back to the deprecated field only when the new one is
  empty/absent (messages from older servers). The fallback chains live in `EvitaEnumConverter`
  (`ToScopedBooleanFlag`, `ToReferenceIndexedFlag`, and the scoped overloads of
  `ToAttributeUniquenessType`/`ToGlobalAttributeUniquenessType`) so each converter is a one-liner.
* **Write:** set both the deprecated field and the new scoped field (dual-write), so older servers and
  consumers keep working.
* **Scope projection:** the C# schema model does not model entity scopes yet, so scoped values project
  onto the **live scope**: a `true` flag writes `[ScopeLive]`; reads check for the live-scope entry.
  When the model gains full scope support, these projections are the places to generalize.

### Pragma convention

`EvitaDB.Client.csproj` builds with `TreatWarningsAsErrors` and **no** exclusion for CS0612/CS0618.
Every deliberate touch of a deprecated wire field is wrapped in the narrowest possible pragma with a
reason that names the server version boundary:

```csharp
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
    EvitaEnumConverter.ToAttributeUniquenessType(mutation.UniqueInScopes, mutation.Unique),
#pragma warning restore CS0612
```

Reason strings in use:

* `deprecated wire fields are read as fallback for servers older than <version>`
* `deprecated wire fields are dual-written for servers older than <version>`
* `the deprecated scope list is dual-written for servers older than 2025.6` (reference index middle
  representation)
* one-off documented cases: the `facetGroupStatistics` extra result (kept until the `referenceSummary`
  requirement is ported — see below) and the associated-data `jsonValue` fallback (servers < 2025.4).

An unexpected CS0612/CS0618 **fails the build by design** — it means a new deprecation arrived with a
proto update and a conscious read/write decision is needed.

## Known intentional gaps

* `GrpcExtraResults.referenceGroupStatistics` (successor of the deprecated `facetGroupStatistics`) is
  only produced for the `referenceSummary` requirement, which this client cannot express yet. Until the
  `ReferenceSummary` constraint/extra-result family is ported, `ResponseConverter` reads the deprecated
  field — the same code path Java's backward-compatibility branch uses for `facetSummary` queries.
