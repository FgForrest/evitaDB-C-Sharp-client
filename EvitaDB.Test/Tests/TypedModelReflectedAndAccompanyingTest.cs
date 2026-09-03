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
/// Tests of reflected references and accompanying prices.
/// <remarks>
/// Both features sit on driver gaps that these tests pin deliberately: the fluent schema builder cannot
/// declare a reflected reference, and the entity model does not carry accompanying prices. In both cases
/// the typed layer refuses clearly instead of producing something subtly wrong.
/// </remarks>
/// </summary>
public class TypedModelReflectedAndAccompanyingTest
{
    private static readonly Currency Czk = new("CZK");

    // ---------------------------------------------------------------------
    // reflected references
    // ---------------------------------------------------------------------

    [EvitaEntity("ReflectingCategory")]
    public interface IReflectingCategory
    {
        int Id { get; }

        [ReflectedReference("products", OfEntity = "Product", OfName = "categories")]
        IReadOnlyList<int> ProductIds { get; }
    }

    [EvitaEntity("ReflectingCategory")]
    public interface IReflectingCategoryEditor : ITypedEntityEditor
    {
        [ReflectedReference("products", OfEntity = "Product", OfName = "categories")]
        void SetProductIds(IReadOnlyList<int> ids);
    }

    [Fact]
    public void ShouldReadReflectedReferenceLikeAnyOtherReference()
    {
        // a category does not store its products - the product stores its categories, and evitaDB
        // materialises the inverse, which arrives as an ordinary reference
        IReflectingCategory category = BuildReflectingCategory().AsModel<IReflectingCategory>();

        Assert.Equal([300, 301], category.ProductIds.OrderBy(it => it));
    }

