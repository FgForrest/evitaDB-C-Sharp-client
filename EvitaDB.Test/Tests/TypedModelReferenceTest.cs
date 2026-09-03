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
/// Tests of references, nested typed views and parent navigation in the typed-contract layer. Like
/// <see cref="TypedModelTest"/> these run without a server - entities, references and their schemas are
/// all constructible in process.
/// </summary>
public class TypedModelReferenceTest
{
    // ---------------------------------------------------------------------
    // contracts under test
    // ---------------------------------------------------------------------

    [EvitaEntity("Brand")]
    public interface IBrand
    {
        int Id { get; }
        [EvitaAttribute] string? Name { get; }
    }

    [EvitaEntity("Category")]
    public interface ICategoryNode
    {
        int Id { get; }
        [EvitaAttribute] string? Name { get; }
    }

    /// <summary>
    /// A reference contract: no type-level annotation, its members read the reference itself.
    /// </summary>
    public interface IProductCategoryLink
    {
        [ReferencedEntity] ICategoryNode Category { get; }
        [ReferencedEntity] int ReferencedEntityPrimaryKey { get; }
        [EvitaAttribute(Filterable = true)] int? Position { get; }
        [ReferencedEntityGroup] int? GroupPrimaryKey { get; }
    }

    [EvitaEntity("Product")]
    public interface IProductWithReferences
    {
        int Id { get; }

        [EvitaReference("brand", Entity = "Brand", Indexed = true)]
        IBrand? Brand { get; }

        [EvitaReference("categories", Faceted = true)]
        IReadOnlyList<IProductCategoryLink> Categories { get; }

        // an integer projection reveals nothing about the target, so the entity type must be stated
        [EvitaReference("categories", Entity = "Category")]
        IReadOnlyList<int> CategoryIds { get; }

        [ReferenceRef("brand")] int? BrandId { get; }
    }

    [EvitaEntity("Product")]
    public interface IProductWithUnfetchedReferenceBody
    {
        [EvitaReference("tagsRef", Entity = "Tag")] ITag? Tag { get; }
    }

    [EvitaEntity("Tag")]
    public interface ITag
    {
        int Id { get; }
    }

    [EvitaEntity("Category")]
    public interface ICategoryWithParentKey
    {
        int Id { get; }
        [ParentEntity] int? ParentId { get; }
    }

    [EvitaEntity("Category")]
    public interface ICategoryWithTypedParent
    {
        int Id { get; }
        [ParentEntity] ICategoryWithTypedParent? Parent { get; }
    }

    // ---------------------------------------------------------------------
    // reading references
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldReadScalarReferenceAsNestedTypedView()
    {
        IProductWithReferences product = BuildProduct();

        Assert.NotNull(product.Brand);
        Assert.Equal(7, product.Brand!.Id);
        Assert.Equal("Acme", product.Brand.Name);
    }

    [Fact]
    public void ShouldReadIntegerProjectedReferenceAsPrimaryKey()
    {
        IProductWithReferences product = BuildProduct();

        Assert.Equal(7, product.BrandId);
        Assert.Equal(new[] { 10, 20 }, product.CategoryIds.OrderBy(it => it));
    }

    [Fact]
    public void ShouldReadReferenceCollectionAsReferenceContractViews()
    {
        IProductWithReferences product = BuildProduct();

        Assert.Equal(2, product.Categories.Count);
        Assert.Equal(
            new[] { 10, 20 },
            product.Categories.Select(it => it.ReferencedEntityPrimaryKey).OrderBy(it => it)
        );
    }

    [Fact]
    public void ShouldReadAttributesOfTheReferenceItself()
    {
        IProductWithReferences product = BuildProduct();

        IProductCategoryLink audio =
            product.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10);
        IProductCategoryLink portable =
            product.Categories.Single(it => it.ReferencedEntityPrimaryKey == 20);

