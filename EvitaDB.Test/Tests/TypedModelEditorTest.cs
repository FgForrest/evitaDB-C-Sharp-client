using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Typed;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Tests of editor (write) contracts: property setters, fluent setters, localized setters, removers and
/// the accumulated mutation. Server-free, like the other typed model suites.
/// </summary>
public class TypedModelEditorTest
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs");

    /// <summary>A plain POCO stored as associated data.</summary>
    public class Localization
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    [EvitaEntity("Editable")]
    public interface IEditableProduct : ITypedEntityEditor
    {
        int Id { get; }

        [EvitaAttribute(Filterable = true)] string? Name { get; set; }
        [EvitaAttribute] decimal? Weight { get; }
        [EvitaAttribute] IEditableProduct SetWeight(decimal value);
        [EvitaAttribute(Localized = true)] string? GetTitle(CultureInfo locale);
        [EvitaAttribute(Localized = true)] void SetTitle(CultureInfo locale, string value);
        [EvitaAttribute] IReadOnlyList<string>? Tags { get; set; }
        [EvitaAssociatedData] Localization? Localization { get; set; }
        [ParentEntity] int? ParentId { get; set; }

        [EvitaAttribute("name")] [RemoveWhenExists] void RemoveName();
    }

    /// <summary>A deliberately non-hierarchical, price-less entity - see
    /// <see cref="ShouldBuildAnInstanceOfANonHierarchicalEntity"/>.</summary>
    [EvitaEntity("Flat")]
    public interface IEditableFlatProduct : ITypedEntityEditor
    {
        [EvitaAttribute] string? Name { get; set; }
    }

    [EvitaEntity("Editable")]
    public interface IEditableWithPriceSetter : ITypedEntityEditor
    {
        [EvitaPrice] IReadOnlyList<IPrice> Prices { get; }
        [EvitaPrice] void SetPrices(IReadOnlyList<IPrice> prices);
    }

    // ---------------------------------------------------------------------
    // writing
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldStartWithoutChanges()
    {
        IEditableProduct draft = NewDraft();

        Assert.False(draft.HasChanges);
    }

    [Fact]
    public void ShouldWriteAndReadBackThroughPropertySetter()
    {
        IEditableProduct draft = NewDraft();

        draft.Name = "Speaker";

        Assert.Equal("Speaker", draft.Name);
        Assert.True(draft.HasChanges);
    }

    [Fact]
    public void ShouldReturnEditorItselfFromFluentSetter()
    {
        IEditableProduct draft = NewDraft();

        IEditableProduct returned = draft.SetWeight(1.25m);

        Assert.Same(draft, returned);
        Assert.Equal(1.25m, draft.Weight);
    }

    [Fact]
    public void ShouldWriteCollectionAsTypedArray()
    {
        // the driver stores the value's own runtime type, so the array must be string[] rather than
        // object[] or the schema type check rejects it
        IEditableProduct draft = NewDraft();

        draft.Tags = new[] { "audio", "portable" };

        Assert.Equal(new[] { "audio", "portable" }, draft.Tags);
    }

    [Fact]
    public void ShouldWriteLocalizedAttributePerLocale()
    {
        IEditableProduct draft = NewDraft();

        draft.SetTitle(Czech, "Reproduktor");

        Assert.Equal("Reproduktor", draft.GetTitle(Czech));
    }

    [Fact]
    public void ShouldWriteAssociatedDataFromPoco()
    {
        IEditableProduct draft = NewDraft();

        draft.Localization = new Localization { Title = "Speaker", Description = "Loud" };

        Assert.Equal("Loud", draft.Localization!.Description);
    }

    [Fact]
    public void ShouldWriteParentPlacement()
    {
        IEditableProduct draft = NewDraft();

        draft.ParentId = 400;

        Assert.Equal(400, draft.ParentId);
    }

    [Fact]
    public void ShouldRemoveValueThroughRemover()
    {
        IEditableProduct draft = NewDraft();
        draft.Name = "Speaker";

        draft.RemoveName();

        Assert.Null(draft.Name);
    }

    [Fact]
    public void ShouldExposeAccumulatedMutationAndInstance()
    {
        IEditableProduct draft = NewDraft();
        draft.Name = "Speaker";

        Assert.NotNull(draft.ToMutation());
        Assert.Equal("Speaker", draft.ToInstance().GetAttribute("name"));
    }

    // ---------------------------------------------------------------------
    // editing a fetched entity
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldNotMutateTheFetchedEntity()
    {
        ISealedEntity fetched = BuildFetchedEntity();

        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();
        editor.Name = "Changed";

        Assert.Equal("Changed", editor.Name);
        Assert.Equal("Original", fetched.GetAttribute("name"));
    }

    [Fact]
    public void ShouldProduceNoMutationWhenNothingChanged()
    {
        // regression: the driver's change-set builders can contribute a *null* local mutation, which made
        // an untouched editor look changed. Fixed in the driver, and additionally normalized by the typed
        // layer so the contract holds on any driver build
        ISealedEntity fetched = BuildFetchedEntity();

        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();

        Assert.Null(editor.ToMutation());
        Assert.False(editor.HasChanges);
    }

    [Fact]
    public void ShouldOverwriteAnAttributeWrittenTwice()
    {
        // regression: the change set is keyed by attribute, and the port registered mutations with
        // Dictionary.Add where Java uses Map.put - so the second write to the same attribute threw
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();

        editor.Name = "Changed";
        editor.Name = "Changed again";

        Assert.Equal("Changed again", editor.Name);
        Assert.Equal("Changed again", editor.ToInstance().GetAttribute("name"));
    }

    [Fact]
    public void ShouldOverwriteAValueWrittenTwiceOnEveryContainer()
    {
        // the same porting bug sat in the attribute, associated data, price and reference change sets
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();

        editor.SetTitle(Czech, "Reproduktor");
        editor.SetTitle(Czech, "Zesilovac");
        editor.Localization = new Localization { Title = "First" };
        editor.Localization = new Localization { Title = "Second" };

        Assert.Equal("Zesilovac", editor.GetTitle(Czech));
        Assert.Equal("Second", editor.Localization!.Title);
    }

    [Fact]
    public void ShouldBuildAnInstanceOfANonHierarchicalEntity()
    {
        // regression: rebuilding an existing entity read the parent through the accessor that asserts the
        // entity is hierarchical, so ToInstance() threw for every non-hierarchical one. Every other fixture
        // that reaches this path happens to be hierarchical, which is why it stayed hidden
        IEditableFlatProduct editor = BuildFlatEntity().OpenForWrite<IEditableFlatProduct>();
        editor.Name = "Changed";

        Assert.Equal("Changed", editor.ToInstance().GetAttribute("name"));
    }

    [Fact]
    public void ShouldStopListingRemovedValues()
    {
        // Java filters the collection accessors through Droppable::exists; the port listed the tombstone,
        // so a removed value still appeared in the names, keys and values of the container
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();
        editor.Localization = new Localization { Title = "Speaker" };

        editor.RemoveName();
        editor.EntityBuilder.RemoveAssociatedData("localization");

        Assert.DoesNotContain("name", editor.EntityBuilder.GetAttributeNames());
        Assert.Empty(editor.EntityBuilder.GetAttributeValues("name"));
        Assert.DoesNotContain("localization", editor.EntityBuilder.GetAssociatedDataNames());
    }

    [Fact]
    public void ShouldStillReadAttributesLeftAlone()
    {
        // regression: the read path answered only for attributes that had a mutation of their own, so once
        // anything was written every untouched attribute read back as null
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();

        editor.SetWeight(2.5m);

        Assert.Equal("Original", editor.Name);
    }

    [Fact]
    public void ShouldRemoveAnAttributeThatWasJustWritten()
    {
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();

        editor.Name = "Changed";
        editor.RemoveName();

        Assert.Null(editor.Name);
    }

    [Fact]
    public void ShouldNeverEmitNullLocalMutations()
    {
        ISealedEntity fetched = BuildFetchedEntity();
        IEditableProduct editor = fetched.OpenForWrite<IEditableProduct>();
        editor.Name = "Changed";

        IEntityMutation? mutation = editor.ToMutation();

        Assert.NotNull(mutation);
        EntityUpsertMutation upsert = Assert.IsType<EntityUpsertMutation>(mutation);
        Assert.NotEmpty(upsert.LocalMutations);
        Assert.All(upsert.LocalMutations, it => Assert.NotNull(it));
    }

    // ---------------------------------------------------------------------
    // diagnostics
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldWritePricesDeclaratively()
    {
        // prices were once builder-only; they are declarative now, and a whole IPrice is the unit of write
        // because its identity is (price id, price list, currency)
        IEditableWithPriceSetter draft =
            TypedEntityFactory.CreateNew<IEditableWithPriceSetter>(BuildSchema(), 1);

        draft.SetPrices([
            new Price(new PriceKey(1, "basic", new Currency("CZK")), null, 100m, 21m, 121m, null, true)
        ]);

        Assert.Single(draft.Prices);
        Assert.Equal(121m, draft.Prices[0].PriceWithTax);
    }

    [Fact]
    public void ShouldExposeBuilderForMutationsWithoutDeclarativeSupport()
    {
        IEditableProduct draft = NewDraft();

        draft.EntityBuilder.SetPrice(1, "basic", new Currency("CZK"), 100m, 21m, 121m, true);

        Assert.Single(draft.ToInstance().GetPrices());
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static IEditableProduct NewDraft() =>
        TypedEntityFactory.CreateNew<IEditableProduct>(BuildSchema(), 500);

    private static ISealedEntity BuildFetchedEntity()
    {
        InitialEntityBuilder builder = new(BuildSchema(), 501);
        builder.SetAttribute("name", "Original");
        return builder.ToInstance();
    }

    private static ISealedEntity BuildFlatEntity()
    {
        InitialEntityBuilder builder = new(FlatSchema(), 601);
        builder.SetAttribute("name", "Original");
        return builder.ToInstance();
    }

    /// <summary>Non-hierarchical and price-less, unlike <see cref="BuildSchema"/>.</summary>
    private static IEntitySchema FlatSchema() => EntitySchema.InternalBuild(
        1, "Flat", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>
        {
            ["name"] = EntityAttributeSchema.InternalBuild("name", typeof(string), false)
        },
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>(),
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );

    private static IEntitySchema BuildSchema() => EntitySchema.InternalBuild(
        1, "Editable", null, null, false, true, true, 2,
        new HashSet<CultureInfo> { Czech },
        new HashSet<Currency> { new("CZK") },
        new Dictionary<string, IEntityAttributeSchema>
        {
            ["name"] = EntityAttributeSchema.InternalBuild("name", typeof(string), false),
            ["weight"] = EntityAttributeSchema.InternalBuild("weight", typeof(decimal), false),
            ["tags"] = EntityAttributeSchema.InternalBuild("tags", typeof(string[]), false),
            ["title"] = EntityAttributeSchema.InternalBuild("title", typeof(string), true)
        },
        new Dictionary<string, IAssociatedDataSchema>
        {
            ["localization"] = AssociatedDataSchema.InternalBuild(
                "localization", null, null, false, true, typeof(Localization))
        },
        new Dictionary<string, IReferenceSchema>(),
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );
}
