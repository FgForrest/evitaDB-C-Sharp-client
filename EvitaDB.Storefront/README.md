# evitaShop — Blazor WebAssembly storefront demo

A **pure Blazor WebAssembly** app (no backend of its own) that talks straight from the browser to
[demo.evitadb.io](https://demo.evitadb.io) over **gRPC-Web**, using the **local `EvitaDB.Client` driver**
via a project reference.

It renders a small e-commerce storefront over the public `evita` demo catalog: a category tree, a product
listing with a facet panel and histograms, and a product detail page.

```shell
dotnet run --project EvitaDB.Storefront
```

Then open the printed `http://localhost:<port>` address.

## Running it in Docker

A prebuilt image is published to GitHub Container Registry on every release, for `linux/amd64` and
`linux/arm64`:

```shell
docker run --rm -p 8080:8080 ghcr.io/fgforrest/evitadb-c-sharp-client/storefront:latest
```

Tags are `latest`, the release version (`1.2.3`), the minor line (`1.2`) and the commit (`sha-abc1234`).

The port nginx binds inside the container is `STOREFRONT_LISTEN_PORT` (default `8080`). Change it when the
image runs behind a reverse proxy whose target port is fixed and not yours to configure:

```shell
docker run --rm -e STOREFRONT_LISTEN_PORT=9000 -p 9000:9000 \
  ghcr.io/fgforrest/evitadb-c-sharp-client/storefront:latest
```

It must be 1024 or above — nginx runs as an unprivileged user here and cannot bind a privileged port. The
container refuses to start with a clear message rather than failing obscurely if it is out of range. The
health check follows the same variable, so a reconfigured port stays healthy.

The image runs under an arbitrary uid, not only its default `101`, which matters where the platform assigns
one. The two files the entrypoint rewrites at start-up — the nginx server block and `appsettings.json` —
are pre-created world-writable for that reason, so no `--user` flag is needed. Mounting a volume over
either path takes that away; the entrypoint says so explicitly if it happens.

Point it elsewhere with the same environment variables the compose file uses:

```shell
docker run --rm -p 8080:8080 \
  -e EVITA_HOST=my-server.example.com -e EVITA_PORT=5555 \
  ghcr.io/fgforrest/evitadb-c-sharp-client/storefront:latest
```

Or build it yourself:

```shell
docker compose up --build          # against the public demo dataset - no configuration needed
```

Then open <http://localhost:8080>.

Because Blazor WebAssembly publishes to plain static files, the image is a two-stage build that ends in
nginx: **nothing of this app runs on the server**, it is all executed by the browser. The runtime stage
uses `nginxinc/nginx-unprivileged`, so the container listens on 8080 as a non-root user.

Build it directly if you prefer — but note the context must be the **repository root**, because the
project has a `ProjectReference` to `EvitaDB.Client`:

```shell
docker build -f EvitaDB.Storefront/Dockerfile -t evita-storefront .
docker run --rm -p 8080:8080 evita-storefront
```

### Pointing it at another server

`wwwroot/appsettings.json` is a static file the browser fetches, so it would normally be fixed at build
time — one image per target server. `docker/entrypoint.sh` rewrites it at container start instead, from
four environment variables (it validates them, so a typo fails loudly rather than producing malformed
JSON the app cannot parse):

| variable | default | |
| --- | --- | --- |
| `EVITA_HOST` | `demo.evitadb.io` | |
| `EVITA_PORT` | `443` | 443, not 5555 — see the note above about the demo's front proxy |
| `EVITA_TLS` | `true` | |
| `EVITA_CATALOG` | `evita` | |
| `STOREFRONT_PORT` | `8080` | host port only, compose-level |

```shell
EVITA_HOST=my-server.example.com EVITA_PORT=5555 docker compose up
```

### Running evitaDB locally too

```shell
wget https://evitadb.io/download/evita-demo-dataset.zip && unzip -d ./data evita-demo-dataset.zip
EVITA_HOST=localhost EVITA_PORT=5555 docker compose --profile local up
```

Two things that trip people up here:

* **The storefront container is not a proxy.** gRPC-Web traffic goes from the browser straight to
  evitaDB, so the server has to be published on the host and reachable from the browser — pointing
  `EVITA_HOST` at a compose service name would not work.
* **evitaDB generates a self-signed certificate on first start**, and the browser will refuse gRPC-Web
  calls to it until that certificate is trusted. Open `https://localhost:5555` once and accept it, or
  configure evitaDB with a certificate the machine already trusts. The public demo needs none of this.

### What nginx has to get right

Two settings in `docker/default.conf` are less obvious than they look, and both are commented there:

* there is deliberately **no `types { }` block** — one at server level *replaces* the inherited MIME map
  wholesale, which would serve CSS and JS as `octet-stream`. nginx has shipped `application/wasm` since
  1.21, and `.dat`/`.blat`/`.dll` are correctly covered by the default `octet-stream`;
* cache policy comes from a **`map`** rather than per-location `add_header`, because an `add_header`
  inside a `location` silently discards every header inherited from the server level — mixing the two
  would drop the security headers on exactly the paths that set their own caching.

`index.html`, `blazor.boot.json` and `appsettings.json` are never cached (a stale one of those serves an
app whose assets no longer exist); hashed `_framework/` assets are cached immutably. Unknown paths fall
through to `index.html` so the Blazor router owns `/category/…` and `/product/…`.

## What it demonstrates

* **gRPC-Web from the browser.** `EvitaClientConfiguration.SetHttpHandler(new GrpcWebHandler(...))` — the
  driver's transport seam. See `documentation/architecture.md` § *Browser hosts*.
* **Facet filtering with correct boolean semantics.** OR within a facet group, AND between groups — which
  is evitaDB's default, so the query adds no `facetGroupsConjunction`/`facetGroupsDisjunction` at all.
* **Facet impact analysis** (`FacetStatisticsDepth.Impact`). Each option row shows *two different numbers*,
  styled so they cannot be confused:

  | shown as | source | meaning |
  | --- | --- | --- |
  | plain dimmed number | `FacetStatistics.Count` | products carrying the option, **ignoring** the user filter — so it does not move as options are ticked |
  | pill with ↑/↓ arrow | `Impact.MatchCount` (+ `Impact.Difference` in the tooltip) | what the result count **would become** if this option were ticked |

  They coincide while nothing is ticked, and diverge as soon as a filter is active — e.g. with one option of
  a group applied, a sibling shows *has option 98* but *↑206*, because OR-within-group widens the result.
  The pill is hidden for an already-applied option: evitaDB reports impact for *adding* an option, so for an
  applied one the figure has no unambiguous reading.
* **Price and attribute histograms with draggable range limiters.** Both are filters: the price histogram
  applies `priceBetween`, the attribute histograms apply `attributeBetween` (degrading to
  `attributeGreaterThanEquals` / `attributeLessThanEquals` when only one handle is moved). Dragging updates
  the highlight live but only re-queries on release, so a drag is one round trip rather than one per pixel.
* **Transparent session recovery.** evitaDB drops idle sessions; `EvitaCatalogContext.ExecuteAsync` catches
  `InstanceTerminatedException`, opens a fresh session and retries once. Reopening costs a single RPC
  because the schema cache lives on `EvitaClient` keyed by catalog, not on the session.
* **Product-type grouping.** The listing shows only `BASIC` and `MASTER` products (`attributeInSet` on
  `productType`); `VARIANT`s are represented by their master. A master's price is labelled *from* because
  evitaDB resolves it as the lowest across inner records (`LOWEST_PRICE` handling) — the client does not
  compute it. The master's detail page lists its variants. In `portables` this turns 1332 rows into 284.
* **Seven sort options** built from the schema's sortable attributes — recommended (the `order`
  Predecessor chain), price ↑/↓, best selling, best rated, newest, name A–Z. Options whose attribute the
  catalog does not declare sortable are dropped rather than offered and failing.
