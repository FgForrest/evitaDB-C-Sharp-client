# Blazor WebAssembly storefront demo — implementation plan

**Status: IMPLEMENTED** (2026-08-11). Phases 1–5 are in the tree: the driver seam plus the async path in
`EvitaDB.Client`, and the whole app in `EvitaDB.Storefront/`. Phase 0a was executed against the live
demo server and passed (§1a). Deviations from this plan, discovered while implementing:

* **Phase 1 was bigger than "five transport edits".** Three *blocking* gRPC calls sit on the ordinary
  read path — `CreateSession` (reached even from `QueryCatalogAsync`), and the lazily invoked
  `FetchEntitySchema` / `FetchCatalogSchema`. The driver gained `CreateSessionAsync`,
  `GetEntitySchemaAsync`, `GetEntitySchemaOrThrowAsync`, `GetCatalogSchemaAsync` and
  `EvitaEntitySchemaCache.SetEntitySchema`/`SetCatalogSchema` (which prime the *versioned* cache key,
  not just the "latest" one). See `documentation/architecture.md` § *Browser hosts*.
* **Product is not hierarchical** in this dataset, so the detail query must not request
  `hierarchyContent`; the breadcrumb reads the referenced Category entities instead.
* **`description` / `descriptionShort` are localized attributes**, not associated data — and are empty for
  every product in the demo dataset, so the detail page's description block stays hidden there.
