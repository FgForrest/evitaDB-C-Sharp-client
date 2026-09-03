using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Typed;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Tests of prices and associated data in the typed-contract layer. Server-free, like the other typed
/// model suites.
/// </summary>
public class TypedModelPriceAndDataTest
{
    private static readonly Currency Czk = new("CZK");

    /// <summary>A plain POCO stored as associated data.</summary>
    public class Localization
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    [EvitaEntity("Priced")]
    public interface IPricedProduct
    {
        int Id { get; }

        [PriceForSale(AllowedCurrencies = ["CZK"])] IPrice? SellingPrice { get; }
        [PriceForSale] IReadOnlyList<IPrice> AllSellingPrices { get; }
        [EvitaPrice] IReadOnlyList<IPrice> Prices { get; }
        [EvitaPrice(PriceList = ["vip"])] IPrice? VipPrice { get; }

        [EvitaAssociatedData] Localization? Localization { get; }
        [EvitaAssociatedData] Maybe<string> Warranty { get; }
        [EvitaAssociatedData] Maybe<string> Manual { get; }
        [AssociatedDataRef("warranty")] string? WarrantyAlias { get; }
    }

    [EvitaEntity("Priced")]
    public interface IPricedWithScalarPriceMember
    {
        [EvitaPrice] decimal? Price { get; }
    }

    // ---------------------------------------------------------------------
    // prices
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldExposeAllStoredPrices()
    {
        IPricedProduct product = BuildPricedProduct();

        Assert.Equal(3, product.Prices.Count);
        Assert.Equal(100m, product.Prices.Single(it => it.PriceList == "basic").PriceWithoutTax);
    }

    [Fact]
    public void ShouldNarrowPricesToDeclaredPriceLists()
    {
        IPricedProduct product = BuildPricedProduct();

        Assert.NotNull(product.VipPrice);
        Assert.Equal("vip", product.VipPrice!.PriceList);
        Assert.Equal(80m, product.VipPrice.PriceWithTax);
    }

    [Fact]
    public void ShouldExposePricesEligibleForSale()
    {
        IPricedProduct product = BuildPricedProduct();

        // only prices marked sellable qualify; the reference price does not
        Assert.NotEmpty(product.AllSellingPrices);
        Assert.DoesNotContain("reference", product.AllSellingPrices.Select(it => it.PriceList));
    }

    [Fact]
    public void ShouldReturnNullSellingPriceWithoutQueryPriceContext()
    {
        // the selling price is computed by evitaDB from the query's currency and price lists; without
        // that context there is nothing to return
        IPricedProduct product = BuildPricedProduct();

        Assert.Null(product.SellingPrice);
    }

