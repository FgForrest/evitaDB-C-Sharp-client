# Typed entity models

The driver's default read model is `ISealedEntity` with stringly-typed accessors
(`entity.GetAttribute("name")`). The typed-model layer (`EvitaDB.Client.Models.Typed`) lets you declare
an annotated **contract** instead and read entities through it, and lets you derive an entity schema
from that same contract.

This is the C#-native counterpart of the Java driver's annotation + proxy feature
(`evita_api/…/data/annotation`, `ClassSchemaAnalyzer`, `ProxyFactory`). The Java version is the
behavioural reference; the deviations are listed at the end and are deliberate.

## Quick start

```csharp
using EvitaDB.Client.Models.Typed;

[EvitaEntity("Product", AllowedLocales = ["cs", "en"])]
public interface IProduct
{
    int Id { get; }                                              // primary key, inferred by name

    [EvitaAttribute(Filterable = true)] string? Name { get; }
    [EvitaAttribute("ean")] string? Ean { get; }
    [EvitaAttribute(Sortable = true)] Maybe<decimal> Quantity { get; }
    [EvitaAttribute] IReadOnlyList<string>? Tags { get; }
    [EvitaAttribute(Localized = true)] string? GetTitle(CultureInfo locale);
}
```

Derive the schema from the contract, then read entities as `IProduct`:

```csharp
// once, when setting the catalog up (read-write session)
session.DefineEntitySchemaFromModelType<IProduct>();

// reading
IProduct? product = await session.QueryOneModelAsync<IProduct>(query);
IList<IProduct> products = await session.QueryModelListAsync<IProduct>(query);
IProduct? byKey = await session.GetModelAsync<IProduct>(42, [AttributeContentAll()]);

// or convert an entity you already hold
IProduct typed = someSealedEntity.AsModel<IProduct>();
```

Nothing needs a session to bind: `IEntity.Schema` travels with the entity, so `AsModel<T>()` works
anywhere an entity is available — which is also why the layer is unit-testable without a server (see
`EvitaDB.Test/Tests/TypedModelTest.cs`).

## Annotation vocabulary

| Attribute | Applies to | Meaning |
|---|---|---|
| `[EvitaEntity(name?)]` | contract type | maps the contract to an entity type **and** allows schema derivation from it. `Description`, `DeprecationNotice`, `GeneratedPrimaryKey` (fallback only — a `[PrimaryKey]` member wins), `AllowedLocales` |
| `[EvitaEntityRef(name?)]` | contract type | maps the contract to an entity type, read-only — schema derivation is refused |
| `[PrimaryKey]` | member | the entity primary key. `AutoGenerate` (default `true`) declares a server-generated key; `false` declares a caller-supplied key and turns generation off on a schema that had it on. Schema derivation emits a mutation only when the schema disagrees. An `Id` member that is merely inferred carries no schema intent |
| `[EntityType]` | member | the entity type name of the fetched entity |
| `[EvitaAttribute(name?)]` | member | an entity attribute **and** its schema: `Filterable`, `Sortable`, `Localized`, `Nullable`, `Representative`, `Unique`, `IndexedDecimalPlaces`, `Description`, `DeprecationNotice` |
| `[AttributeRef(name?)]` | member | an existing attribute, read-only — contributes nothing to the schema |
| `[EvitaReference(name?)]` | member | an entity reference **and** its schema: `Entity`, `Managed`, `GroupEntity`, `GroupEntityManaged`, `Indexed`, `Faceted`, `AllowEmpty`, `Description`, `DeprecationNotice` |
| `[ReferenceRef(name?)]` | member | an existing reference, read-only |
| `[ReflectedReference(name?)]` | member | a reference evitaDB maintains as the inverse of one on another entity (`OfEntity`, `OfName`, `Faceted`, `AttributeInheritance`) — read-only |
| `[AccompanyingPrice(name?)]` | member | a price computed alongside the selling price; requested per query, not declared in the schema |
| `[EvitaAssociatedData(name?)]` | member | associated data **and** its schema: `Localized`, `Nullable`, `Description`, `DeprecationNotice` |
| `[AssociatedDataRef(name?)]` | member | existing associated data, read-only |
| `[EvitaPrice]` | member | the entity's stored prices, optionally narrowed by `PriceList`; declares the entity priced (`IndexedPricePlaces`, `AllowedCurrencies`) |
| `[PriceForSale]` | member | the price evitaDB computed as the selling price (`IndexedPricePlaces`, `AllowedCurrencies`) |
| `[ParentEntity]` | member | the parent of a hierarchical entity; also makes the derived schema hierarchical |
| `[ReferencedEntity]` | member of a reference contract | the referenced entity |
| `[ReferencedEntityGroup]` | member of a reference contract | the reference group |
| `[RemoveWhenExists]` | method of an editor contract | the member removes the data it is bound to instead of writing it |
| `[NotBound]` | member | excluded from binding; the member must carry its own implementation |