* **Six pre-existing driver bugs had to be fixed** before any of this ran (all unrelated to the browser
  work; all four affect the desktop driver too, two of them silently):
  * `AttributeNatural(string, OrderDirection)` and `AttributeSetInFilter(string)` passed the attribute name
    as the first argument to `AbstractOrderConstraintLeaf(string? name, params object?[])`, so it was
    consumed as the **constraint name**: `attributeNatural('orderedQuantity', DESC)` serialized as
    `orderedQuantity(DESC)`, which the server rejects. Any sort other than `priceNatural` was unusable.
  * `BaseConstraint.ToString()` closed its bracket inside the `string.Join` argument, concatenating it onto
    the `IEnumerable` and printing the LINQ iterator's type name instead of the arguments.
  * `PrettyPrintingVisitor.PrintLeaf` decided argument separators by raw index while skipping null
    arguments, so a constraint with a null bound serialized as `priceBetween(?, )` (a syntax error) or -
    worse - `priceBetween(?)` with the *upper* bound silently rebound to the *lower* slot. evitaQL has no
    null literal and no empty argument slot, so a null before a printed argument now throws instead of
    querying for the wrong thing; separators are computed from the arguments actually printed.
  * `EntityConverter.ToEntities` passed **`entity.Version`** (the entity's own data version) where the
    **schema** version belongs — Java passes `entity.getSchemaVersion()`, and every other call site in
    `EvitaClientSession` gets it right. The lookup key could therefore never match, so **every query that
    returns sealed entities re-fetched the entity schema from the server**, and the re-fetch goes through
    the *blocking* accessor — fatal in WebAssembly. Verified fixed by counting RPCs: `GetEntitySchema`
    stays at 19 across repeated queries instead of growing by one each time.
  * `EvitaDataTypesConverter` did not handle `ReferencedEntityPredecessor` in any of its four conversion
    switches (and was missing `Predecessor` in the `Type` → gRPC direction entirely). The demo catalog uses
    it exactly once — `Parameter.parameterValues.orderInParameter` — which made *fetching that schema*
    throw `Unsupported Evita data type in gRPC API 'ReferencedEntityPredecessor'`.
  * `FacetSummary.GetFacetGroupStatistics()` appended to `Dictionary<,>.Values`, a **read-only view** in
    C# — `NotSupportedException` on every call. A textbook case of the Java→C# porting family in
    `upgrading-evitadb.md#known-porting-pitfalls`; the existing tests never call this accessor.
* The §7.1 category tree is asked on the Product collection via `hierarchyOfReference`, so
  `queriedEntityCount` counts products.
* Phase 6's optional xUnit smoke test against the demo host was **not** added — see the status note there.

**Verified by compiler:** `EvitaDB.Client` builds clean (0 warnings, `TreatWarningsAsErrors` on);
`EvitaDB.Test` still compiles unchanged against it; all of `EvitaDB.Storefront/Services/*.cs` type-checks
against the real driver. **Not verified:** the five `.razor` files and the exact `Grpc.Net.Client.Web`
API — the sandbox cannot restore Blazor packages (see the memory note); the user builds those locally.
**Goal:** a new project in this repository — a *pure* Blazor WebAssembly app (no backend of its own)
that talks **directly from the browser** to `demo.evitadb.io` (**port 443**, see §1a) over
**gRPC-Web**, using the **local `EvitaDB.Client` driver** (project reference, not NuGet), and renders
a small e-commerce storefront over the public `evita` demo catalog.

---

## 1a. Live verification against demo.evitadb.io (executed, 2026-08-11)

Driven with a throwaway gRPC-Web client speaking the JSON serialization format
(`application/grpc-web+json`, 5-byte frame prefix) — no .NET SDK was needed to prove the wire.

| Checked | Result |
| --- | --- |
| **Transport** | `POST https://demo.evitadb.io/io.evitadb.externalApi.grpc.generated.EvitaManagementService/ServerStatus` → `HTTP/2 200`, `content-type: application/grpc-web+proto`, `server: Armeria/1.40.0`, valid framed response. **gRPC-Web works.** |
| **Port** | **443, not 5555.** The demo sits behind a front proxy: `https://demo.evitadb.io/` → `302 → /lab`, and the gRPC path answers on 443. The server's *self-reported* API URLs still say `:6555` (`ServerStatus.apis`), and `:5555` is what the docs advertise — but 443 is what a browser can actually reach, and needs no non-standard port. **Use 443.** |
| **CORS preflight** | `OPTIONS` with `origin: http://localhost:5000` → `200`, `access-control-allow-origin: *`, `access-control-allow-methods: POST`, `access-control-allow-headers` echoing `content-type,x-grpc-web,clientversion,clientid,sessionid`. |
| **CORS trailers** | On the *actual* POST: `access-control-expose-headers: grpc-status,grpc-message,armeria.grpc.throwableproto-bin`. Without this the client could never read a status — **green**. |
| **Server version** | `2026.2-SNAPSHOT`, server name `evitaDB-demo`. |
| **Risk #0 (version rejection) — resolved** | `EvitaClient.ParseVersion` returns `null` for anything containing `SNAPSHOT`, and `VerifyServerCompatibilityAsync` returns early when either side is unparseable. The `2026.2.4` client will **not** be rejected. |
| **Session lifecycle** | `EvitaService/CreateReadOnlySession {catalogName:"evita"}` → `sessionId`, `catalogState: ALIVE`. Subsequent calls authenticate with the `sessionid` header alone. |
| **Facet summary round-trip** | The §7.2-shaped query returns `extraResults.facetGroupStatistics` — **the deprecated field the C# `ResponseConverter` reads** — with 78 group entries across `brand` (57 facets) and `parameterValues` (77 groups). The driver's existing facet path is therefore live-compatible. |
| **Facet IMPACT depth** | 929 facets carried `impact` — e.g. `{"count":213,"impact":-3850,"matchCount":213,"hasSense":true}`. |
| **Price histogram** | `priceHistogram(10)` → 10 buckets, `min 0.00`, `max 5036.00`, each bucket `{threshold, occurrences, requested, relativeFrequency}`. |
| **Category hierarchy** | `hierarchyOfSelf(fromRoot('menu', …, stopAt(level(2)), statistics(CHILDREN_COUNT, QUERIED_ENTITY_COUNT)))` → `extraResults.selfHierarchy.hierarchy.menu.levelInfos`, e.g. *Portables* (childrenCount 3) → *Cell phones*, *Tablets*, with localized `name` and `url` (`/en/portables`, `/en/cell-phones`). |
| **Scale** | 4063 products match `entityLocaleEquals('en') + priceInCurrency('EUR') + priceInPriceLists('basic') + priceValidInNow()`; 1332 within category `portables`. |

### Dataset facts — replace the guesses in §6/§7 with these

* **Locales: `cs`, `de`, `en`** — plain language tags, *not* `en-US`/`cs-CZ` as in
  `EvitaClientDemoQueryTest`. Use `new CultureInfo("en")`.
* **Currencies: `CZK`, `EUR`.** Price scale: 2 decimal places.
* **Price lists (20):** `basic`, `reference`, `vip-group-1-level`, `vip-group-2-level`,
  `vip-group-3-level`, `christmas-prices`, `b2b-basic-price`, `b2b-reference-price`,
  `b2b-vip-group-{1,2,3}-level`, `employee-basic-price`, `employee-reference-price`,
  `employee-group-{1,2}-level`, `management-price`, `shareholders-basic-price`,
  `shareholders-reference-price`, `shareholders-group-{1,2}-level`. These map beautifully onto the
  task's "derived from user profile" selector: retail (`basic` + `reference` for the struck-through
  price) vs `b2b-*` vs `employee-*`.
* **Faceted references on `Product`** (`facetedInScopes: [SCOPE_LIVE]`): `parameterValues`
  (group → `Parameter`), `brand`, `tags` (group → `TagCategory`), `categories`, `groups`,
  `variantParameters`, `stocks`. Non-faceted but useful: `relatedProducts`, `media`, `variants`,
  `master`, `bundles`. **`brand` and `tags` — previously guesses — are real and faceted.**

### ⚠ The facet-relation trap (measured, not inferred)

Within category `portables`, two facets of the **same** group (`Parameter` 66547):

| userFilter | products | relation |
| --- | --- | --- |
| `facetHaving('parameterValues', entityPrimaryKeyInSet(103894))` | 1211 | — |
| `facetHaving('parameterValues', entityPrimaryKeyInSet(103872))` | 1173 | — |
| `facetHaving('parameterValues', entityPrimaryKeyInSet(103894,103872))` — **one** constraint | **1291** | **OR** ✔ union |
| two **separate** `facetHaving` constraints, same reference | **1093** | **AND** ✘ intersection |
| `entityPrimaryKeyInSet(105256,105701)` — one constraint, **different** groups | 0 | **AND** ✔ |