* **Multi-select price lists.** Several lists can be held at once, in catalog order, which is the
  `priceInPriceLists` resolution priority.
* **Shareable URLs.** Facets, price and attribute ranges, sort, locale, currency, price lists and tax mode
  all round-trip through the query string, e.g.
  `?currency=EUR&sort=price-asc&f.parameterValues=103894&r.weight=100-900`. Restoring a link reproduces the
  identical result count.
* **Rich HTML descriptions, sanitized.** `description` holds an entire vendor marketing page — its own
  class names, `data-analytics-*` hooks and buttons wired to modals this app does not have.
  `Services/HtmlContent.cs` rewrites it against an allowlist before it reaches `MarkupString`: unknown
  elements are unwrapped, `script`/`style`/`button` and friends are dropped with their content, and every
  attribute except `href`/`title`/`alt`/`src` is stripped (with `javascript:` URLs rejected and outbound
  links given `rel="noopener noreferrer nofollow"`). A real page shrinks 4042 → 2337 characters and keeps
  only `a br div h2 p span strong sup`. The block renders collapsed with a masked fade and a
  *Read full description* toggle.
* **Locale / currency / price-list / tax switching**, propagated into every query — the things a production
  site derives from the domain, the request locale or the customer's profile.

## Configuration

`wwwroot/appsettings.json`:

```json
{ "Evita": { "Host": "demo.evitadb.io", "Port": 443, "TlsEnabled": true, "Catalog": "evita" } }
```

**Port 443, not 5555.** The public demo sits behind a front proxy that serves gRPC-Web on 443; the `:5555`
the evitaDB documentation advertises is not reachable from a browser on most networks. To run against a
local server with [the downloadable demo dataset](https://evitadb.io/download/evita-demo-dataset.zip), set
`Host` to `localhost` and `Port` to `5555`.

## How it is put together

| File | Role |
| --- | --- |
| `Services/EvitaCatalogContext.cs` | connects, opens the read-only session, **primes the schema caches** |
| `Services/StorefrontSchema.cs` | every dataset-specific name, in one place |
| `Services/StorefrontState.cs` | locale / currency / price profile / tax mode / facet selection / ranges |
| `Services/ErrorDetail.cs` | unwraps `IEvitaError.PrivateMessage` + inner exceptions for legible errors |
| `Services/ProductCatalogService.cs` | all queries |
| `Services/EntityDisplay.cs` | guarded readers for localized attributes, prices and referenced names |
| `Services/ProductSort.cs` | sort options mapped to evitaDB order constraints |
| `Services/HtmlContent.cs` | allowlist sanitizer + plain-text teasers for the stored HTML |
| `Shared/FacetGroupList.razor` | one facet group; shared by the sidebar and the all-filters modal |
| `Pages/Listing.razor` | category tree + product grid + facet panel |
| `Pages/ProductDetail.razor` | prices, description, grouped parameters, tags, related products |

## Three things that will bite you if you change the query code

**1. One `facetHaving` per reference — never one per checkbox.**

Facet ids for a reference must go into a *single* `facetHaving` carrying all of them. evitaDB then applies
OR within a group and AND between groups. Emitting one constraint per ticked box ANDs everything instead.
Both versions compile and return plausible numbers; measured against the demo dataset, two options of one
group give **1291** products in a single constraint but only **1093** when split across two.

**2. Always send both range bounds.**

evitaQL cannot express a one-sided `priceBetween` — `priceBetween(100,)`, `priceBetween(null,500)` and
`priceBetween(100)` are all rejected by the grammar. An untouched slider handle is therefore sent as the
histogram's own extreme, which selects the same products. (`attributeBetween` is the same; the query
builder degrades to `attributeGreaterThanEquals`/`attributeLessThanEquals` when a bound is genuinely
absent.)

**3. Facet selections and the price range belong inside `userFilter`.**

The facet summary and the price histogram are computed while *ignoring* the `userFilter` container. Move
those constraints out of it and every impact figure becomes self-referential.

## Known quirks of the demo dataset

* A handful of `ParameterValue`s (12 of 273 in the `portables` category) are assigned to **two** `Parameter`
  groups. Since groups combine with AND, selecting one of those returns **zero** products even though its
  badge shows a non-zero count. That is the engine's correct answer, not a bug — and the reason the empty
  listing view mentions it.
* Product is **not** a hierarchical collection here (Category is), so the detail page's breadcrumb lists the
  referenced categories rather than a true ancestor chain, and the product query must not ask for
  `hierarchyContent`.
* `rating`, `ratingVotes` and `orderedQuantity` are **0 for every product**, so the star badge is hidden and
  the "best rated" / "best selling" sorts tie on every row (evitaDB then falls back to primary-key order).
  The constraints are correct; the data simply is not there.
* `description` / `descriptionShort` are localized **attributes**, not associated data, and they are
  populated **only in the `cs` locale** (roughly half the products) — `en` and `de` carry just `name`,
  `unit` and `url`. Switch the language to `cs` to see the description and card teasers; in the other
  locales those blocks simply stay hidden. The `localization` associated data is not a substitute — it
  holds SEO labels whose values are all just the product name repeated.

## Constraints

* **Async only.** Every driver call must be the `…Async` one. The sync facades block, and blocking deadlocks
  on WebAssembly's single thread — including the non-obvious ones (`QueryCatalog`, `Close`,
  `GetEntitySchema`).
* **Not trimmed** (`PublishTrimmed=false`). The driver pulls Newtonsoft.Json and the OTLP exporter; the cost
  is a large download, which is acceptable for a demo.
* Read-only. The demo catalog does not accept writes.
