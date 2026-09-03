# Upgrading the driver to a newer evitaDB version

This is the distilled process from the 2024.4 → 2026.2.4 migration. Following it in order keeps the
upgrade mechanical. A Claude Code skill automating this checklist lives in
[`.claude/skills/evitadb-version-upgrade/`](../.claude/skills/evitadb-version-upgrade/SKILL.md).

Throughout: **the Java implementation is the source of truth.** Have the evitaDB repository cloned with
all tags fetched. The relevant modules:

| Java module | Mirrors to |
|---|---|
| `evita_external_api/evita_external_api_grpc/shared/src/main/proto` | `EvitaDB.Client/Protos` |
| `evita_external_api/evita_external_api_grpc/shared/.../requestResponse/**` (converters) | `EvitaDB.Client/Converters/**` |
| `evita_java_driver` (`EvitaClient`, `EvitaClientSession`, …) | `EvitaDB.Client/EvitaClient*.cs` |
| `evita_api` (contracts, schemas, mutations, query DSL, requests) | `EvitaDB.Client/Models`, `EvitaDB.Client/Queries` |
| `evita_test/evita_functional_tests/.../driver/*Test.java` | `EvitaDB.Test/Tests` |

## 1. Scope the change

```shell
git -C <evitadb-repo> fetch --tags
# what changed on the wire between the currently pinned tag and the target
git -C <evitadb-repo> diff v<current>..v<target> --stat -- \
    evita_external_api/evita_external_api_grpc/shared/src/main/proto
# what changed in the driver itself
git -C <evitadb-repo> diff v<current>..v<target> --stat -- evita_java_driver
```

The currently pinned tag is recorded in this document's history and recoverable from
`ClientInterceptor.AdvertisedClientVersion`. Read the proto diff message by message — it is the
authoritative list of work items. Classify each change: new RPC, new message/field, deprecated field
with scoped successor, new enum value, renamed concept.

## 2. Update protos

Copy the target tag's protos over `EvitaDB.Client/Protos`, then **re-apply documented local
deviations** (grep the old protos for comments mentioning renames — currently `DataItemMap` →
`GrpcDataItemMap`, kept wire-compatible because of a C# name collision).

Build immediately: `TreatWarningsAsErrors` with no CS0612/CS0618 exclusion means **every newly
deprecated field the code still touches becomes a build error**. That error list is your converter
worklist — do not blanket-suppress it (see [wire-compatibility.md](wire-compatibility.md)).

## 3. Bump the advertised client version

Set `ClientInterceptor.AdvertisedClientVersion` to the target version. The server gates wire behavior
on it (e.g. structured associated data requires advertising ≥ 2025.4) — forgetting this makes the
server keep serving legacy shapes and hides new-format bugs.

## 4. Sync enums (do this early)

New proto enum values that lack a C# model counterpart **fail at runtime, not compile time**
(`EvitaEnumConverter` switches throw on unknown values, typically deep inside response conversion).
For every enum in the proto diff, extend the C# model enum and both directions in
`EvitaEnumConverter`. Watch for **semantic renames** (e.g. `FIRST_OCCURRENCE` → `LOWEST_PRICE`) and
value reordering — never assume ordinal stability.

## 5. Rework converters per the Java shared module

For each changed message, open the corresponding Java converter and mirror it:

* deprecated field with scoped successor → fallback-read + dual-write per
  [wire-compatibility.md](wire-compatibility.md); reuse/extend the scoped helpers in
  `EvitaEnumConverter`,
* new fields → straight port; check whether the C# model class needs new members,
* remember the C# model currently projects scopes onto the live scope only.

## 6. Port API surface changes

* **New RPCs:** implement the async core calling the async stub + sync facade, following
  [async-api.md](async-api.md). Route calls through the existing `ExecuteWith*Service(Async)` helpers
  so exception translation and channel pooling stay uniform.
* **Streaming RPCs:** `IAsyncEnumerable` + `MoveNextTranslated`; check whether the first message is an
  acknowledgement to consume internally (CDC does this).
* **Long-running operations:** if the RPC gained a `…WithProgress` variant, drain the stream but do not
  trust it — pair it with an effect-polling fallback (see `DrainProgressStreamAsync` usage), because
  the server may abort progress streams while the operation continues.
* **New contracts/constraints/mutations:** port from `evita_api` keeping C# conventions (records where
  Java uses records, default interface methods where Java uses default methods, `Task` where Java uses
  `CompletionStage`/`Progress`).

## 7. Adapt and extend tests

* Mirror new/changed Java driver tests (`evita_test/evita_functional_tests/.../driver`) into the xUnit
  suites, following existing style ([testing.md](testing.md)).
* Bump `SetupFixture.DefaultImageVersion` to the target server tag.
* Run the full suite. For each failure, decide **client bug vs server bug**: reproduce the scenario
  against the Java driver (or read its handling) before working around anything. Genuine server bugs
  get a `[Fact(Skip = "...")]` with the root cause and a note in [testing.md](testing.md).