**Therefore: emit exactly ONE `facetHaving` per reference, carrying every selected facet id for that
reference.** The engine then applies OR within a group and AND between groups — precisely what the
task asks for, with no `FacetGroupsConjunction`/`Disjunction` needed. Building the filter the natural
way — one `facetHaving` per ticked checkbox — silently ANDs everything and produces a facet panel
that looks right and is wrong. This is the single highest-risk detail in the whole implementation.

Also confirmed: after selecting facet `103894`, the returned summary flags exactly that facet with
`requested: true` — so checkbox state can be driven straight off `FacetStatistics.Requested` rather
than tracked separately.

### Badge counts are trustworthy — with one measured exception

Sampled across the extremes of the `portables` summary, a facet's summary `count` equals exactly the
number of products you get when you select it alone:

| facet | count | matchCount | actual when selected |
| --- | --- | --- | --- |
| 103894 | 1211 | 1211 | 1211 ✔ |
| 103872 | 1173 | 1173 | 1173 ✔ |
| 103898 | 1168 | 1168 | 1168 ✔ |
| 103881 | 1 | 1 | 1 ✔ |
| 104460 | 1 | 1 | 1 ✔ |

**The exception: 12 of the 273 distinct facets (~4%) are listed under _two_ group entries** — the same
`ParameterValue` is assigned to two different `Parameter` groups in the demo data. Because the engine
ANDs across groups, selecting such a facet applies both group constraints at once and returns **0**:

| facet | listed as | actual when selected |
| --- | --- | --- |
| 105701 | group 66461 (count 6), group 114004 (count 1) | **0** |
| 105726 | group 66461 (count 4), group 114004 (count 26) | **0** |
| 106714 | group 66461 (count 1), group 114004 (count 1) | **0** |

This is a demo-dataset quirk, not a client bug, and it is the correct engine answer. Consequences for
the implementation:

* Rendering "badge = `FacetStatistics.Count`" is **correct** — do it, and don't build a workaround.
* The panel iterates group entries, so a duplicated facet legitimately appears twice under different
  group headings. That reads as a rendering bug but isn't; leave it.
* Phase 6's checklist must list these three ids so a tester who clicks a dead-end facet doesn't file
  it as a regression.

### Attribute histograms — verified, with real attribute names

`Product` declares **27+ numeric (`BIG_DECIMAL`) filterable, non-localized attributes**, all
`filterableInScopes: [SCOPE_LIVE]`: `battery-capacity`, `battery-life`, `display-size`, `weight`,
`weight-in-kg`, `width`, `height-in-mm`, `depth`, `length`, `cpu-frequency`, `refresh-rate`,
`response-time`, `snr`, `front-camera-resolut`, `rear-camera-resoluti`, `frequency-from`/`-to`, …

`attributeHistogram(10, 'battery-capacity', …)` returns
`extraResults.attributeHistogram` keyed by attribute name — e.g. `battery-capacity`:
`min 120.00`, `max 11200.00`, `overallCount 1615`, 10 buckets carrying
`{threshold, occurrences, requested, relativeFrequency}`. Same bucket shape as the price histogram, so
one component renders both. Note many of these are near-duplicates/`-source-a` variants of each other —
pick 2–3 sensible ones per category rather than rendering all 27.

### Still unverified

**Only the browser leg (Phase 0b).** Everything above ran from a shell, not from `browser-wasm`.
What remains untested is exactly the platform question: `SocketsHttpHandler`, `Dns.GetHostName()`,
and the sync-facade deadlocks (§4). The transport, CORS, protocol, and dataset are settled.

Reproduction script: `scratchpad/grpcweb.py` (throwaway; not part of the deliverable). Note the SAFE
`Query` RPC rejects embedded literals — *"Literal value is forbidden in mode `SAFE`"* — so the spike
used `QueryUnsafe`. **The driver always sends the parametrised form, so this affects the spike only.**

---

## 1. What is verified from source

