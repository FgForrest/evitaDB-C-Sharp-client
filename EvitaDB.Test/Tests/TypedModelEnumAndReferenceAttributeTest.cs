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
/// Tests of enum-typed members and of writing the attributes a reference carries. Server-free, like the
/// other typed model suites.
/// </summary>
public class TypedModelEnumAndReferenceAttributeTest
{
    public enum ProductKind
    {
        Basic,
        Master,
        Variant
    }

    // ---------------------------------------------------------------------
    // enums
    // ---------------------------------------------------------------------

    [EvitaEntity("Enums")]
    public interface IEnumProduct : ITypedEntityEditor
    {
        [EvitaAttribute(Filterable = true)] ProductKind Kind { get; set; }
        [EvitaAttribute] ProductKind? OptionalKind { get; }
        [EvitaAttribute] Maybe<ProductKind> MaybeKind { get; }
        [EvitaAttribute] IReadOnlyList<ProductKind>? Kinds { get; set; }
    }

    [Fact]
    public void ShouldStoreEnumByNameAndReadItBack()
    {
        // evitaDB has no enum data type, so the value travels as the member's name
        IEnumProduct product = NewEnumEditor();

        product.Kind = ProductKind.Master;

        Assert.Equal(ProductKind.Master, product.Kind);
        Assert.Equal("Master", product.ToInstance().GetAttribute("kind"));
    }

    [Fact]
    public void ShouldRoundTripEnumCollections()
    {
        IEnumProduct product = NewEnumEditor();

        product.Kinds = [ProductKind.Basic, ProductKind.Variant];

        Assert.Equal([ProductKind.Basic, ProductKind.Variant], product.Kinds!);
    }

    [Fact]
    public void ShouldReportUnsetEnumMembersAsAbsent()
    {
        IEnumProduct product = NewEnumEditor();

        Assert.Null(product.OptionalKind);
        Assert.Equal(MaybeState.Absent, product.MaybeKind.State);
    }

    [Fact]
    public void ShouldParseEnumNamesCaseInsensitively()
    {
        // the value may have been written by another client with different casing conventions
        InitialEntityBuilder builder = new(EnumSchema(), 2);
        builder.SetAttribute("kind", "vArIaNt");

        Assert.Equal(ProductKind.Variant, builder.ToInstance().AsModel<IEnumProduct>().Kind);
    }