## 8. Finalize

* README compatibility table, `documentation/` updates.
* Verify the release workflow still packs and publishes (`release.yml`).
* Check open GitHub issues that the upgrade resolves.

## Known porting pitfalls

Learned the hard way during 2024.4 → 2026.2.4:

1. **Equality bug family.** Java equality is structural; C# defaults often are not. Watch for:
   `Equals(dict, dict)` / `Equals(array, array)` (reference-based — compare entries/`SequenceEqual`),
   `ISet.Equals` (use `SetEquals`), missing `Equals` overrides on ported classes (`Range<T>`),
   `this == other` inside a `record`'s strongly-typed `Equals` (recurses via synthesized `operator ==`
   — use `ReferenceEquals`), class methods delegating to a same-signature interface default method via
   `(this as IFace).Method()` (self-recursion — hoist the default body into a static interface helper),
   bare method-group comparisons (`A != B` compiles since C# 10 and is always true), and
   `value is object[]` missing value-type arrays (test with `DateTimeOffset[]`).
   The same family covers `DiffersFrom`: Java's `differsFrom` on a `Droppable` value ends with
   `this.dropped != other.dropped`, and dropping that clause is invisible until something is *removed* —
   a removal keeps the value and only raises the tombstone, so the tombstone compares equal to the value
   it removes and the read falls back to the pre-removal value. `AttributeValue` and `AssociatedDataValue`
   both had it missing; `IPrice`, `IReference` and `GroupEntityReference` did not.
2. **Stubs must be built on the interceptor channel** (`channel.Invoker`), never the raw channel —
   otherwise calls go out without `sessionId`/`clientVersion` headers and fail `UNAUTHENTICATED`.
3. **Session identity:** sessions are identified by the `sessionId` header alone; catalog name travels
   in request payloads where needed (e.g. `GrpcCloseRequest`).
4. **CDC:** first message of a capture registration is the ack; unmodelled mutation types must degrade
   to header-only capture, not crash the stream.
5. **Restored/attached catalogs may arrive `INACTIVE`** since 2026 — activate them explicitly.
6. **Random seed data:** fixture entity caches are only valid for the current seeding round — refresh
   caches whenever the catalog is re-seeded.
7. **`Map.put` is not `Dictionary.Add`.** `put` overwrites, `Add` throws on a key that is already
   present. Every `Existing*Builder` registered its change set with `Add`, so the *second* write to the
   same attribute, associated data, price or reference threw `ArgumentException: An item with the same
   key has already been added` — and in `buildChangeSet`, where the accumulator is seeded from the base
   entity, the *first* write to an existing value threw. Use the indexer (`dict[key] = value`) wherever
   Java uses `put`. `putIfAbsent` is the one that maps to a guarded `Add`.
8. **Builder reads must go through the pending mutations.** Several `Existing*Builder` accessors were
   ported as straight delegations to the base container, which silently ignores everything written to the
   builder (`ExistingAssociatedDataBuilder.GetAssociatedData<T>`), or as "answer only if this key has a
   mutation", which makes every *untouched* value read back as `null` once anything else is written
   (`ExistingAttributesBuilder.GetAttributeValueInternal`). Java's shape is: take the base value, apply
   this key's mutation if there is one, keep the base when the result does not differ, fall back to
   mutating `null` when there is no base value at all, then filter the result through the container's
   predicate (which defaults to `Droppable::exists`). The **collection** accessors
   (`getAttributeValues()`, `getAssociatedDataValues()`, and the names/keys/locales derived from them)
   filter through that predicate too — but the `…WithoutPredicate` helper they build on must keep the
   tombstones, because `Build()` needs them.
9. **A guarded property is not a field.** Java reads bare fields in places where the C# port reached for
   the accessor of the same name — and several accessors assert first. `Entity.MutateEntity` reading
   `entity.Parent` (which asserts `WithHierarchy`) instead of the backing field made `ToInstance()` throw
   for every non-hierarchical entity opened for write.
10. **A wire field the port re-derives locally will drift.** `GrpcFacetStatistics.hasSense` was on the
    wire and read by nothing: `RequestImpact` computed `HasSense => MatchCount > 0` instead. The server's
    rule is `matchCount > 0 && (difference != 0 || hasSenseAlone)`, so an option that matches something
    but would change nothing — every match already in the result, and nothing of its own within its
    group — came back "worth offering" from the client and "not worth offering" from the server. Nothing
    failed; a facet panel just kept offering options that do nothing. When converting a message, account
    for **every** field it carries: one the client answers for itself is the one that silently diverges
    on the next release. (Same family as a discarded exception: `Attributes` had seven
    `CreateAttributeNotFoundException(name);` statements whose result was never thrown, so an unknown
    attribute read `null` instead of failing.)