| Fact | Evidence |
| --- | --- |
| The evitaDB server speaks gRPC-Web **at the targeted release**, and allows any browser origin | `git show v2026.2.4:…/grpc/GrpcProviderRegistrar.java` → `.supportedSerializationFormats(GrpcSerializationFormats.values())`, `.enableUnframedRequests(true)`, `CorsService.builderForAnyOrigin().allowAllRequestHeaders(true).exposeHeaders(GRPC_STATUS, GRPC_MESSAGE, ARMERIA_GRPC_THROWABLEPROTO_BIN)` |
| gRPC-Web is exercised by the server's own test suite | `evita_test/…/EvitaServerTest.java#shouldBeAbleToGetGrpcWebResponse` (`gproto-web+https://…`) |
| gRPC service path for smoke tests | `EvitaDB.Client/Protos/GrpcEvitaManagementAPI.proto`: `package io.evitadb.externalApi.grpc.generated;` `service EvitaManagementService { rpc ServerStatus(google.protobuf.Empty) … }` → `/io.evitadb.externalApi.grpc.generated.EvitaManagementService/ServerStatus` |
| Demo catalog is `evita`; collections include `Product` (4223), `Category` (36), `Brand` (57), `Parameter` (113), `ParameterValue` (3319), `ParameterGroup` (5), `Tag` (18), `PriceList` (20), `Group` (10), `Stock` | evitaDB `documentation/user/en/get-started/query-our-dataset.md` |
| Working query shapes against the demo dataset (references `categories`, `parameterValues`, `groups`, `relatedProducts`; attributes `url`, `code`, `orderedQuantity`) | `EvitaDB.Test/Tests/EvitaClientDemoQueryTest.cs`, `EvitaDB.Test/DemoSetupFixture.cs` — but its locales (`en-US`/`cs-CZ`) and price lists (`vip`) do **not** match the live schema; §1a wins |
| **The facet semantics the task asks for are evitaDB's defaults** — no override constraints needed | evitaDB `documentation/user/en/query/requirements/reference.md` §"Default reference calculation rules": *"the default relation between options within a group is logical disjunction (OR)"*, *"between options in different groups / references is logical conjunction (AND)"* |
| Reference/facet summary is computed **excluding** the `userFilter` part of the query | same section, rules 1–2 |
| A read-only session `Close` is a **unary** RPC (no server-streaming needed in the happy path) | `EvitaClientSession.CloseAsync` → `session.CloseAsync(new GrpcCloseRequest{…}).ResponseAsync` |
| The facet panel must use the **legacy-named** `FacetSummary`/`FacetSummaryOfReference` requirement — the 2026 `referenceSummary` requirement is **not ported to C# yet** | `ResponseConverter.cs:111-160`: the converter reads the deprecated `extraResults.FacetGroupStatistics` (produced by the server for `facetSummary`) under `#pragma warning disable CS0612 // …kept until the referenceSummary requirement is ported`; `referenceGroupStatistics` is explicitly *not* consumed |
| The driver already has a full async surface (`QueryAsync`, `QueryCatalogAsync`, …) | `EvitaClientSession.cs:506`, `documentation/async-api.md` |

**Blocker found in the driver:** `EvitaClient`'s private constructor (`EvitaClient.cs:67`) hard-codes
`new SocketsHttpHandler()`, and `EvitaClient.Create` routes every TLS configuration through
`ClientCertificateManager` (file IO, `X509`, `ConfigureSslOptions`). Neither works under
`browser-wasm`. There is no seam to inject a handler today — **the driver must gain one** (§4).

**On reachability:** the sandbox proxy allows `CONNECT` only to port 443, which is why `:5555` returns
`403` — but the demo answers gRPC-Web on 443 anyway, so §1a could be executed in full. Only the
browser leg remains unverified.

---

## 2. Shape of the deliverable

```
EvitaDB.Storefront/                 # new: Blazor WebAssembly Standalone, net10.0
  Program.cs                        # DI: transport, EvitaClient singleton, ShopContext
  Services/
    EvitaConnectionFactory.cs       # builds the gRPC-Web-backed EvitaClient
    CatalogMetadataService.cs       # locales/currencies/price lists discovered at runtime
    ProductCatalogService.cs        # all storefront queries live here (one place)
    StorefrontState.cs              # locale/currency/price list/facets/price range + change events
  Models/                           # small view models mapped off ISealedEntity
  Pages/
    Index.razor                     # landing → category tree
    Category.razor                  # listing + facet panel + histograms + paging
    Product.razor                   # detail: prices, description, parameters, tags, related
  Layout/, wwwroot/
documentation/blazor-storefront-plan.md   # this file
```

Added to `EvitaDB.slnx` as a fourth project. `EvitaDB.Client` gains a small additive, backward-compatible
transport seam; **no** behavioural change for existing consumers.

---

## 3. Phase 0 — go/no-go spikes

Two *independent* variables. **The protocol/dataset variable is now settled (§1a); only the browser
platform variable remains.**

### 0a — wire check ✅ DONE (see §1a)

Transport, CORS preflight, exposed trailers, server version, session lifecycle, facet summary with
IMPACT, price histogram, and category hierarchy were all verified live against
`https://demo.evitadb.io` (port 443). Nothing here blocks.

The remaining *optional* piece of 0a is to re-run one query through the **driver itself** rather than
a raw client, which additionally proves the §4 seam and the driver's converters. Throwaway console
project referencing `EvitaDB.Client` + `Grpc.Net.Client.Web`:

```csharp
var handler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler());
var client  = await EvitaClient.Create(config /* Host=demo.evitadb.io, Port=443, HttpHandler=handler */);
Console.WriteLine((await client.Management().GetServerStatusAsync()).Version);   // expect 2026.2-SNAPSHOT
```

Then run the §7.2 query through the driver and assert what §1a already saw on the wire, this time
through the C# converters — which is the part a raw client cannot prove:

* `FacetSummary` present in `ExtraResults`, `GetFacetGroupStatistics()` non-empty,
* at least one `FacetStatistics.Impact` with `HasSense == true`,
* `PriceHistogram.Buckets.Length > 0`,
* and the §1a facet-relation table reproduces through `FacetHaving` (one constraint per reference).