    [Fact]
    public void ShouldRejectPriceMemberThatDoesNotExposeIPrice()
    {
        ISealedEntity entity = BuildPricedEntity();

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { _ = entity.AsModel<IPricedWithScalarPriceMember>().Price; }
        );
        Assert.Contains("must expose `IPrice`", exception.Message);
    }

    // ---------------------------------------------------------------------
    // associated data
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldDeserializeComplexAssociatedDataIntoPoco()
    {
        IPricedProduct product = BuildPricedProduct();

        Assert.NotNull(product.Localization);
        Assert.Equal("Speaker", product.Localization!.Title);
        Assert.Equal("Loud", product.Localization.Description);
    }

    [Fact]
    public void ShouldReadScalarAssociatedData()
    {
        IPricedProduct product = BuildPricedProduct();

        Assert.Equal(MaybeState.Present, product.Warranty.State);
        Assert.Equal("24 months", product.Warranty.Value);
        Assert.Equal("24 months", product.WarrantyAlias);
    }

    [Fact]
    public void ShouldReportDeclaredButUnsetAssociatedDataAsAbsent()
    {
        // an entity assembled on the client knows only the associated data set on it and raises
        // AttributeNotFoundException for anything else; since the member's name was validated against the
        // schema at bind time, that can only mean "no value here"
        IPricedProduct product = BuildPricedProduct();

        Assert.Equal(MaybeState.Absent, product.Manual.State);
        Assert.False(product.Manual.HasValue);
    }

    [Fact]
    public void ShouldFailAtBindTimeWhenAnnotatedAssociatedDataIsMissingFromSchema()
    {
        ISealedEntity entity = BuildPricedEntity();

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { entity.AsModel<IPricedWithUnknownAssociatedData>(); }
        );
        Assert.Contains("does not exist in schema", exception.Message);
    }

    [EvitaEntity("Priced")]
    public interface IPricedWithUnknownAssociatedData
    {
        [EvitaAssociatedData("nope")] string? Missing { get; }
    }

    // ---------------------------------------------------------------------
    // schema derivation
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldDeclareEntityPricedWithAllowedCurrencies()
    {
        IEntitySchema derived = DeriveSchema<IPricedProduct>();

        Assert.True(derived.WithPrice());
        Assert.Contains(Czk, derived.Currencies);
    }

    [Fact]
    public void ShouldDeriveAssociatedDataSchemas()
    {
        IEntitySchema derived = DeriveSchema<IPricedProduct>();

        Assert.True(derived.AssociatedData.ContainsKey("localization"));
        Assert.True(derived.AssociatedData.ContainsKey("warranty"));
        // [AssociatedDataRef] reads existing data without declaring it
        Assert.False(derived.AssociatedData.ContainsKey("warrantyAlias"));
    }

    [Fact]
    public void ShouldNormalizeComplexPayloadTypeToComplexDataObject()
    {
        // evitaDB stores anything outside its own data types as a complex object, so that is the type the
        // schema records - the POCO type lives only in the contract
        IEntitySchema derived = DeriveSchema<IPricedProduct>();

        Assert.Equal(typeof(ComplexDataObject), derived.AssociatedData["localization"].Type);
        Assert.Equal(typeof(string), derived.AssociatedData["warranty"].Type);
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static IPricedProduct BuildPricedProduct() => BuildPricedEntity().AsModel<IPricedProduct>();

    private static ISealedEntity BuildPricedEntity()
    {
        EntitySchema schema = EntitySchema.InternalBuild(
            1, "Priced", null, null, false, false, true, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency> { Czk },
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>
            {
                ["localization"] = AssociatedDataSchema.InternalBuild(
                    "localization", null, null, false, true, typeof(Localization)),
                ["warranty"] = AssociatedDataSchema.InternalBuild(
                    "warranty", null, null, false, true, typeof(string)),
                ["manual"] = AssociatedDataSchema.InternalBuild(
                    "manual", null, null, false, true, typeof(string))
            },
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );

        InitialEntityBuilder builder = new(schema, 1);
        builder.SetPrice(1, "basic", Czk, 100m, 21m, 121m, true);
        builder.SetPrice(2, "vip", Czk, 66m, 21m, 80m, true);
        builder.SetPrice(3, "reference", Czk, 200m, 21m, 242m, false);
        builder.SetAssociatedData("localization", new Localization
        {
            Title = "Speaker",
            Description = "Loud"
        });
        builder.SetAssociatedData("warranty", "24 months");
        return builder.ToInstance();
    }

    private static IEntitySchema DeriveSchema<T>()
    {
        TypedContractDescriptor descriptor = TypedContractAnalyzer.Describe<T>();

        CatalogSchema catalogSchema = CatalogSchema.InternalBuild(
            "testCatalog",
            EvitaDB.Client.Utils.NamingConventionHelper.Generate("testCatalog"),
            new HashSet<CatalogEvolutionMode> { CatalogEvolutionMode.AddingEntityTypes },
            new EmptyEntitySchemaProvider()
        );

        IEntitySchemaBuilder builder = new InternalEntitySchemaBuilder(
            catalogSchema, EntitySchema.InternalBuild(descriptor.EntityType)
        );

        ModelSchemaAnalyzer.Apply(descriptor, builder);
        return builder.ToInstance();
    }

    private sealed class EmptyEntitySchemaProvider : IEntitySchemaProvider
    {
        public IEnumerable<IEntitySchema?> GetEntitySchemas() => [];

        public IEntitySchema? GetEntitySchema(string name) => null;
    }
}