    [Fact]
    public void ShouldRefuseToWriteAReflectedReference()
    {
        IReflectingCategoryEditor editor =
            TypedEntityFactory.CreateNew<IReflectingCategoryEditor>(ReflectingCategorySchema(), 1);

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { editor.SetProductIds([1]); }
        );
        Assert.Contains("maintained by the server", exception.Message);
    }

    /// <summary>
    /// A reflected reference derives into a reflected reference, not a plain one.
    /// </summary>
    /// <remarks>
    /// This used to assert the opposite — that the driver refused to derive one at all, because emitting a
    /// plain reference would silently create a second, independently maintained reference instead of the
    /// server-maintained inverse the contract asked for. Refusing was the right answer while
    /// <c>WithReflectedReferenceToEntity</c> did not exist; it does now, so the assertion is that the derived
    /// schema really carries the reflection rather than that the attempt is rejected.
    /// </remarks>
    [Fact]
    public void ShouldDeriveAReflectedReferenceRatherThanAPlainOne()
    {
        IEntitySchema derived = DeriveSchema<IReflectingCategory>();

        IReferenceSchema products = derived.References["products"];

        IReflectedReferenceSchema reflected = Assert.IsAssignableFrom<IReflectedReferenceSchema>(products);

        Assert.Equal("categories", reflected.ReflectedReferenceName);
        Assert.Equal("Product", reflected.ReferencedEntityType);
    }

    // ---------------------------------------------------------------------
    // accompanying prices
    // ---------------------------------------------------------------------

    [EvitaEntity("Priced")]
    public interface IProductWithAccompanyingPrices
    {
        [AccompanyingPrice("reference")] IPrice? ReferencePrice { get; }
        [AccompanyingPrice] IPrice? Default { get; }
    }

    /// <summary>
    /// Supplies accompanying prices the way a driver build that carried them would - see
    /// <see cref="IAccompanyingPriceSource"/> for the two changes that would make this unnecessary.
    /// </summary>
    private sealed class SourceWithAccompanyingPrices(
        ISealedEntity entity,
        Dictionary<string, IPrice> accompanying)
        : IAccompanyingPriceSource
    {
        internal ISealedEntity Entity { get; } = entity;

        public IPrice? GetAccompanyingPrice(string accompanyingPriceName) =>
            accompanying.GetValueOrDefault(accompanyingPriceName);

        public IReadOnlyCollection<string> GetAccompanyingPriceNames() => accompanying.Keys;
    }

    [Fact]
    public void ShouldReadNamedAccompanyingPrice()
    {
        SourceWithAccompanyingPrices source = BuildAccompanyingSource();
        BoundTypedContract contract = BindAccompanying(source);

        object? value = contract.ReadByMember(
            nameof(IProductWithAccompanyingPrices.ReferencePrice), source, null);

        IPrice price = Assert.IsAssignableFrom<IPrice>(value);
        Assert.Equal("reference", price.PriceList);
        Assert.Equal(242m, price.PriceWithTax);
    }

    [Fact]
    public void ShouldFallBackToTheDefaultAccompanyingPriceName()
    {
        // a query that named no accompanying price stores it under "default"
        SourceWithAccompanyingPrices source = BuildAccompanyingSource();
        BoundTypedContract contract = BindAccompanying(source);

        object? value = contract.ReadByMember(
            nameof(IProductWithAccompanyingPrices.Default), source, null);

        IPrice price = Assert.IsAssignableFrom<IPrice>(value);
        Assert.Equal("basic", price.PriceList);
    }

    [Fact]
    public void ShouldDiagnoseEntitiesThatCarryNoAccompanyingPrices()
    {
        // this is the current driver: EntityConverter never reads GrpcEntity.accompanyingPrices
        SourceWithAccompanyingPrices source = BuildAccompanyingSource();
        BoundTypedContract contract = BindAccompanying(source);

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => contract.ReadByMember(
                nameof(IProductWithAccompanyingPrices.ReferencePrice), source.Entity, null)
        );
        Assert.Contains("does not supply accompanying prices", exception.Message);
    }

    [Fact]
    public void ShouldDescribeAccompanyingPriceMembers()
    {
        TypedContractDescriptor descriptor =
            TypedContractAnalyzer.Describe<IProductWithAccompanyingPrices>();

        Assert.Equal(
            2,
            descriptor.Members.Count(it => it.Kind == TypedMemberKind.AccompanyingPrice)
        );
        Assert.Contains(
            descriptor.Members,
            it => it.Kind == TypedMemberKind.AccompanyingPrice && it.DataName == "reference"
        );
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static BoundTypedContract BindAccompanying(SourceWithAccompanyingPrices source) =>
        BoundTypedContract.Bind(typeof(IProductWithAccompanyingPrices), source.Entity.Schema);

    private static SourceWithAccompanyingPrices BuildAccompanyingSource()
    {
        EntitySchema schema = EntitySchema.InternalBuild(
            1, "Priced", null, null, false, false, true, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency> { Czk },
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );

        return new SourceWithAccompanyingPrices(
            new InitialEntityBuilder(schema, 1).ToInstance(),
            new Dictionary<string, IPrice>
            {
                ["reference"] = new Price(
                    new PriceKey(9, "reference", Czk), null, 200m, 21m, 242m, null, false),
                ["default"] = new Price(
                    new PriceKey(8, "basic", Czk), null, 100m, 21m, 121m, null, true)
            }
        );
    }

    private static ISealedEntity BuildReflectingCategory()
    {
        InitialEntityBuilder builder = new(ReflectingCategorySchema(), 200);
        builder.SetReference("products", 300);
        builder.SetReference("products", 301);
        return builder.ToInstance();
    }

    private static IEntitySchema ReflectingCategorySchema() => EntitySchema.InternalBuild(
        1, "ReflectingCategory", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>(),
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>
        {
            // a genuine ReflectedReferenceSchema, not a plain reference standing in for one
            ["products"] = ReflectedReferenceSchema.InternalBuild("products", "Product", "categories")
        },
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );

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