`ITypedEntity` is an optional mixin a contract can extend to reach `Entity` and `ContractType`;
`TypedEntityFactory.GetEntity(instance)` works whether or not the contract extends it.

## How a member is resolved

Name resolution is the Java driver's four-branch cascade:

1. an annotation naming the data explicitly wins;
2. an annotation with a blank name derives the name from the member (`Name` → `name`); on methods an
   accessor prefix is stripped first (`GetTitle` → `title`). **Annotated members resolve strictly** — a
   missing schema element throws at bind time, naming the contract, member and entity;
3. an unannotated member is inferred: matched against the schema by exact name, then by evitaDB's
   camel-case name variant. **Inference is lenient** — an unmatched member simply declines, and only
   fails if it is actually read;
4. anything else declines.

An unannotated integer member named `Id`, `PrimaryKey`, `Pk` or `EntityPrimaryKey` is taken as the
primary key.

The declared *shape* is a separate axis from the name. `Maybe<T>` is unwrapped first, then the
collection shape (`T[]`, `IReadOnlyList<T>`, `IList<T>`, `ISet<T>`, …), then `Nullable<T>`; the
innermost type is the conversion target. So `string`, `string[]`, `IReadOnlyList<string>`,
`Maybe<string>` and `Maybe<IReadOnlyList<string>>` are all legal shapes over the same attribute.

A member taking a single `CultureInfo` reads the locale-qualified value; a localized attribute read
without one resolves against the locale the entity was fetched with.

## References, nested views and parents

A reference member's **item type** decides what you get — the same rule the Java driver uses:

| Item type | Result |
|---|---|
| `int` / `long` … | the referenced entity primary key |
| `IEntityReference` | a lightweight `{type, primaryKey}` |
| a type annotated `[EvitaEntity]` / `[EvitaEntityRef]` | a nested typed view of the **referenced entity** |
| any other interface | a nested typed view of the **reference itself** (a *reference contract*) |

The collection shape decides arity: a scalar member takes the first reference, a collection member takes
all of them.

```csharp
[EvitaEntity("Product")]
public interface IProduct
{
    // nested view of the referenced entity
    [EvitaReference("brand", Entity = "Brand", Indexed = true)] IBrand? Brand { get; }

    // nested views of the references themselves, exposing their own attributes
    [EvitaReference("categories", Faceted = true)] IReadOnlyList<IProductCategoryLink> Categories { get; }

    // an integer projection reveals nothing about the target, so Entity must be stated
    [EvitaReference("categories", Entity = "Category")] IReadOnlyList<int> CategoryIds { get; }
}

// a reference contract needs no type-level annotation - it is recognised by being a reference
// member's item type
public interface IProductCategoryLink
{
    [ReferencedEntity] ICategory Category { get; }        // navigate to the referenced entity
    [ReferencedEntity] int ReferencedEntityPrimaryKey { get; }
    [EvitaAttribute(Filterable = true)] int? Position { get; }  // attribute *of the reference*
    [ReferencedEntityGroup] int? GroupPrimaryKey { get; }
}
```

Inside a reference contract, an unannotated member is an attribute of the reference; integer members
named `ReferencedEntityPrimaryKey` / `PrimaryKey` / `Id` (or `GroupPrimaryKey` / `GroupId`) are accepted
as the respective keys without annotation.