Note the naming hazard: the 2026 docs renamed the constraint to `referenceSummary`, and the driver
deliberately still speaks the deprecated `facetGroupStatistics` wire message — which §1a confirms the
server still emits. Use `FacetSummaryOfReference`, never a `referenceSummary` equivalent.

### 0b — platform check (minimal Blazor WASM page)

Empty Blazor WASM app, same one call, served from `http://localhost:*`. **This is now the only
remaining gate.** §1a proved the server side of CORS from a shell; what a shell cannot prove is the
browser platform. Landmines to hit here deliberately:

* `EvitaClientConfiguration.Builder`'s constructor calls `Dns.GetHostName()` and catches only
  `SocketException`. If `browser-wasm` throws `PlatformNotSupportedException`, `new Builder()` throws
  before the caller can do anything — §4 widens the catch.
* `EvitaClient.Create` must not touch `ClientCertificateManager` on this path.
* Only `*Async` methods may be called (§4, note 5).

**Exit criteria:** 0b prints the server version in the browser console. Nothing in §5+ starts before that.

### If 0b fails

Given §1a, a CORS failure here is now unlikely — the probable failure mode is a *platform*
exception inside the driver (§4), which is fixable, not fatal. Only if the browser itself refuses the
transport is the "pure WASM, no backend" shape unachievable. Stated fallback
(**dev-only, not the plan's baseline**): run a thin local gRPC-Web ↔ gRPC proxy (Armeria/Envoy
`grpc_web` filter, or a 30-line ASP.NET Core app with `UseGrpcWeb` + `MapGrpcService` forwarding)
and point the WASM app at it. The storefront code is unchanged — only the base address moves. The
plan should not be reorganised around this unless 0b forces it.

---

## 4. Phase 1 — the browser transport seam in `EvitaDB.Client`

Small, additive, runtime-branched. **Deliberately not** a `net10.0-browser` multi-target: a plain
`net10.0` library is referenceable from Blazor WASM, and multi-targeting the shipped driver is far
more change than this demo justifies.

1. **`EvitaClientConfiguration`** — add an optional `HttpMessageHandler? HttpHandler` (or
   `Func<HttpMessageHandler>` factory) + `Builder.SetHttpHandler(...)`. Record is positional, so the
   new member goes last with a default to keep the existing constructor call sites compiling.
2. **`EvitaClient` ctor (`EvitaClient.cs:67`)** — when a handler is supplied, use it **verbatim**:
   skip `new SocketsHttpHandler()` entirely (constructing it at all is what fails on browser), skip
   the keep-alive/idle-timeout tuning (HTTP/2 pings are meaningless over fetch), skip
   `certificateManager?.ConfigureSslOptions(...)`.
3. **`EvitaClient.Create`** — when a handler is supplied, do **not** build a `ClientCertificateManager`
   even if `TlsEnabled` is true. The browser owns TLS; the demo server uses a publicly trusted cert.
4. **`ChannelPool`** — pool size is hard-coded `10` (`EvitaClient.cs:91`), and the constructor builds
   all of them eagerly, i.e. 10 `HttpClient`s. Make the size configurable and pass `1` for the browser.
   The dedicated `_cdcChannel` is built unconditionally — either leave it (one extra idle channel, no
   connection is opened until used) or make it lazy; decide during implementation, note it either way.
5. **Widen the `Dns.GetHostName()` catch** in `EvitaClientConfiguration.Builder`'s constructor to
   `Exception` (or gate on `OperatingSystem.IsBrowser()`), pending the 0b finding.
6. **Hard rule #1 still applies** — `TreatWarningsAsErrors`, no `WarningsNotAsErrors`, no new
   obsolete-warning suppressions.
7. **Document the deviation.** This seam has no Java counterpart (the Java driver has no browser
   target), so it is a deliberate C#-only addition — record it in `documentation/architecture.md`
   §Transport, the same way the `GrpcDataItemMap` rename is documented, rather than pretending to
   mirror Java.
8. **Regression guard:** the existing `EvitaDB.Test` suite must still pass unchanged (the seam is
   opt-in; default path keeps `SocketsHttpHandler`).

### Storefront-side consequences (call them out in code comments, they are easy to trip on)

* **Async only.** There are 24 `.GetAwaiter().GetResult()` sync facades in `EvitaClientSession` alone;
  on WASM's single thread they deadlock. The traps are the ones that *don't* look like I/O:
  `QueryCatalog(...)`, `Close()`, `session.GetEntitySchema(...)`. Use `QueryCatalogAsync`,
  `CloseAsync`, `*Async` throughout — and consider an analyzer/`grep` check in review.
* **`PublishTrimmed=false`, no AOT** for this demo. The driver pulls Newtonsoft (reflection-heavy
  complex-data-object conversion) and the OTLP exporter; trimming is a rabbit hole with no payoff here.
  Expect a large download — acceptable for a demo, worth one sentence in the README.
* **No CDC, no writes.** The demo catalog is read-only; client/bidi streaming is unsupported by
  gRPC-Web anyway.