    [Fact]
    public void ShouldDiagnoseUnknownEnumNameInsteadOfDefaulting()
    {
        InitialEntityBuilder builder = new(EnumSchema(), 3);
        builder.SetAttribute("kind", "NoSuchKind");

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { _ = builder.ToInstance().AsModel<IEnumProduct>().Kind; }
        );
        Assert.Contains("has no such member", exception.Message);
    }

    [Fact]
    public void ShouldDeclareEnumMembersAsStringInTheSchema()
    {
        IEntitySchema derived = DeriveSchema<IEnumProduct>();

        Assert.Equal(typeof(string), derived.Attributes["kind"].Type);
        Assert.Equal(typeof(string[]), derived.Attributes["kinds"].Type);
    }

    // ---------------------------------------------------------------------
    // attributes carried by a reference
    // ---------------------------------------------------------------------

    /// <summary>A reference contract whose own attribute is writable.</summary>
    public interface ICategoryLinkEditor
    {
        [ReferencedEntity] int ReferencedEntityPrimaryKey { get; }
        [EvitaAttribute] int? Position { get; set; }
    }

    [EvitaEntity("Referencing")]
    public interface IReferenceAttributeEditor : ITypedEntityEditor
    {
        [EvitaReference("categories")] IReadOnlyList<int> CategoryIds { get; set; }
        [EvitaReference("categories")] IReadOnlyList<ICategoryLinkEditor> Categories { get; }
    }

    [Fact]
    public void ShouldWriteAnAttributeCarriedByOneReference()
    {
        IReferenceAttributeEditor editor = NewReferenceEditor();

        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;

        Assert.Equal(
            1,
            editor.EntityBuilder.GetReferences("categories")
                .Single(it => it.ReferencedPrimaryKey == 10)
                .GetAttribute("position")
        );
    }

    [Fact]
    public void ShouldLeaveSiblingReferencesUntouched()
    {
        IReferenceAttributeEditor editor = NewReferenceEditor();

        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;

        Assert.Null(
            editor.EntityBuilder.GetReferences("categories")
                .Single(it => it.ReferencedPrimaryKey == 20)
                .GetAttribute("position")
        );
    }

    [Fact]
    public void ShouldReadTheWrittenReferenceAttributeBack()
    {
        // nested views are not memoized once a builder exists, or this would return the pre-write state
        IReferenceAttributeEditor editor = NewReferenceEditor();

        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;

        Assert.Equal(1, editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position);
    }

    [Fact]
    public void ShouldPreserveOtherAttributesOfTheSameReference()
    {
        // InitialEntityBuilder cannot update a reference in place, so the write rebuilds it - anything it
        // already carried has to come along
        IReferenceAttributeEditor editor = NewReferenceEditor();
        editor.EntityBuilder.SetReference("categories", 30, it => it.SetAttribute("label", "kept"));

        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 30).Position = 7;

        IReference rebuilt = editor.EntityBuilder.GetReferences("categories")
            .Single(it => it.ReferencedPrimaryKey == 30);
        Assert.Equal("kept", rebuilt.GetAttribute("label"));
        Assert.Equal(7, rebuilt.GetAttribute("position"));
    }

    [Fact]
    public void ShouldCarryReferenceAttributeWritesIntoTheMutation()
    {
        IReferenceAttributeEditor editor = NewReferenceEditor();

        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;

        Assert.NotNull(editor.ToMutation());
    }

    [Fact]
    public void ShouldRefuseReferenceAttributeWriteOnAnEntityNotOpenForWriting()
    {
        IReferenceAttributeEditor editor = NewReferenceEditor();
        ISealedEntity sealedEntity = editor.ToInstance();

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { sealedEntity.AsModel<IReferenceAttributeEditor>().Categories.First().Position = 9; }
        );
        Assert.Contains("not open for writing", exception.Message);
    }

    // ---------------------------------------------------------------------
    // filtered reference access
    // ---------------------------------------------------------------------

    public interface ICategoryLinkView
    {
        [ReferencedEntity] int ReferencedEntityPrimaryKey { get; }
        [EvitaAttribute] int? Position { get; }
    }

    [EvitaEntity("Referencing")]
    public interface IFilteredReferences
    {
        [EvitaReference("categories")] ICategoryLinkView? GetCategory(int primaryKey);

        [EvitaReference("categories")] IReadOnlyList<ICategoryLinkView> AtPosition(
            [AttributeRef("position")] int position);

        [EvitaReference("categories")] IReadOnlyList<ICategoryLinkView> Matching(
            Predicate<ICategoryLinkView> filter);

        [EvitaReference("categories")] IReadOnlyList<int> AllIds { get; }
    }

    [Fact]
    public void ShouldFilterReferencesByReferencedPrimaryKey()
    {
        IFilteredReferences product = BuildFilterable();

        Assert.Equal(20, product.GetCategory(20)!.ReferencedEntityPrimaryKey);
    }

    [Fact]
    public void ShouldReturnNothingWhenNoReferenceMatches()
    {
        // regression: nested views are memoized by member, so before parameters were taken into account
        // this returned whatever the previous call had cached
        IFilteredReferences product = BuildFilterable();

        Assert.Equal(20, product.GetCategory(20)!.ReferencedEntityPrimaryKey);
        Assert.Null(product.GetCategory(999));
    }

    [Fact]
    public void ShouldFilterReferencesByOneOfTheirOwnAttributes()
    {
        IFilteredReferences product = BuildFilterable();

        Assert.Equal(
            [20, 30],
            product.AtPosition(2).Select(it => it.ReferencedEntityPrimaryKey).OrderBy(it => it)
        );
    }

    [Fact]
    public void ShouldFilterReferencesByPredicateOverTheProjectedView()
    {
        IFilteredReferences product = BuildFilterable();

        Assert.Equal(2, product.Matching(it => it.ReferencedEntityPrimaryKey > 15).Count);
    }

    [Fact]
    public void ShouldLeaveUnfilteredMembersAlone()
    {
        IFilteredReferences product = BuildFilterable();

        Assert.Equal(3, product.AllIds.Count);
    }

    private static IFilteredReferences BuildFilterable()
    {
        IReferenceAttributeEditor editor =
            TypedEntityFactory.CreateNew<IReferenceAttributeEditor>(ReferencingSchema(), 5);
        editor.CategoryIds = [10, 20, 30];
        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10).Position = 1;
        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 20).Position = 2;
        editor.Categories.Single(it => it.ReferencedEntityPrimaryKey == 30).Position = 2;
        return editor.ToInstance().AsModel<IFilteredReferences>();
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static IEnumProduct NewEnumEditor() =>
        TypedEntityFactory.CreateNew<IEnumProduct>(EnumSchema(), 1);

    private static IReferenceAttributeEditor NewReferenceEditor()
    {
        IReferenceAttributeEditor editor =
            TypedEntityFactory.CreateNew<IReferenceAttributeEditor>(ReferencingSchema(), 1);
        editor.CategoryIds = [10, 20];
        return editor;
    }

    private static IEntitySchema EnumSchema() => EntitySchema.InternalBuild(
        1, "Enums", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>
        {
            ["kind"] = EntityAttributeSchema.InternalBuild("kind", typeof(string), false),
            ["optionalKind"] = EntityAttributeSchema.InternalBuild("optionalKind", typeof(string), false),
            ["maybeKind"] = EntityAttributeSchema.InternalBuild("maybeKind", typeof(string), false),
            ["kinds"] = EntityAttributeSchema.InternalBuild("kinds", typeof(string[]), false)
        },
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>(),
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );

    private static IEntitySchema ReferencingSchema() => EntitySchema.InternalBuild(
        1, "Referencing", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>(),
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>
        {
            ["categories"] = ReferenceSchema.InternalBuild(
                "categories", null, null, "Category", true, Cardinality.ZeroOrMore, null, false, true, false,
                new Dictionary<string, IAttributeSchema>
                {
                    ["position"] = AttributeSchema.InternalBuild("position", typeof(int), false),
                    ["label"] = AttributeSchema.InternalBuild("label", typeof(string), false)
                },
                new Dictionary<string, SortableAttributeCompoundSchema>())
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
