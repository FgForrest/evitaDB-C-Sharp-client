using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Typed;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Tests of declarative price and reference writes through editor contracts, plus the price-ambiguity
/// rule they exercise. Server-free, like the other typed model suites.
/// </summary>
public class TypedModelWriteReferenceTest
{
    private static readonly Currency Czk = new("CZK");

    [EvitaEntity("Priced")]
    public interface IPriceEditor : ITypedEntityEditor
    {
        [EvitaPrice] IReadOnlyList<IPrice> Prices { get; }
        [EvitaPrice] void SetPrice(IPrice price);
        [EvitaPrice] void SetPrices(IReadOnlyList<IPrice> prices);
        [EvitaPrice] [RemoveWhenExists] void RemoveAllPrices();
        [EvitaPrice(PriceList = ["vip"])] [RemoveWhenExists] void RemoveVipPrices();
        [PriceForSale] void SetSellingPrice(IPrice price);
    }

    [EvitaEntity("Brand")]
    public interface IBrand
    {
        int Id { get; }
    }

    [EvitaEntity("Referencing")]
    public interface IReferenceEditor : ITypedEntityEditor
    {
        [EvitaReference("brand")] int? BrandId { get; set; }
        [EvitaReference("brand")] void SetBrandFromView(IBrand brand);
        [EvitaReference("categories")] IReadOnlyList<int> CategoryIds { get; set; }
        [EvitaReference("categories")] void AddCategory(int primaryKey);
        [EvitaReference("categories")] [RemoveWhenExists] void RemoveCategory(int primaryKey);
        [EvitaReference("categories")] [RemoveWhenExists] void RemoveAllCategories();
    }

    // ---------------------------------------------------------------------
    // prices
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldWriteWholePriceThroughScalarSetter()
    {
        IPriceEditor editor = NewPriceEditor();

        editor.SetPrice(Price(1, "basic", 100m, 121m));

        Assert.Single(editor.Prices);
        Assert.Equal("basic", editor.Prices[0].PriceList);
        Assert.Equal(121m, editor.Prices[0].PriceWithTax);
    }

    [Fact]
    public void ShouldWriteManyPricesThroughCollectionSetter()
    {
        IPriceEditor editor = NewPriceEditor();

        editor.SetPrices([Price(1, "basic", 100m, 121m), Price(2, "vip", 66m, 80m)]);

        Assert.Equal(2, editor.Prices.Count);
    }

    [Fact]
    public void ShouldRemoveOnlyPricesOfTheDeclaredPriceList()
    {
        IPriceEditor editor = NewPriceEditor();
        editor.SetPrices([Price(1, "basic", 100m, 121m), Price(2, "vip", 66m, 80m)]);

        editor.RemoveVipPrices();

        Assert.Single(editor.Prices);
        Assert.Equal("basic", editor.Prices[0].PriceList);
    }

    [Fact]
    public void ShouldRemoveAllPricesWhenRemoverIsUnscoped()
    {
        IPriceEditor editor = NewPriceEditor();
        editor.SetPrices([Price(1, "basic", 100m, 121m), Price(2, "vip", 66m, 80m)]);

        editor.RemoveAllPrices();

        Assert.Empty(editor.Prices);
    }