* **Session lifetime:** one long-lived read-only session per client (or per page navigation) created
  via `QueryCatalogAsync`. Do not open/close a session per component render.

---

## 5. Phase 2 — project scaffold

* `dotnet new blazorwasm -o EvitaDB.Storefront` (Standalone, not Hosted), `net10.0`.
* `ProjectReference` → `EvitaDB.Client`; `PackageReference` → `Grpc.Net.Client.Web` **2.83.0**
  (must match `Grpc.Net.Client`).
* Add to `EvitaDB.slnx`. Keep it **out of** `dotnet build EvitaDB.slnx` CI gating initially, or add it
  as a build-only job — it must never make the driver's CI depend on network access to the demo.
* `Program.cs` DI:
  * singleton `EvitaClient` created via an async factory (`await EvitaClient.Create(...)`) — Blazor DI
    cannot `await`, so use a `Task<EvitaClient>`-returning provider or initialise in `Program.Main`
    before `RunAsync()`. Prefer the latter: a failed connection then surfaces as a clear startup error.
  * host/port/catalog from `wwwroot/appsettings.json` so a local server can be substituted. **The
    default is literally this** — note `443`, *not* the `5555` the evitaDB docs advertise (§1a):

    ```json
    { "Evita": { "Host": "demo.evitadb.io", "Port": 443, "Catalog": "evita", "TlsEnabled": true } }
    ```

    A local container from the downloadable dataset would use `localhost` / `5555` instead.

---

## 6. Phase 3 — storefront context (locale / currency / price list)

The task's "things a production site derives from domain, user preferences or profile" is modelled as
one `StorefrontState` scoped service holding: `CultureInfo Locale`, `Currency Currency`,
`string[] PriceLists`, `QueryPriceMode PriceType`, plus the current facet selection and price range.
It raises `OnChanged`; the listing page re-queries on that event.

**Discovered at runtime, not hard-coded** — §1a lists the live values, but reading them from the
server keeps the app correct if the dataset shifts, and exercises the schema API the storefront needs
anyway:

* `session.GetEntitySchemaAsync("Product")` → `IEntitySchema.Locales` (live: `cs`, `de`, `en`) and
  `IEntitySchema.Currencies` (live: `CZK`, `EUR`) populate the language and currency selectors.
* A query over the `PriceList` collection (`Collection("PriceList")` + `EntityFetch(AttributeContent("code"))`)
  populates the price-list selector — §1a shows `code` is a **global** (non-localized) attribute, so
  no locale is needed for this query. Default to `basic`, with `reference` as the optional
  struck-through comparison price.
* Present the price lists as named *profiles* rather than raw codes — retail (`basic` + `reference`),
  B2B (`b2b-basic-price` + `b2b-reference-price`), employee, shareholder. That is exactly the
  "derived from the user's profile" behaviour the task describes, and the demo dataset was clearly
  built for it.
* Cache all of this once in `CatalogMetadataService`.

Propagation into every query (the part that must be right, or nothing else matters):

* `EntityLocaleEquals(locale)` in `FilterBy` **and** `DataInLocales(locale)` in `Require` — the first
  filters to entities having that localization, the second selects which localized values come back.
* `PriceInCurrency(currency)` + `PriceInPriceLists(priceLists…)` + `PriceValidInNow()` in `FilterBy`
  — required for `PriceNatural` ordering, `PriceBetween`, and `PriceHistogram` to mean anything.
* `PriceType(QueryPriceMode.WithTax | WithoutTax)` in `Require` for the with/without-tax toggle.
* Fetch prices with `PriceContentRespectingFilter()` so the rendered price matches the filter.

---

## 7. Phase 4 — queries (all in `ProductCatalogService`, one file, async)

### 7.1 Category tree (nav)

```csharp
Query(
  Collection("Category"),
  FilterBy(EntityLocaleEquals(locale)),
  Require(
    Page(1, 1),
    HierarchyOfSelf(
      FromRoot("megaMenu", EntityFetch(AttributeContent("code", "name", "url")),
               StopAt(Level(3)), Statistics(StatisticsType.ChildrenCount))))
)
```

Read from `Hierarchy` extra result → `List<LevelInfo>` (`LevelInfo(Entity, Requested, QueriedEntityCount,
ChildrenCount, Children)`), rendered as a recursive component.

### 7.2 Product listing + facets + histograms — **the core query**