        Assert.Equal(1, audio.Position);
        Assert.Equal(2, portable.Position);
    }

    [Fact]
    public void ShouldNavigateFromReferenceToReferencedEntityBody()
    {
        IProductWithReferences product = BuildProduct();

        IProductCategoryLink audio =
            product.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10);

        Assert.Equal("Audio", audio.Category.Name);
    }

    [Fact]
    public void ShouldReadReferenceGroupPrimaryKey()
    {
        IProductWithReferences product = BuildProduct();

        IProductCategoryLink audio =
            product.Categories.Single(it => it.ReferencedEntityPrimaryKey == 10);
        IProductCategoryLink portable =
            product.Categories.Single(it => it.ReferencedEntityPrimaryKey == 20);

        Assert.Equal(99, audio.GroupPrimaryKey);
        Assert.Null(portable.GroupPrimaryKey);
    }

    [Fact]
    public void ShouldReturnSameNestedViewInstanceAcrossReads()
    {
        // stable identity is what makes walking an object graph possible, and is the read-side half of
        // the per-proxy instance cache the Java driver keeps
        IProductWithReferences product = BuildProduct();

        Assert.Same(product.Brand, product.Brand);
    }

    [Fact]
    public void ShouldExposeUnderlyingReferenceOfAReferenceView()
    {
        IProductWithReferences product = BuildProduct();

        IReference reference = TypedEntityFactory.GetReference(product.Categories[0]);

        Assert.Equal("categories", reference.ReferenceName);
    }

    [Fact]
    public void ShouldRejectEntityAccessorOnAReferenceView()
    {
        IProductWithReferences product = BuildProduct();

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { TypedEntityFactory.GetEntity(product.Categories[0]); }
        );
        Assert.Contains("reference view", exception.Message);
    }

    [Fact]
    public void ShouldDiagnoseMissingReferencedBodyInsteadOfReturningNull()
    {
        ISealedEntity entity = BuildProductWithKeyOnlyReference();

        ContextMissingException exception = Assert.Throws<ContextMissingException>(
            () => { _ = entity.AsModel<IProductWithUnfetchedReferenceBody>().Tag; }
        );
        Assert.Contains("was not fetched", exception.Message);
    }

    // ---------------------------------------------------------------------
    // parent navigation
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldReadParentPrimaryKeyWithoutFetchedBody()
    {
        // the parent key is available on its own, even when the parent body was not fetched
        ISealedEntity child = BuildCategoryWithParentKey();

        Assert.Equal(100, child.AsModel<ICategoryWithParentKey>().ParentId);
    }

    [Fact]
    public void ShouldReadParentAsNestedTypedViewWhenBodyWasFetched()
    {
        ISealedEntity child = BuildCategoryWithParentBody();

        ICategoryWithTypedParent typed = child.AsModel<ICategoryWithTypedParent>();

        Assert.NotNull(typed.Parent);
        Assert.Equal(100, typed.Parent!.Id);
        Assert.Same(typed.Parent, typed.Parent);
    }

    [Fact]
    public void ShouldDiagnoseMissingParentBodyInsteadOfReturningNull()
    {
        ISealedEntity child = BuildCategoryWithParentKey();

        ContextMissingException exception = Assert.Throws<ContextMissingException>(() => { _ = child.AsModel<ICategoryWithTypedParent>().Parent; });
        Assert.Contains("only its primary key", exception.Message);
    }

    // ---------------------------------------------------------------------
    // schema derivation
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldDeriveReferencesFromContract()
    {
        IEntitySchema derived = DeriveSchema<IProductWithReferences>();

        Assert.True(derived.References.ContainsKey("brand"));
        Assert.True(derived.References.ContainsKey("categories"));
        // [ReferenceRef] reads an existing reference without declaring one
        Assert.Equal(2, derived.References.Count);
    }

    [Fact]
    public void ShouldResolveReferencedEntityTypeFromContractOrDeclaration()
    {
        IEntitySchema derived = DeriveSchema<IProductWithReferences>();

        Assert.Equal("Brand", derived.References["brand"].ReferencedEntityType);
        // taken from the reference contract's [ReferencedEntity] member
        Assert.Equal("Category", derived.References["categories"].ReferencedEntityType);
    }

    [Fact]
    public void ShouldDeriveCardinalityFromCollectionShape()
    {
        IEntitySchema derived = DeriveSchema<IProductWithReferences>();

        Assert.Equal(Cardinality.ZeroOrOne, derived.References["brand"].Cardinality);
        Assert.Equal(Cardinality.ZeroOrMore, derived.References["categories"].Cardinality);
    }

    [Fact]
    public void ShouldPropagateReferenceFlagsAndAttributes()
    {
        IEntitySchema derived = DeriveSchema<IProductWithReferences>();

        Assert.True(derived.References["brand"].IsIndexed);
        Assert.True(derived.References["categories"].IsFaceted);

        IAttributeSchema? position = derived.References["categories"].GetAttribute("position");
        Assert.NotNull(position);
        Assert.True(position!.Filterable());
    }

    [Fact]
    public void ShouldMakeSchemaHierarchicalWhenContractDeclaresParent()
    {
        Assert.True(DeriveSchema<ICategoryWithTypedParent>().WithHierarchy());
    }

    [Fact]
    public void ShouldRefuseReferenceThatDoesNotSayWhichEntityItPointsTo()
    {
        EvitaInvalidUsageException exception =
            Assert.Throws<EvitaInvalidUsageException>(DeriveSchema<IProductWithUntargetedReference>);
        Assert.Contains("does not say which entity", exception.Message);
    }

    [EvitaEntity("Product")]
    public interface IProductWithUntargetedReference
    {
        [EvitaReference("something")] int? SomethingId { get; }
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static IProductWithReferences BuildProduct()
    {
        IEntitySchema brandSchema = BuildSchema("Brand", hierarchical: false, ("name", typeof(string)));
        IEntitySchema categorySchema = BuildSchema("Category", hierarchical: true, ("name", typeof(string)));

        InitialEntityBuilder brand = new(brandSchema, 7);
        brand.SetAttribute("name", "Acme");

        InitialEntityBuilder audio = new(categorySchema, 10);
        audio.SetAttribute("name", "Audio");

        InitialEntityBuilder portable = new(categorySchema, 20);
        portable.SetAttribute("name", "Portable");

        InitialEntityBuilder product = new(BuildProductSchemaWithReferences(), 1);
        product.SetReference("brand", "Brand", Cardinality.ZeroOrOne, 7);
        product.SetReference("categories", "Category", Cardinality.ZeroOrMore, 10,
            it => it.SetAttribute("position", 1).SetGroup("CategoryGroup", 99));
        product.SetReference("categories", "Category", Cardinality.ZeroOrMore, 20,
            it => it.SetAttribute("position", 2));

        ISealedEntity entity = AttachReferencedBodies(
            product.ToInstance(),
            new Dictionary<string, IReadOnlyDictionary<int, ISealedEntity>>
            {
                ["brand"] = new Dictionary<int, ISealedEntity> { [7] = brand.ToInstance() },
                ["categories"] = new Dictionary<int, ISealedEntity>
                {
                    [10] = audio.ToInstance(),
                    [20] = portable.ToInstance()
                }
            }
        );

        return entity.AsModel<IProductWithReferences>();
    }

    /// <summary>
    /// Rebuilds an entity with referenced bodies attached to its references - the shape the driver
    /// produces after a <c>referenceContent(entityFetch(...))</c> query. Reference attributes are carried
    /// over so a reference contract can still read them.
    /// </summary>
    private static ISealedEntity AttachReferencedBodies(
        Entity entity,
        IReadOnlyDictionary<string, IReadOnlyDictionary<int, ISealedEntity>> bodiesByReference)
    {
        List<IReference> rebuilt = [];

        foreach (IReference reference in entity.GetReferences())
        {
            bodiesByReference.TryGetValue(reference.ReferenceName, out var bodies);
            ISealedEntity? body = bodies?.GetValueOrDefault(reference.ReferencedPrimaryKey);

            rebuilt.Add(new Reference(
                entity.Schema,
                reference.Version,
                reference.ReferenceName,
                reference.ReferencedPrimaryKey,
                reference.ReferencedEntityType,
                reference.ReferenceCardinality,
                reference.Group,
                reference.GetAttributeValues().ToDictionary(it => it.Key, it => it),
                body
            ));
        }

        // reading Parent on a non-hierarchical schema throws, so ask the schema first
        bool hierarchical = entity.Schema.WithHierarchy();

        return Entity.InternalBuild(
            entity, entity.PrimaryKey, entity.Version, entity.Schema,
            hierarchical ? entity.Parent : null,
            hierarchical ? entity.ParentEntity : null,
            rebuilt, null, null, null, null
        );
    }

    private static ISealedEntity BuildProductWithKeyOnlyReference()
    {
        EntitySchema schema = EntitySchema.InternalBuild(
            1, "Product", null, null, false, false, false, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency>(),
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>
            {
                ["tagsRef"] = ReferenceSchema.InternalBuild(
                    "tagsRef", "Tag", true, Cardinality.ZeroOrOne, null, false, true, false)
            },
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );

        InitialEntityBuilder product = new(schema, 1);
        product.SetReference("tagsRef", "Tag", Cardinality.ZeroOrOne, 5);
        return product.ToInstance();
    }

    private static ISealedEntity BuildCategoryWithParentKey()
    {
        InitialEntityBuilder child = new(BuildSchema("Category", true, ("name", typeof(string))), 200);
        child.SetParent(100);
        return child.ToInstance();
    }

    private static ISealedEntity BuildCategoryWithParentBody()
    {
        IEntitySchema schema = BuildSchema("Category", true, ("name", typeof(string)));

        InitialEntityBuilder parent = new(schema, 100);
        parent.SetAttribute("name", "Root");

        InitialEntityBuilder child = new(schema, 200);
        child.SetParent(100);

        return Entity.InternalBuild(
            child.ToInstance(), 200, 1, schema, 100, parent.ToInstance(),
            null, null, null, null, null
        );
    }

    private static IEntitySchema BuildProductSchemaWithReferences()
    {
        Dictionary<string, IReferenceSchema> references = new()
        {
            ["brand"] = ReferenceSchema.InternalBuild(
                "brand", "Brand", true, Cardinality.ZeroOrOne, null, false, true, false),
            ["categories"] = ReferenceSchema.InternalBuild(
                "categories", null, null, "Category", true, Cardinality.ZeroOrMore, "CategoryGroup", false,
                true, true,
                new Dictionary<string, IAttributeSchema>
                {
                    ["position"] = AttributeSchema.InternalBuild("position", typeof(int), false)
                },
                new Dictionary<string, SortableAttributeCompoundSchema>())
        };

        return EntitySchema.InternalBuild(
            1, "Product", null, null, false, false, false, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency>(),
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>(),
            references,
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );
    }

    private static IEntitySchema BuildSchema(
        string name,
        bool hierarchical,
        params (string Name, Type Type)[] attributes)
    {
        Dictionary<string, IEntityAttributeSchema> attributeSchemas = attributes.ToDictionary(
            it => it.Name,
            it => (IEntityAttributeSchema)EntityAttributeSchema.InternalBuild(it.Name, it.Type, false)
        );

        return EntitySchema.InternalBuild(
            1, name, null, null, false, hierarchical, false, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency>(),
            attributeSchemas,
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );
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
