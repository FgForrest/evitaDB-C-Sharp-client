using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Entities;
using EvitaDB.Client.Models.Typed;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Tests of the typed entity-contract layer. These run without a server: contracts bind against an
/// <see cref="IEntitySchema"/> and an <see cref="ISealedEntity"/>, both of which can be built in
/// process, so no Docker daemon or catalog seeding is involved.
/// </summary>
public class TypedModelTest
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs");

    // ---------------------------------------------------------------------
    // contracts under test
    // ---------------------------------------------------------------------

    [EvitaEntity("Product", AllowedLocales = ["cs"])]
    public interface IProduct
    {
        int Id { get; }
        [EntityType] string Collection { get; }

        [EvitaAttribute(Filterable = true)] string? Name { get; }
        [EvitaAttribute("ean")] string? Ean { get; }
        [EvitaAttribute(Sortable = true)] Maybe<decimal> Quantity { get; }
        [EvitaAttribute] IReadOnlyList<string>? Tags { get; }
        [EvitaAttribute] Maybe<string> Subtitle { get; }
        [EvitaAttribute] Maybe<decimal> Weight { get; }
        [EvitaAttribute] Maybe<int> Rating { get; }
        [EvitaAttribute(Localized = true)] string? GetTitle(CultureInfo locale);

        string? Brand { get; }
        [AttributeRef("name")] string? Alias { get; }
    }

    [EvitaEntityRef("Product")]
    public interface IReadOnlyProduct
    {
        [AttributeRef("name")] string? Name { get; }
    }

    [EvitaEntity("Product", GeneratedPrimaryKey = true)]
    public interface IProductWithGeneratedKeys
    {
        int Id { get; }
    }

    [EvitaEntity("Product")]
    public interface IProductWithBarePrimaryKey
    {
        [PrimaryKey] int Id { get; }
    }

    // the member states the intent explicitly and wins over the entity-level flag, as its docs promise
    [EvitaEntity("Product", GeneratedPrimaryKey = true)]
    public interface IProductWithCallerSuppliedKeys
    {
        [PrimaryKey(AutoGenerate = false)] int Id { get; }
    }

    [EvitaEntity("Product")]
    public interface IProductWithDefaultMember
    {
        [EvitaAttribute] string? Name { get; }
        string Shouty => (Name ?? string.Empty).ToUpperInvariant();
    }

    [EvitaEntity("Product")]
    public interface IProductWithUnknownAttribute
    {
        [EvitaAttribute("doesNotExist")] string? Missing { get; }
    }

    [EvitaEntity("Category")]
    public interface ICategory
    {
        int Id { get; }
    }

    [EvitaEntity]
    public interface IDerivedNameContract
    {
        int Id { get; }
    }

    // ---------------------------------------------------------------------
    // reading entity data through a contract
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldReadScalarAttributesAndIdentity()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal(42, product.Id);
        Assert.Equal("Product", product.Collection);
        Assert.Equal("Bluetooth speaker", product.Name);
        Assert.Equal("EAN-123", product.Ean);
    }

    [Fact]
    public void ShouldResolveAttributeRefShortcutToSameAttribute()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal(product.Name, product.Alias);
    }

    [Fact]
    public void ShouldInferUnannotatedMemberByNamingConvention()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal("Acme", product.Brand);
    }

    [Fact]
    public void ShouldMaterializeArrayAttributeAsTypedCollection()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.NotNull(product.Tags);
        Assert.Equal(new[] { "audio", "portable" }, product.Tags);
    }

    [Fact]
    public void ShouldReadLocalizedAttributeThroughCultureInfoParameter()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal("Bluetooth reproduktor", product.GetTitle(Czech));
    }

    // ---------------------------------------------------------------------
    // three-state absence semantics
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldReportPresentValueThroughMaybe()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal(MaybeState.Present, product.Quantity.State);
        Assert.True(product.Quantity.HasValue);
        Assert.True(product.Quantity.IsFetched);
        Assert.Equal(12.5m, product.Quantity.Value);
    }

    [Fact]
    public void ShouldReportFetchedButMissingValueAsAbsent()
    {
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal(MaybeState.Absent, product.Subtitle.State);
        Assert.True(product.Subtitle.IsFetched);
        Assert.False(product.Subtitle.HasValue);
        Assert.Null(product.Subtitle.ValueOrDefault);
        Assert.Equal("fallback", product.Subtitle.ValueOr("fallback"));
    }

    [Fact]
    public void ShouldReportAbsentValueTypeWithoutFallingBackToDefault()
    {
        // regression: wrapping went through MethodBase.Invoke, which substitutes default(T) for a null
        // argument, so an unset decimal/int attribute was reported as Present(0) instead of Absent
        IProduct product = BuildProductEntity().AsModel<IProduct>();

        Assert.Equal(MaybeState.Absent, product.Weight.State);
        Assert.False(product.Weight.HasValue);
        Assert.Equal(MaybeState.Absent, product.Rating.State);
        Assert.False(product.Rating.HasValue);
    }

    [Fact]
    public void ShouldTreatDefaultMaybeAsNotFetched()
    {
        // the zero value must not claim that absence was observed
        Maybe<string> uninitialized = default;

        Assert.Equal(MaybeState.NotFetched, uninitialized.State);
        Assert.False(uninitialized.IsFetched);
        Assert.False(uninitialized.HasValue);
        Assert.Throws<InvalidOperationException>(() => { _ = uninitialized.Value; });
    }

    [Fact]
    public void ShouldReportEmptyCollectionAsPresentRatherThanAbsent()
    {
        // deliberate divergence from the Java driver, which maps empty collections to Optional.empty()
        Maybe<IReadOnlyList<string>> empty = Maybe<IReadOnlyList<string>>.Of([]);

        Assert.Equal(MaybeState.Present, empty.State);
        Assert.Empty(empty.Value);
    }

    // ---------------------------------------------------------------------
    // diagnostics
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldRejectContractMappedToDifferentEntityType()
    {
        ISealedEntity entity = BuildProductEntity();

        EvitaInvalidUsageException exception =
            Assert.Throws<EvitaInvalidUsageException>(() => { entity.AsModel<ICategory>(); });
        Assert.Contains("maps to entity", exception.Message);
    }

    [Fact]
    public void ShouldFailAtBindTimeWhenAnnotatedAttributeIsMissingFromSchema()
    {
        ISealedEntity entity = BuildProductEntity();

        EvitaInvalidUsageException exception =
            Assert.Throws<EvitaInvalidUsageException>(
                () => { entity.AsModel<IProductWithUnknownAttribute>(); });
        Assert.Contains("does not exist in schema", exception.Message);
    }

    [Fact]
    public void ShouldDiagnoseDefaultInterfaceMemberClearly()
    {
        // a runtime proxy cannot delegate to a default implementation without Reflection.Emit, so the
        // limitation is reported precisely rather than silently returning wrong data
        IProductWithDefaultMember product = BuildProductEntity().AsModel<IProductWithDefaultMember>();

        EvitaInvalidUsageException exception =
            Assert.Throws<EvitaInvalidUsageException>(() => { _ = product.Shouty; });
        Assert.Contains("default interface implementation", exception.Message);
    }

    [Fact]
    public void ShouldDescribeDefaultInterfaceMemberAsUnbound()
    {
        TypedContractDescriptor descriptor = TypedContractAnalyzer.Describe<IProductWithDefaultMember>();

        TypedMemberDescriptor member = descriptor.Members
            .Single(it => it.MemberName == nameof(IProductWithDefaultMember.Shouty));
        Assert.Equal(TypedMemberKind.Unbound, member.Kind);
        Assert.NotNull(member.UnboundReason);
    }

    // ---------------------------------------------------------------------
    // descriptor level
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldDeriveEntityTypeAndSchemaCapability()
    {
        TypedContractDescriptor product = TypedContractAnalyzer.Describe<IProduct>();
        TypedContractDescriptor readOnly = TypedContractAnalyzer.Describe<IReadOnlyProduct>();

        Assert.Equal("Product", product.EntityType);
        Assert.True(product.DrivesSchema);
        Assert.Equal("Product", readOnly.EntityType);
        Assert.False(readOnly.DrivesSchema);
    }

    [Fact]
    public void ShouldDeriveEntityTypeFromClrTypeName()
    {
        Assert.Equal("DerivedName", TypedContractAnalyzer.ExtractEntityType(typeof(IDerivedNameContract)));
    }

    [Fact]
    public void ShouldDeriveDataNameFromMemberName()
    {
        Assert.Equal("shortName", TypedContractAnalyzer.ToCamelCase("ShortName"));
        Assert.Equal(
            "title",
            TypedContractAnalyzer.DeriveDataName(typeof(IProduct).GetMethod(nameof(IProduct.GetTitle))!)
        );
    }

    [Fact]
    public void ShouldCacheBindingPerContractAndEntityType()
    {
        IEntitySchema schema = BuildProductSchema();

        Assert.Same(
            BoundTypedContract.Bind(typeof(IProduct), schema),
            BoundTypedContract.Bind(typeof(IProduct), schema)
        );
    }

    [Fact]
    public void ShouldExposeUnderlyingEntity()
    {
        ISealedEntity entity = BuildProductEntity();

        IProduct product = entity.AsModel<IProduct>();

        Assert.Same(entity, TypedEntityFactory.GetEntity(product));
    }

    // ---------------------------------------------------------------------
    // schema derivation
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldDeriveAttributeSchemasFromContract()
    {
        IEntitySchema derived = DeriveSchema<IProduct>();

        Assert.True(derived.Attributes.ContainsKey("name"));
        Assert.True(derived.Attributes.ContainsKey("ean"));
        Assert.True(derived.Attributes.ContainsKey("quantity"));
        Assert.True(derived.Attributes.ContainsKey("tags"));
        Assert.True(derived.Attributes.ContainsKey("title"));
    }

    [Fact]
    public void ShouldPropagateAttributeFlagsIntoDerivedSchema()
    {
        IEntitySchema derived = DeriveSchema<IProduct>();

        Assert.True(derived.Attributes["name"].Filterable());
        Assert.True(derived.Attributes["quantity"].Sortable());
        Assert.True(derived.Attributes["title"].Localized());
    }

    [Fact]
    public void ShouldMapMemberShapesToSchemaTypes()
    {
        IEntitySchema derived = DeriveSchema<IProduct>();

        // a collection-shaped member becomes an array type, which is how evitaDB models multi-value
        Assert.Equal(typeof(string[]), derived.Attributes["tags"].Type);
        // Maybe<T> is an absence wrapper, not part of the stored type
        Assert.Equal(typeof(decimal), derived.Attributes["quantity"].Type);
    }

    [Fact]
    public void ShouldDeclareAllowedLocalesAndLeaveAnInferredPrimaryKeyToTheServer()
    {
        IEntitySchema derived = DeriveSchema<IProduct>();

        Assert.Contains(Czech, derived.Locales);
        // `int Id` is only inferred as the primary key, so it expresses no schema intent: the flag stays at
        // whatever the schema had, which for a fresh collection is the server's default
        Assert.False(derived.WithGeneratedPrimaryKey());
    }

    [Fact]
    public void ShouldTurnGeneratedPrimaryKeysOnFromTheEntityDeclaration()
    {
        Assert.True(DeriveSchema<IProductWithGeneratedKeys>().WithGeneratedPrimaryKey());
    }

    [Fact]
    public void ShouldTurnGeneratedPrimaryKeysOnFromABarePrimaryKeyMember()
    {
        // [PrimaryKey] defaults to AutoGenerate = true, exactly as the Java annotation does
        Assert.True(DeriveSchema<IProductWithBarePrimaryKey>().WithGeneratedPrimaryKey());
    }

    [Fact]
    public void ShouldTurnGeneratedPrimaryKeysOffWhenTheMemberSaysCallerSupplied()
    {
        // the schema had generation on; the contract's member says off and beats the entity-level `true`
        InternalEntitySchemaBuilder builder = NewProductSchemaBuilder(withGeneratedPrimaryKey: true);

        ModelSchemaAnalyzer.Apply(TypedContractAnalyzer.Describe<IProductWithCallerSuppliedKeys>(), builder);

        Assert.False(builder.ToInstance().WithGeneratedPrimaryKey());
        SetEntitySchemaWithGeneratedPrimaryKeyMutation mutation = Assert.Single(
            builder.Mutations.OfType<SetEntitySchemaWithGeneratedPrimaryKeyMutation>()
        );
        Assert.False(mutation.WithGeneratedPrimaryKey);
    }

    [Fact]
    public void ShouldNotEmitAPrimaryKeyMutationWhenTheSchemaAlreadyAgrees()
    {
        // re-defining a contract against a catalog that already matches must be a no-op, not a mutation storm
        InternalEntitySchemaBuilder builder = NewProductSchemaBuilder(withGeneratedPrimaryKey: true);

        ModelSchemaAnalyzer.Apply(TypedContractAnalyzer.Describe<IProductWithBarePrimaryKey>(), builder);

        Assert.True(builder.ToInstance().WithGeneratedPrimaryKey());
        Assert.Empty(builder.Mutations.OfType<SetEntitySchemaWithGeneratedPrimaryKeyMutation>());
    }

    [Fact]
    public void ShouldNotDefineSchemaForReadOnlyOrInferredMembers()
    {
        IEntitySchema derived = DeriveSchema<IProduct>();

        // [AttributeRef] and unannotated members read existing data without claiming to define it
        Assert.False(derived.Attributes.ContainsKey("alias"));
        Assert.False(derived.Attributes.ContainsKey("brand"));
    }

    [Fact]
    public void ShouldRefuseSchemaDerivationFromReadOnlyContract()
    {
        EvitaInvalidUsageException exception =
            Assert.Throws<EvitaInvalidUsageException>(DeriveSchema<IReadOnlyProduct>);
        Assert.Contains("read-only", exception.Message);
    }

    // ---------------------------------------------------------------------
    // regression tests for schema-mutation guards
    //
    // These guards were ported from Java with inverted operands, which made the mutations silently
    // skip the change they were asked to perform. The typed layer surfaced it: a contract declaring
    // AllowedLocales produced a schema with no locales at all.
    // ---------------------------------------------------------------------

    [Fact]
    public void ShouldAllowLocaleOnSchemaThatHasNoLocalesYet()
    {
        IEntitySchemaBuilder builder = NewProductSchemaBuilder();

        builder.WithLocale(Czech);

        Assert.Contains(Czech, builder.ToInstance().Locales);
    }

    [Fact]
    public void ShouldAllowAdditionalLocaleWhenExistingOnesAreASubset()
    {
        IEntitySchemaBuilder builder = NewProductSchemaBuilder();
        CultureInfo german = CultureInfo.GetCultureInfo("de");

        builder.WithLocale(Czech);
        builder.WithLocale(Czech, german);

        ISet<CultureInfo> locales = builder.ToInstance().Locales;
        Assert.Contains(Czech, locales);
        Assert.Contains(german, locales);
    }

    [Fact]
    public void ShouldRemoveLocaleThatSchemaActuallyHas()
    {
        // the old guard skipped removal whenever the schema's locales were a subset of the removed set,
        // which includes the everyday case of removing the only locale present
        IEntitySchemaBuilder builder = NewProductSchemaBuilder();
        builder.WithLocale(Czech);

        builder.WithoutLocale(Czech);

        Assert.DoesNotContain(Czech, builder.ToInstance().Locales);
    }

    [Fact]
    public void ShouldAllowEvolutionModeOnSchemaThatHasNoneYet()
    {
        IEntitySchemaBuilder builder = NewEmptyEvolutionSchemaBuilder();

        builder.VerifySchemaButAllow(EvolutionMode.AddingAttributes);

        Assert.Contains(EvolutionMode.AddingAttributes, builder.ToInstance().EvolutionModes);
    }

    [Fact]
    public void ShouldRemoveEvolutionModeThatSchemaActuallyHas()
    {
        IEntitySchemaBuilder builder = NewProductSchemaBuilder();
        builder.VerifySchemaButAllow(EvolutionMode.AddingAttributes);

        builder.VerifySchemaStrictly();

        Assert.DoesNotContain(EvolutionMode.AddingAttributes, builder.ToInstance().EvolutionModes);
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static ISealedEntity BuildProductEntity()
    {
        InitialEntityBuilder builder = new(BuildProductSchema(), 42);
        builder.SetAttribute("name", "Bluetooth speaker");
        builder.SetAttribute("ean", "EAN-123");
        builder.SetAttribute("quantity", 12.5m);
        builder.SetAttribute("tags", new[] { "audio", "portable" });
        builder.SetAttribute("brand", "Acme");
        builder.SetAttribute("title", Czech, "Bluetooth reproduktor");
        return builder.ToInstance();
    }

    private static IEntitySchema BuildProductSchema()
    {
        Dictionary<string, IEntityAttributeSchema> attributes = new()
        {
            ["name"] = EntityAttributeSchema.InternalBuild("name", typeof(string), false),
            ["ean"] = EntityAttributeSchema.InternalBuild("ean", typeof(string), false),
            ["quantity"] = EntityAttributeSchema.InternalBuild("quantity", typeof(decimal), false),
            ["tags"] = EntityAttributeSchema.InternalBuild("tags", typeof(string[]), false),
            ["subtitle"] = EntityAttributeSchema.InternalBuild("subtitle", typeof(string), false),
            ["brand"] = EntityAttributeSchema.InternalBuild("brand", typeof(string), false),
            ["title"] = EntityAttributeSchema.InternalBuild("title", typeof(string), true),
            ["weight"] = EntityAttributeSchema.InternalBuild("weight", typeof(decimal), false),
            ["rating"] = EntityAttributeSchema.InternalBuild("rating", typeof(int), false)
        };

        return EntitySchema.InternalBuild(
            1,
            "Product",
            null,
            null,
            false,
            false,
            false,
            2,
            new HashSet<CultureInfo> { Czech },
            new HashSet<Currency>(),
            attributes,
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );
    }

    private static InternalEntitySchemaBuilder NewProductSchemaBuilder(bool withGeneratedPrimaryKey = false)
    {
        CatalogSchema catalogSchema = CatalogSchema.InternalBuild(
            "testCatalog",
            EvitaDB.Client.Utils.NamingConventionHelper.Generate("testCatalog"),
            new HashSet<CatalogEvolutionMode> { CatalogEvolutionMode.AddingEntityTypes },
            new EmptyEntitySchemaProvider()
        );

        EntitySchema baseSchema = withGeneratedPrimaryKey
            ? EntitySchema.InternalBuild(
                1, "Product", null, null, true, false, false, 2,
                new HashSet<CultureInfo>(),
                new HashSet<Currency>(),
                new Dictionary<string, IEntityAttributeSchema>(),
                new Dictionary<string, IAssociatedDataSchema>(),
                new Dictionary<string, IReferenceSchema>(),
                new HashSet<EvolutionMode>(),
                new Dictionary<string, SortableAttributeCompoundSchema>()
            )
            : EntitySchema.InternalBuild("Product");

        return new InternalEntitySchemaBuilder(catalogSchema, baseSchema);
    }

    private static IEntitySchemaBuilder NewEmptyEvolutionSchemaBuilder()
    {
        CatalogSchema catalogSchema = CatalogSchema.InternalBuild(
            "testCatalog",
            EvitaDB.Client.Utils.NamingConventionHelper.Generate("testCatalog"),
            new HashSet<CatalogEvolutionMode> { CatalogEvolutionMode.AddingEntityTypes },
            new EmptyEntitySchemaProvider()
        );

        EntitySchema bare = EntitySchema.InternalBuild(
            1, "Product", null, null, false, false, false, 2,
            new HashSet<CultureInfo>(),
            new HashSet<Currency>(),
            new Dictionary<string, IEntityAttributeSchema>(),
            new Dictionary<string, IAssociatedDataSchema>(),
            new Dictionary<string, IReferenceSchema>(),
            new HashSet<EvolutionMode>(),
            new Dictionary<string, SortableAttributeCompoundSchema>()
        );

        return new InternalEntitySchemaBuilder(catalogSchema, bare);
    }

    private static IEntitySchema DeriveSchema<T>()
    {
        TypedContractDescriptor descriptor = TypedContractAnalyzer.Describe<T>();
        IEntitySchemaBuilder builder = NewProductSchemaBuilder();
        ModelSchemaAnalyzer.Apply(descriptor, builder);
        return builder.ToInstance();
    }

    private sealed class EmptyEntitySchemaProvider : IEntitySchemaProvider
    {
        public IEnumerable<IEntitySchema?> GetEntitySchemas() => [];

        public IEntitySchema? GetEntitySchema(string name) => null;
    }
}