```csharp
Query(
  Collection("Product"),
  FilterBy(
    And(
      EntityLocaleEquals(locale),
      HierarchyWithin("categories", AttributeEquals("url", categoryUrl)),
      PriceInCurrency(currency), PriceInPriceLists(priceLists), PriceValidInNow(),
      UserFilter(                                   // ← everything the user clicked goes HERE
        // EXACTLY ONE FacetHaving per reference, carrying ALL ids ticked for that reference.
        // One-per-checkbox would AND them together — see the measured table in §1a.
        FacetHaving("parameterValues", EntityPrimaryKeyInSet(selectedParameterValueIds)),
        FacetHaving("brand",           EntityPrimaryKeyInSet(selectedBrandIds)),
        FacetHaving("tags",            EntityPrimaryKeyInSet(selectedTagIds)),
        PriceBetween(from, to)                      // ← price slider also inside userFilter
      ))),
  OrderBy(PriceNatural(direction)),
  Require(
    Page(page, 20),
    EntityFetch(AttributeContent("code", "name", "url"), PriceContentRespectingFilter()),
    // overload (string, FacetStatisticsDepth?, FilterBy?, OrderBy?, params IEntityRequire[]?) — IQueryConstraints.cs:1462
    FacetSummaryOfReference("parameterValues", FacetStatisticsDepth.Impact,
        (FilterBy?)null, OrderBy(AttributeNatural("order")),   // cast disambiguates the FilterGroupBy? overload
        EntityFetch(AttributeContentAll()), EntityGroupFetch(AttributeContentAll())),
    // overload (string, FacetStatisticsDepth?, params IEntityRequire[]) — IQueryConstraints.cs:1456
    FacetSummaryOfReference("brand", FacetStatisticsDepth.Impact, EntityFetch(AttributeContentAll())),
    FacetSummaryOfReference("tags",  FacetStatisticsDepth.Impact, EntityFetch(AttributeContentAll())),
    PriceHistogram(20),
    // live-verified names; pick 2-3 per category from the schema's numeric filterable attributes
    AttributeHistogram(20, "battery-capacity", "display-size", "weight"),
    DataInLocales(locale), PriceType(priceMode))
)
```

Non-negotiable constraints on this query, all evidenced in §1/§1a:

* **One `FacetHaving` per reference, never one per checkbox.** Measured in §1a: two same-group facets
  in one constraint → 1291 (OR); the same two split across two constraints → 1093 (AND). This is the
  single highest-risk detail in the implementation, and both variants compile and return plausible
  numbers.
* **Facet selections and the price slider live inside `UserFilter`.** Facet impact and the price
  histogram are computed relative to the *non-`userFilter`* part of the query; move them out and the
  numbers become self-referential (an option's impact would be measured against a result set that
  already applies that option).
* **No `FacetGroupsConjunction`/`Disjunction` is needed.** OR-within-group and AND-between-groups is
  already the engine default — confirmed both in the docs and on live data. Adding these constraints
  would *invert* the requested behaviour.