    [Fact]
    public void ShouldRefuseToWriteTheSellingPrice()
    {
        IPriceEditor editor = NewPriceEditor();

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => editor.SetSellingPrice(Price(1, "basic", 100m, 121m))
        );
        Assert.Contains("computed by the server", exception.Message);
    }

    [Fact]
    public void ShouldRejectAmbiguousPriceInsteadOfThrowingNullReference()
    {
        // regression: the ambiguity check read `price.Validity != null && (...) || it.Validity!.Overlaps(...)`,
        // whose precedence made it dereference a null validity - so adding a second price without validity
        // to the same price list and currency raised NullReferenceException instead of the intended
        // AmbiguousPriceException
        IPriceEditor editor = NewPriceEditor();
        editor.SetPrice(Price(1, "basic", 100m, 121m));

        Assert.Throws<AmbiguousPriceException>(() => editor.SetPrice(Price(2, "basic", 90m, 109m)));
    }

    [Fact]
    public void ShouldAllowSamePriceListWhenValidityRangesDoNotOverlap()
    {
        IPriceEditor editor = NewPriceEditor();
        DateTimeOffset start = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        editor.SetPrice(Price(1, "basic", 100m, 121m,
            DateTimeRange.Between(start, start.AddDays(10))));
        editor.SetPrice(Price(2, "basic", 90m, 109m,
            DateTimeRange.Between(start.AddDays(20), start.AddDays(30))));

        Assert.Equal(2, editor.Prices.Count);
    }

    // ---------------------------------------------------------------------
    // references
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldWriteReferenceByPrimaryKey()
    {
        IReferenceEditor editor = NewReferenceEditor();

        editor.BrandId = 7;

        Assert.Equal(7, editor.BrandId);
    }

    [Fact]
    public void ShouldWriteReferenceFromTypedView()
    {
        IReferenceEditor editor = NewReferenceEditor();
        IBrand brand = BuildBrand(9);

        editor.SetBrandFromView(brand);

        Assert.Equal(9, editor.BrandId);
    }

    [Fact]
    public void ShouldReplaceSingleCardinalityReferenceRatherThanAddASecond()
    {
        // "set" means "make it be this"; a ZeroOrOne reference written twice must not end up with two
        IReferenceEditor editor = NewReferenceEditor();

        editor.BrandId = 7;
        editor.BrandId = 9;

        Assert.Equal(9, editor.BrandId);
        Assert.Single(editor.EntityBuilder.GetReferences("brand"));
    }

    [Fact]
    public void ShouldReplaceWholeSetThroughCollectionSetter()
    {
        IReferenceEditor editor = NewReferenceEditor();

        editor.CategoryIds = [10, 20];
        editor.CategoryIds = [20, 30];

        Assert.Equal([20, 30], editor.CategoryIds.OrderBy(it => it));
    }

    [Fact]
    public void ShouldAddToMultiCardinalityReferenceThroughScalarSetter()
    {
        // a scalar setter on a ZeroOrMore reference adds, rather than replacing every existing one
        IReferenceEditor editor = NewReferenceEditor();

        editor.AddCategory(10);
        editor.AddCategory(20);

        Assert.Equal([10, 20], editor.CategoryIds.OrderBy(it => it));
    }

    [Fact]
    public void ShouldRemoveOneReferenceByKey()
    {
        IReferenceEditor editor = NewReferenceEditor();
        editor.CategoryIds = [10, 20];

        editor.RemoveCategory(20);

        Assert.Equal([10], editor.CategoryIds);
    }

    [Fact]
    public void ShouldRemoveAllReferencesWhenRemoverTakesNoKey()
    {
        IReferenceEditor editor = NewReferenceEditor();
        editor.CategoryIds = [10, 20];

        editor.RemoveAllCategories();

        Assert.Empty(editor.CategoryIds);
    }

    [Fact]
    public void ShouldCarryReferenceWritesIntoTheMutation()
    {
        IReferenceEditor editor = NewReferenceEditor();

        editor.BrandId = 7;

        Assert.NotNull(editor.ToMutation());
        Assert.True(editor.HasChanges);
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static IPrice Price(
        int priceId,
        string priceList,
        decimal withoutTax,
        decimal withTax,
        DateTimeRange? validity = null) =>
        new Price(new PriceKey(priceId, priceList, Czk), null, withoutTax, 21m, withTax, validity, true);

    private static IPriceEditor NewPriceEditor() =>
        TypedEntityFactory.CreateNew<IPriceEditor>(BuildPricedSchema(), 700);

    private static IReferenceEditor NewReferenceEditor() =>
        TypedEntityFactory.CreateNew<IReferenceEditor>(BuildReferencingSchema(), 800);

    private static IBrand BuildBrand(int primaryKey)
    {
        EntitySchema schema = EntitySchema.InternalBuild(
            1, "Brand", null, null, false, false, false, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency>(),
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );

        return new InitialEntityBuilder(schema, primaryKey).ToInstance().AsModel<IBrand>();
    }

    private static IEntitySchema BuildPricedSchema() => EntitySchema.InternalBuild(
        1, "Priced", null, null, false, false, true, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency> { Czk },
        new Dictionary<string, IEntityAttributeSchema>(),
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>(),
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );

    private static IEntitySchema BuildReferencingSchema() => EntitySchema.InternalBuild(
        1, "Referencing", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>(),
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>
        {
            ["brand"] = ReferenceSchema.InternalBuild(
                "brand", "Brand", true, Cardinality.ZeroOrOne, null, false, true, false),
            ["categories"] = ReferenceSchema.InternalBuild(
                "categories", "Category", true, Cardinality.ZeroOrMore, null, false, true, false)
        },
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );
}