**Parents.** `[ParentEntity] int? ParentId` reads the parent primary key, which evitaDB returns even
without a hierarchy fetch. `[ParentEntity] ICategory? Parent` returns a nested view and therefore needs
the parent *body* — `hierarchyContent(entityFetch(...))`.

**Bodies must actually be fetched.** A nested view over a referenced entity, a reference group or a
parent needs that body in the response. When only the key came back, the read raises
`ContextMissingException` naming the member and the requirement rather than returning `null` — the
difference between "no such relation" and "you did not ask for it" stays visible.

**Nested views have stable identity**: reading the same member twice returns the same object, memoized
per instance. That is the read-side half of the Java driver's per-proxy instance cache, and it is what
makes walking an object graph predictable.

**Schema derivation** declares references with cardinality from the declared shape (`ZeroOrOne` /
`ExactlyOne` / `ZeroOrMore` / `OneOrMore`, chosen by `AllowEmpty`), propagates `Indexed`/`Faceted` and
the group type, and picks up the **attributes declared by the reference contract**. The referenced entity
type comes from `Entity` when given, otherwise from the annotated entity contract the member (or its
reference contract's `[ReferencedEntity]` member) exposes; when neither says, derivation fails loudly.

## Prices

```csharp
[EvitaEntity("Product")]
public interface IProduct
{
    [PriceForSale] IPrice? SellingPrice { get; }              // the winning price
    [PriceForSale] IReadOnlyList<IPrice> AllSellingPrices { get; }  // every price eligible for sale
    [EvitaPrice] IReadOnlyList<IPrice> Prices { get; }         // all stored prices
    [EvitaPrice(PriceList = ["vip"])] IPrice? VipPrice { get; } // narrowed to a price list
}
```

A price member's item type must be `IPrice` — there is no scalar projection, mirroring the Java driver's
requirement that the type be assignable to `PriceContract`; read the amount off the returned price.
A scalar member takes the first match, a collection member takes all. Dropped prices are skipped.

`[PriceForSale]` depends on the **query** having established a price context (`priceInCurrency` plus
`priceInPriceLists`). Without one the scalar member is `null` rather than an error, because that is what
the driver reports.

Schema derivation declares the entity priced with `IndexedPricePlaces`, and `AllowedCurrencies` are
additive across members — several price members can each contribute the currencies they care about.

## Associated data

Associated data is stored but never indexed, and it is the one place a plain POCO works as a member type:

```csharp
[EvitaEntity("Product")]
public interface IProduct
{
    [EvitaAssociatedData] Localization? Localization { get; }   // deserialized from a complex object
    [EvitaAssociatedData] Maybe<string> Warranty { get; }       // one of evitaDB's own data types
    [EvitaAssociatedData(Localized = true)] string? GetLeaflet(CultureInfo locale);
}
```

Members typed as one of evitaDB's supported data types are read directly; anything else is deserialized
through the driver's `GetAssociatedData<T>` (the `ComplexDataObjectConverter` path). A value-typed member
that is not a supported data type is rejected at bind time, since there is nothing to deserialize into.

Two behaviours worth knowing:

- **The schema records `ComplexDataObject`, not your POCO type.** evitaDB stores anything outside its own
  data types as a complex object, so that is what schema derivation declares; the POCO type exists only in
  the contract.
- **Declared-but-unset associated data reads as absent.** An entity assembled on the client knows only the
  associated data actually set on it and raises `AttributeNotFoundException` for other names, whereas a
  server-fetched entity returns `null`. Since the member's name is validated against the schema at bind
  time, that exception can only mean "no value here", and is reported as absent.

## Writing: editor contracts

Add setters to a contract and extend `ITypedEntityEditor` to get an editor:

```csharp
[EvitaEntity("Product")]
public interface IProductEditor : ITypedEntityEditor
{
    int Id { get; }

    [EvitaAttribute(Filterable = true)] string? Name { get; set; }        // property setter
    [EvitaAttribute] IProductEditor SetWeight(decimal value);              // fluent setter
    [EvitaAttribute(Localized = true)] void SetTitle(CultureInfo locale, string value);
    [EvitaAssociatedData] Localization? Localization { get; set; }
    [ParentEntity] int? ParentId { get; set; }

    [EvitaAttribute("name")] [RemoveWhenExists] void RemoveName();
}
```

```csharp
var draft = session.CreateNewModel<IProductEditor>();      // new entity
draft.Name = "Speaker";
draft.SetWeight(1.25m).SetTitle(czech, "Reproduktor");     // fluent chaining
session.UpsertModel(draft);

var editor = fetchedEntity.OpenForWrite<IProductEditor>(); // change an existing entity
editor.Name = "Renamed";
editor.UpsertVia(session);
```

How it behaves:

- **An untouched editor sends nothing.** `ToMutation()` is `null` and `HasChanges` is `false` when no
  mutating member ran. The typed layer normalizes what the builder returns rather than trusting it: a
  change-set builder that contributes a `null` local mutation would otherwise make an unmodified entity
  look changed and put a null element on the wire. A deep upsert additionally always sends a *new* entity,
  since creating it is the change whether or not it carries any data.
- **The fetched entity is never mutated.** A builder is materialized on the first write, and from then on
  reads go through it — so a value you just wrote reads back immediately, and the original sealed entity
  is untouched.
- **Nothing is sent until you ask.** `ToMutation()` returns `null` when nothing changed, `HasChanges` says
  whether a mutating member ran, and `ToInstance()` shows the entity as it would look with the changes
  applied.
- **A setter returning the contract is fluent** and returns the editor for chaining; a `void` setter
  returns nothing.
- **Writers are recognised** as property setters, methods named `SetXxx`, or any annotated method taking a
  value; the locale may sit on either side of the value. Removers are `[RemoveWhenExists]` methods (or
  methods named `RemoveXxx` taking at most a locale).

### Writing prices and references

Both are declarative too, but their values arrive whole rather than field by field:

```csharp
[EvitaEntity("Product")]
public interface IProductEditor : ITypedEntityEditor
{
    // a price is written as an IPrice: its identity is (price id, price list, currency) and every field
    // must travel together
    [EvitaPrice] void SetPrice(IPrice price);
    [EvitaPrice] void SetPrices(IReadOnlyList<IPrice> prices);
    [EvitaPrice(PriceList = ["vip"])] [RemoveWhenExists] void RemoveVipPrices();

    // a reference is written by primary key, by IEntityReference, or from a typed view of the target
    [EvitaReference("brand")] int? BrandId { get; set; }
    [EvitaReference("brand")] void SetBrand(IBrand brand);
    [EvitaReference("categories")] IReadOnlyList<int> CategoryIds { get; set; }
    [EvitaReference("categories")] void AddCategory(int primaryKey);
    [EvitaReference("categories")] [RemoveWhenExists] void RemoveCategory(int primaryKey);
    [EvitaReference("categories")] [RemoveWhenExists] void RemoveAllCategories();
}
```

Semantics worth knowing:

- **"Set" means "make it be this".** A collection setter replaces the whole set of that reference, and so
  does a scalar setter on a single-cardinality (`ZeroOrOne`/`ExactlyOne`) reference — writing it twice
  leaves one reference, not two. A scalar setter on a multi-cardinality reference **adds**, since
  replacing every category because one was set would be surprising.
- **A remover with no value removes everything** in its scope: all prices (or only those of the declared
  price lists), or all references of that name. A remover taking a key removes just that one.
- **The referenced entity type and cardinality come from the schema** when the reference is described
  there; `[EvitaReference(Entity = "...")]` is needed only when creating a reference the schema does not
  yet know.
- **`[PriceForSale]` is not writable** — it is computed by the server from the query's price context, and
  a setter for it is refused with that message.

Anything still outside the declarative surface stays available on the builder:

```csharp
draft.EntityBuilder.SetPrice(1, "basic", czk, 100m, 21m, 121m, indexed: true);
draft.EntityBuilder.SetReference("categories", 10, whichIs: it => it.SetAttribute("position", 1));
```

Note the driver's **price-ambiguity rule**, which these writes surface: two prices sharing price list,
currency and inner-record id conflict unless their validity ranges are disjoint, and a price *without*
validity is always valid and therefore conflicts with any other. Writing two undated prices to the same
price list raises `AmbiguousPriceException`.

### Narrowing a reference member

A reference member may take one argument, and its shape decides how the references are narrowed — the same
three forms the Java driver accepts:

```csharp
[EvitaEntity("Product")]
public interface IProduct
{
    // by referenced primary key
    [EvitaReference("categories")] ICategoryLink? GetCategory(int primaryKey);

    // by a value compared against one of the reference's own attributes
    [EvitaReference("categories")] IReadOnlyList<ICategoryLink> AtPosition(
        [AttributeRef("position")] int position);

    // by a predicate over whatever the member projects
    [EvitaReference("categories")] IReadOnlyList<ICategoryLink> Matching(
        Predicate<ICategoryLink> filter);
}
```

An array-valued reference attribute matches when it *contains* the argument, and enums compare by name,
since that is how they are stored. A scalar member returns the first match or `null`; a collection member
returns all matches. Naming a reference attribute the reference schema does not declare fails at bind time.

Note that **members taking arguments are never memoized** — memoization is keyed by member, so caching a
filtered member would hand back the previous call's result. Parameterless nested-view members keep their
stable identity.

### Enums

evitaDB has no enum data type, so an enum-typed member is stored **by name** and the derived schema declares
it as `string` (or `string[]` for a collection) — the same mapping the Java driver applies:

```csharp
public enum ProductKind { Basic, Master, Variant }

[EvitaEntity("Product")]
public interface IProduct : ITypedEntityEditor
{
    [EvitaAttribute(Filterable = true)] ProductKind Kind { get; set; }
    [EvitaAttribute] IReadOnlyList<ProductKind>? Kinds { get; set; }
    [EvitaAttribute] Maybe<ProductKind> MaybeKind { get; }
}
```

Reading parses **case-insensitively**, since the value may have been written by another client; a name the
enum does not define raises a diagnostic rather than silently defaulting to the zero member. Nullable and
`Maybe<T>` shapes behave as they do for any other attribute.

### Writing the attributes a reference carries

A reference contract's own attributes are writable, which is how a link's `position`, `priority` or label is
maintained:

```csharp
public interface IProductCategoryLink
{
    [ReferencedEntity] int ReferencedEntityPrimaryKey { get; }
    [EvitaAttribute] int? Position { get; set; }        // attribute OF the reference
}

editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;
```

The write goes through the owning entity's builder — `SetReference(name, key, whichIs: …)` — so a reference
view needs an entity that is **open for writing**; reading one off a sealed entity and assigning to it says
so explicitly.

Two behaviours worth knowing, both forced by the driver:

- **`InitialEntityBuilder` cannot update a reference in place** (its `SetReference` does a dictionary `Add`
  and throws on a repeat; Java has a separate `updateReference` that was never ported). On a brand-new
  entity the write therefore rebuilds the reference, **carrying its existing attributes across**.
  `ExistingEntityBuilder` already updates in place and is used unchanged.
- **Nested views are memoized only over an immutable source.** Once a builder exists the entity is being
  edited, so caching a nested view would keep reporting the state a reference had before the last write.
  Identity is still stable when reading a fetched entity, which is what the memoization is for.

### Deep upsert: writing an entity together with what it references

A reference is written by primary key, so a brand-new referenced entity poses a chicken-and-egg problem:
the server assigns its key only when it is upserted, but the parent needs that key to point at it. Attach
the child's editor and let the deep upsert order the writes:

```csharp
var product = session.CreateNewModel<IProductEditor>();
var brand   = session.CreateNewModel<IBrandEditor>();   // no primary key yet
brand.Name = "Acme";

product.AttachReferenced("brand", brand);

IReadOnlyList<EntityReference> upserted = product.UpsertDeeplyVia(session);
// brand is upserted first, its generated key is written into product's `brand` reference,
// then product is upserted; the list comes back children-first, root last
```

Rules:

- **Children first, root last**, depth-first, with each child's returned primary key written into the
  referencing entity before that entity is sent.
- **An editor attached to two parents is upserted once**, and both parents receive its key.
- **Nothing is sent for an editor with no mutation** — an existing entity opened for write and left alone
  costs no round trip. A *new* entity is always sent, because creating it is itself the change.
- **Cycles are refused** with a diagnostic rather than looping; break one side and set that reference by
  primary key instead.

`UpsertDeeplyWith(Func<IEntityBuilder, EntityReference>)` is the same traversal with the upsert injected,
which is how ordering and key backfill are tested without a server.

This is deliberately explicit rather than Java's `@CreateWhenMissing`, which materializes a referenced
entity implicitly on read. Attaching an editor you created yourself keeps the object graph visible at the
call site and avoids a read silently creating data.

### Reflected references

A category does not store its products — the product stores its categories, and evitaDB materialises the
inverse on the category:

```csharp
[EvitaEntity("Category")]
public interface ICategory
{
    [ReflectedReference("products", OfEntity = "Product", OfName = "categories")]
    IReadOnlyList<int> ProductIds { get; }        // every reference projection works here too
}
```

Reading is identical to a normal reference — a reflected reference arrives as an ordinary `IReference`, so
keys, `IEntityReference`, nested entity views and reference contracts all apply.

Two things are refused rather than faked, both because of gaps in this driver:

- **Writing one** — the server derives it from the other side, so change the reference it reflects.
- **Deriving its schema** — `IEntitySchemaEditor` exposes no reflected-reference method and
  `ReflectedReferenceSchemaBuilder` is an empty stub, so `DefineEntitySchemaFromModelType<T>()` refuses
  with that explanation. Emitting a plain reference instead would silently create a second, independently
  maintained reference rather than the server-maintained inverse. Define the reflected reference on the
  server (or via `CreateReflectedReferenceSchemaMutation`) and use the annotation for reading.

### Accompanying prices

Prices evitaDB computes next to the selling price, from a different price-list priority. They are a
**query-time** computation, not schema — request them with `AccompanyingPriceContent(name, priceLists)`
(optionally `DefaultAccompanyingPriceLists(...)`), both of which the driver already supports:

```csharp
[EvitaEntity("Product")]
public interface IProduct
{
    [PriceForSale] IPrice? SellingPrice { get; }
    [AccompanyingPrice("reference")] IPrice? ReferencePrice { get; }
    [AccompanyingPrice] IPrice? Default { get; }   // matches a query that named none
}
```

> **This driver does not carry them yet.** The wire does — `GrpcEntity.accompanyingPrices` is a
> `map<string, GrpcPrice>` — but `EntityConverter` never reads that field and `IPrices` has nowhere to put
> it. Reads therefore go through `IAccompanyingPriceSource`, and an entity that does not implement it is
> reported with exactly that explanation instead of silently returning `null`. Closing the gap is two small
> changes, after which the interface becomes a thin adapter that keeps working: give `Prices` an
> `IReadOnlyDictionary<string, IPrice>` and let `Entity` implement the interface over it, then populate it
> in `EntityConverter.ToEntity` beside the existing `new Prices(...)` call.

## Absence: three states, two shapes

evitaDB distinguishes *not fetched* from *fetched but empty*. Java encodes the caller's choice in the
signature (`Optional<T>`, plain type, or a declared `throws ContextMissingException`); C# has neither
`Optional` nor checked exceptions, so the choice is the member's type:

| Declared as | not fetched | fetched, no value | has value |
|---|---|---|---|
| `string? Name` | `null` | `null` | value |
| `Maybe<string> Name` | `MaybeState.NotFetched` | `MaybeState.Absent` | `MaybeState.Present` |
| `int Quantity` (cannot express absence) | throws `ContextMissingException`&nbsp;* | schema default, else `0` | value |

\* not reachable in the current driver — see the note below.

`default(Maybe<T>)` is `NotFetched`, so a zeroed struct never claims that absence was observed. Use
`IsFetched`, `HasValue`, `TryGetValue`, `ValueOrDefault`, `ValueOr(fallback)` or `Value` (which throws,
with a message that says which of the two absences occurred).

> **Reality check on the middle column.** This driver has no partial-fetch decorator: every
> implementation of `AttributesAvailable()` in `Models/Data/Structure/` returns a hard-coded `true`
> (the Java driver instead wraps partially-fetched entities in `EntityDecorator` with availability
> predicates). So **`NotFetched` and `ContextMissingException` cannot occur for attribute reads today** —
> reading an attribute the query did not fetch is indistinguishable from reading one with no value, and
> both report `Absent` / `null`. The state is implemented and tested at the type level so that the
> distinction becomes real the moment the driver gains availability tracking, without changing any
> contract. Until then, treat `Maybe<T>`'s value over a plain nullable as: *"absent" is explicit rather
> than conflated with a zero value* (`Maybe<int>` distinguishes absent from `0`, `int?` does too, but
> `int` cannot).

Prefer a plain nullable member when you do not care why a value is missing; reach for `Maybe<T>` when
absence must be distinguishable from a legitimate zero/empty value.

## Two implementation paths: generated class or runtime proxy

A typed contract can be materialized two ways. Both go through the same descriptor and binding, and both
present the same API — but the generated one is strictly more capable:

| | Generated class (`EvitaDB.Client.Generators`) | Runtime proxy (`DispatchProxy`) |
|---|---|---|
| Default interface members | **inherited and executed** | rejected with a diagnostic |
| Class / record contracts | possible | interfaces only |
| Reflection per call | none | none (dictionary lookup) |
| Trim / AOT | safe | needs `Reflection.Emit` support |
| Setup | reference the generator package | nothing |

The factory prefers a generated implementation whenever one is registered, so adding the generator to a
project silently upgrades every contract in it. Nothing else changes at the call site.

### Wiring the generator into a project

```xml
<ItemGroup>
  <PackageReference Include="EvitaDB.Client.Generators" Version="…"
                   PrivateAssets="all" IncludeAssets="analyzers" />
</ItemGroup>
```

or, inside this repository:

```xml
<ItemGroup>
  <ProjectReference Include="..\EvitaDB.Client.Generators\EvitaDB.Client.Generators.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

For every interface annotated `[EvitaEntity]`/`[EvitaEntityRef]` the generator emits a
`sealed class …__View : TypedEntityView, IYourContract` that forwards each **abstract** member to
`Read`/`Write` by name, and a `[ModuleInitializer]` that registers it with `TypedViewRegistry`. Members
with a default implementation are deliberately *not* emitted, which is what lets them run their own body.
Everything else — the bound contract, the lazy builder, read-through, nested-view memoization, and the
`ITypedEntity`/`ITypedEntityEditor` mixins — comes from `TypedEntityView`, so the generated file contains
nothing but member forwarding:

```csharp
public sealed class Shop_IProduct__View : TypedEntityView, Shop.IProduct
{
    public int Id => (int)Read("Id")!;

    public string? Name
    {
        get => (string?)Read("Name")!;
        set => Write("Name", new object?[] { value });
    }

    public IProduct SetWeight(decimal value) { Write("SetWeight", new object?[] { value }); return this; }
}
```

Members the generator skips (and the proxy would reject too): indexers, generic methods, and methods with
`ref`/`out` parameters.

### Record contracts

A **positional record** is bound by constructor rather than proxied — it is an immutable snapshot, so there
is nothing to intercept:

```csharp
[EvitaEntity("Product")]
public record ProductRecord(int Id, string? Name, decimal? Weight);
```

The generator emits a binder that reads each constructor parameter under the matching property name and
constructs the record, then registers it exactly like a view. The result is a real record: `GetType()` is
your type, and value equality works. Two notes:

- **Only positional records** are bound; one declared with a body has no parameter list to map, and is
  skipped.
- **Annotate through the property**, as C# would otherwise attach the attribute to the constructor
  parameter: `[property: EvitaAttribute("ean")] string? Ean`. Unannotated parameters need nothing — they
  resolve by the usual camel-case inference.
- Records are read-only; an editor contract must be an interface.

## Architecture

Three stages, so that per-call reflection is never on the read path:

1. **Describe** (`TypedContractAnalyzer`) — reflect over the contract once per process and produce a
   `TypedContractDescriptor`: members, resolved names, shapes, absence policies. Nothing here looks at
   a schema. A source generator can emit exactly this shape at compile time.
2. **Bind** (`BoundTypedContract`) — resolve the descriptor against a live `IEntitySchema` and compile
   one reader delegate per member into a `FrozenDictionary<MethodInfo, …>`. This step is inherently
   runtime work: localization, indexed decimal places, defaults and name-variant matching are only
   knowable from the schema. Cached per `(contract type, entity type)` — the same contract may be bound
   to more than one collection.
3. **Materialize** (`TypedEntityFactory`) — a generated `TypedEntityView` subclass when one is registered,
   otherwise a `DispatchProxy` whose `Invoke` is a dictionary lookup keyed by the invoked accessor.
   Generated code addresses members by *name* (`ReadByMember`/`WriteByMember`) rather than by
   `MethodInfo`, which is what keeps it reflection-free.

Schema derivation (`ModelSchemaAnalyzer`) walks the same descriptor and applies it to the fluent
`IEntitySchemaBuilder`, so it produces mutations the caller sends when ready.

## Current limitations

Supported today: interface contracts; primary key; entity type; scalar, array and localized attributes;
references in all four projections, reference contracts with their own attributes, reference groups and
parent navigation, including nested typed views with stable identity; prices (stored, narrowed by price
list, and price-for-sale); associated data including POCO payloads; `Maybe<T>` and nullable shapes; schema
derivation for all of those.

Not yet implemented — each needs its own increment:

- **`[CreateWhenMissing]`**: materializing a referenced entity implicitly when a reference member is read.
  Deep upsert covers the same need explicitly (see above); this would make it implicit.
- **`PriceInnerRecordHandling`** exposure on contracts.
- **Array-typed associated data written from the client.** The reader supports it via
  `GetAssociatedDataArray`, but the driver's client-side write path routes arrays through
  `ComplexDataObjectConverter.GetSerializableForm`, which only passes *scalar* supported types straight
  through — so an array-typed associated data schema cannot be satisfied by a client-built entity. The
  read path is therefore only exercisable against a real server.
- **Reflected-reference schema authoring** (blocked on the driver gap above). Other still-missing pieces (`@ReflectedReference`), sortable attribute compounds, and the per-scope
  (`LIVE` / `ARCHIVED`) index settings.
- **Filtered reference access** — the Java driver lets a reference member take a referenced-entity id, a
  predicate, or an `[AttributeRef]`-derived equality filter as a parameter. Here a reference member is
  parameterless (or takes a `CultureInfo` for localized attributes).
- **Default interface members on the proxy path.** Rejected with a clear error there, *not* silently
  ignored — `DispatchProxy` intercepts default-implemented members too (verified), and delegating to a
  default body non-virtually requires `Reflection.Emit`. Add the generator to the project and they work.

## Deliberate deviations from the Java driver

1. **Additive schema reconciliation.** Java treats the annotated class as the sole source of truth and
   resets schema flags that sit at their annotation default, so re-analysing a bare
   `@Attribute String name()` *clears* a previously-set `filterable`. Here a contract only ever adds or
   widens; removal stays an explicit act through the schema API.
2. **Empty collections are `Present`, not absent.** Java normalises empty collections to
   `Optional.empty()`; in C# reading an empty list as "absent" reads as a bug.
3. **Distinct method names** (`QueryOneModelAsync`, `QueryModelListAsync`, `GetModelAsync`) rather than
   overloads of the existing `QueryOne`/`QueryList`, which are constrained to `IEntityClassifier`.
   Relying on a constraint mismatch to fall through to an extension method produces poor diagnostics.
4. **No parameter-name-based constructor scoring.** Java scores constructors by parameter name (needing
   `-parameters`); the generated positional binder will replace that with a compile-time-checked one.

## Related

- `documentation/architecture.md` — driver layering and schema caching
- `documentation/async-api.md` — async-core/sync-facade convention these APIs follow
- `EvitaDB.Test/Tests/TypedModelTest.cs` — server-free tests covering every rule above