* Reference names are now live-confirmed (§1a), but the implementation should still drive the facet
  panel from **`IReferenceSchema.IsFaceted == true`** on the fetched `Product` schema rather than a
  hard-coded list. Note the live schema exposes both the deprecated flat `faceted` flag and the scoped
  `facetedInScopes: [SCOPE_LIVE]`; `IsFaceted` is the driver's fallback-reading accessor for exactly
  this pair (hard rule #2).
* Checkbox state comes from `FacetStatistics.Requested`, which §1a confirms the server sets on the
  selected facets — no parallel client-side selection model is needed for rendering (one is still
  needed to *build* the query).

Rendering:

* `FacetSummary` → `GetFacetGroupStatistics()` → `FacetGroupStatistics { GroupEntity, Count,
  GetFacetStatistics() }` → `FacetStatistics { FacetEntity, Requested, Count, Impact }`.
  Checkbox `checked` = `Requested`; label = group/facet entity's localized `name` attribute;
  badge = `Count`, and `Impact.Difference` (`+n` / `−n`, greyed when `!Impact.HasSense`).
* `PriceHistogram` / `AttributeHistogram` → `IHistogram { Min, Max, OverallCount, Buckets[] }`,
  `Bucket { Threshold, Occurrences, Requested }` → CSS-only bar chart (no chart library) with a
  two-handle range input bound to the `PriceBetween` values.
* Paging from `response.RecordPage` (`DataChunk`: page number, size, total record count).

### 7.3 Product detail

```csharp
Query(
  Collection("Product"),
  FilterBy(And(EntityLocaleEquals(locale), AttributeEquals("url", productUrl),
               PriceInCurrency(currency), PriceInPriceLists(priceLists), PriceValidInNow())),
  Require(
    EntityFetch(
      AttributeContentAll(), AssociatedDataContentAll(), PriceContentAll(),
      ReferenceContentWithAttributes("parameterValues",
          EntityFetch(AttributeContentAll()), EntityGroupFetch(AttributeContentAll())),
      ReferenceContentWithAttributes("tags",  EntityFetch(AttributeContentAll())),
      ReferenceContentWithAttributes("brand", EntityFetch(AttributeContentAll())),
      ReferenceContentWithAttributes("relatedProducts", EntityFetch(AttributeContentAll())),
      HierarchyContent(EntityFetch(AttributeContentAll()))),           // breadcrumb
    DataInLocales(locale), PriceType(priceMode))
)
```

Detail page renders: name + gallery/description from associated data, price (with/without tax, plus
the "reference"/strikethrough price if a second price list is selected), a **parameters table grouped
by `ParameterGroup`** (group = the reference's group entity, rows = referenced `ParameterValue`
entities), tags as chips, breadcrumb from `HierarchyContent`, related products strip.

Associated data on this dataset is complex/JSON — read defensively (`GetAssociatedData(...)` may be
absent per locale) and fall back to "no description" rather than throwing.

---

## 8. Phase 5 — styling

Hand-written CSS in `wwwroot/css/app.css` — no Tailwind/Bootstrap build step, no CDN (keeps the demo
self-contained and the diff readable). CSS grid product cards, a left facet sidebar, a sticky top bar
holding the locale/currency/price-list/tax selectors, CSS-only histogram bars, and a
`prefers-color-scheme` dark variant. Loading and error states are first-class: the demo server is
described upstream as running on modest shared hardware, so every query surface needs a skeleton
state and a visible error box rather than a blank page.

---

## 9. Phase 6 — verification & documentation

* Manual checklist: category navigation; facet check/uncheck changes counts and results; **two options
  in one group widen the result set (OR) — the §1a regression, and the one that catches the
  one-FacetHaving-per-checkbox bug**; options in two groups narrow it (AND); the three known
  dead-end facets (`105701`, `105726`, `106714` — §1a) correctly return zero results and are *not*
  a bug; price slider narrows
  and the histogram stays stable while only the selected band highlights; switching locale re-labels
  everything; switching currency/price list changes prices and re-computes the price histogram;
  product detail shows grouped parameters, tags, breadcrumb.
* Optional automated smoke — **NOT IMPLEMENTED**: one xUnit test in `EvitaDB.Test` that runs the §7.2
  query against the demo host through the **gRPC-Web** handler, asserting the facet summary and price
  histogram are present. Left out deliberately: it could not be run in the sandbox, and it would add a
  network dependency to the driver's test project. If added, make it skippable/tagged so it never blocks
  CI in a network-restricted environment (mirror how `DemoSetupFixture` honours
  `EVITA_DEMO_HOST`/`EVITA_DEMO_PORT`).
* Docs: `EvitaDB.Storefront/README.md` (how to run, how to point at a local server); a
  §Transport paragraph in `documentation/architecture.md` for the browser seam; a line in the root
  `README.md`.
* **No commits or pushes without explicit approval.**

---

## 10. Risks, ranked

| # | Risk | Mitigation |
| --- | --- | --- |
| ~~0~~ | ~~The client refuses to connect to an older demo server.~~ **RESOLVED (§1a):** the demo reports `2026.2-SNAPSHOT`; `ParseVersion` returns `null` for `SNAPSHOT`, so `VerifyServerCompatibilityAsync` returns early and never rejects. | — (re-check if the demo is ever pinned to a numbered release) |
| ~~1~~ | ~~Deployed demo build ≠ v2026.2.4 tag, or CORS fails.~~ **RESOLVED (§1a)** for the server side: gRPC-Web, preflight and exposed trailers all verified live on port 443. | Browser-side CORS still nominally untested — Phase 0b |
| 1b | **The demo moves off port 443 / the front proxy changes.** The plan depends on a reverse proxy that the evitaDB docs don't mention (they advertise `:5555`, the server self-reports `:6555`). | Host/port live in `wwwroot/appsettings.json`; a browser cannot reach `:5555` from most corporate networks anyway, so 443 is also the more robust choice |
| 2 | Driver seam turns out to need more than the 5 edits in §4 (something else in the call path touches an unsupported API) | 0b surfaces it as a concrete `PlatformNotSupportedException` with a stack trace; each such site is a small additive fix |
| 3 | A sync facade slips into a component and deadlocks the UI thread | async-only rule + review grep for `.GetAwaiter().GetResult()`/`.Result` in `EvitaDB.Storefront` |
| **4** | **Facet filter built as one `FacetHaving` per ticked checkbox → everything ANDs.** Now the top functional risk: it compiles, returns plausible-looking numbers, and §1a shows the two variants differ by 200 products on a single group. | One `FacetHaving` per *reference* with all its ids; add the §1a table as a test case in Phase 6's checklist (two options in one group must *widen* the result set) |
| 5 | Demo dataset reference/attribute names drift | Live-confirmed in §1a, but still drive the facet panel and histograms off the fetched **entity schema** (`IsFaceted` references, filterable numeric attributes) instead of hard-coded names |
| 6 | Large WASM payload / slow first load | `PublishTrimmed=false` accepted deliberately; documented, with Brotli compression left to default |

---

## 11. Suggested order of work

1. ~~Phase 0a~~ **done (§1a)** — transport, CORS, version, facet summary, impact, histogram, hierarchy
   and dataset metadata all verified live. Remaining gate: **0b WASM spike**, which now tests only the
   browser platform. Optionally re-run the §7.2 query through the driver to exercise its converters.
2. Phase 1 driver seam + `documentation/architecture.md` note + existing test suite green.
3. Phase 2 scaffold + `EvitaDB.slnx` wiring + connection bootstrapping with a visible error state.
4. Phase 3 shop context + runtime metadata discovery (locales, currencies, price lists).
5. Phase 4.1 category tree → 4.2 listing with facets/histograms/paging → 4.3 product detail.
6. Phase 5 styling pass.
7. Phase 6 verification + docs.

Phases 2–7 are one developer's linear path; only Phase 1 touches shipped driver code, and it is the
only part that warrants review against the driver's hard rules.
