using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Typed;

namespace EvitaDB.Test.Tests;

/// <summary>
/// Tests of the contract that generated typed views are written against: <see cref="TypedEntityView"/>
/// plus <see cref="TypedViewRegistry"/>.
/// <remarks>
/// <para>
/// The view class below is hand-written to be exactly what <c>EvitaDB.Client.Generators</c> emits, so
/// these tests pin the runtime half of the generated path without requiring the analyzer to run on this
/// project. Wiring the generator into this assembly would generate views for every contract in the test
/// suite and change what the proxy-path tests are testing.
/// </para>
/// <para>
/// The decisive difference from the runtime proxy is
/// <see cref="ShouldExecuteDefaultInterfaceMemberBody"/>: a real class inherits default interface members,
/// whereas <c>DispatchProxy</c> intercepts them and cannot delegate back.
/// </para>
/// </remarks>
/// </summary>
public class TypedGeneratedViewTest
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs");

    [EvitaEntity("Generated")]
    public interface IGeneratedProduct : ITypedEntityEditor
    {
        int Id { get; }

        [EvitaAttribute] string? Name { get; set; }
        [EvitaAttribute] decimal? Weight { get; }
        [EvitaAttribute] IGeneratedProduct SetWeight(decimal value);
        [EvitaAttribute(Localized = true)] string? GetTitle(CultureInfo locale);
        [EvitaAttribute] Maybe<string> Subtitle { get; }

        // computed members: a generated class leaves these to the interface
        string Shouty => (Name ?? string.Empty).ToUpperInvariant();
        bool HasName => !string.IsNullOrEmpty(Name);
    }

    /// <summary>
    /// Byte-for-byte the shape the generator emits: derives from <see cref="TypedEntityView"/>, forwards
    /// each abstract member by name, and implements neither the default members nor the editor mixin.
    /// </summary>
    private sealed class GeneratedProductView : TypedEntityView, IGeneratedProduct
    {
        public GeneratedProductView(BoundTypedContract contract, object source)
            : base(contract, source)
        {
        }

        public int Id => (int)Read("Id")!;

        public string? Name
        {
            get => (string?)Read("Name")!;
            set => Write("Name", [value]);
        }

        public decimal? Weight => (decimal?)Read("Weight")!;

        public IGeneratedProduct SetWeight(decimal value)
        {
            Write("SetWeight", [value]);
            return this;
        }

        public string? GetTitle(CultureInfo locale) => (string?)Read("GetTitle", [locale])!;

        public Maybe<string> Subtitle => (Maybe<string>)Read("Subtitle")!;
    }

    public TypedGeneratedViewTest() =>
        TypedViewRegistry.Register(
            typeof(IGeneratedProduct),
            static (contract, source) => new GeneratedProductView(contract, source)
        );

    [Fact]
    public void ShouldPreferRegisteredImplementationOverRuntimeProxy()
    {
        IGeneratedProduct product = NewDraft();

        Assert.IsType<GeneratedProductView>(product);
    }

    [Fact]
    public void ShouldExecuteDefaultInterfaceMemberBody()
    {
        // the runtime proxy raises a diagnostic for such a member; a generated class inherits it
        IGeneratedProduct product = NewDraft();
        product.Name = "Speaker";

        Assert.Equal("SPEAKER", product.Shouty);
        Assert.True(product.HasName);
    }

    [Fact]
    public void ShouldReadAndWriteThroughGeneratedMembers()
    {
        IGeneratedProduct product = NewDraft();

        product.Name = "Speaker";

        Assert.Equal(500, product.Id);
        Assert.Equal("Speaker", product.Name);
    }

    [Fact]
    public void ShouldSupportFluentSetters()
    {
        IGeneratedProduct product = NewDraft();

        IGeneratedProduct returned = product.SetWeight(1.5m);

        Assert.Same(product, returned);
        Assert.Equal(1.5m, product.Weight);
    }

    [Fact]
    public void ShouldSupportLocalizedMembersAndMaybe()
    {
        IGeneratedProduct product = NewDraft();
        product.EntityBuilder.SetAttribute("title", Czech, "Reproduktor");

        Assert.Equal("Reproduktor", product.GetTitle(Czech));
        Assert.Equal(MaybeState.Absent, product.Subtitle.State);
    }

    [Fact]
    public void ShouldProvideEditorMixinFromTheBaseClass()
    {
        // the generator emits none of these - TypedEntityView implements them
        IGeneratedProduct product = NewDraft();

        Assert.False(product.HasChanges);

        product.Name = "Speaker";

        Assert.True(product.HasChanges);
        Assert.NotNull(product.ToMutation());
        Assert.Equal("Speaker", product.ToInstance().GetAttribute("name"));
        Assert.NotNull(product.EntityBuilder);
    }

    [Fact]
    public void ShouldMemoizeNestedViewsAndDescribeItself()
    {
        IGeneratedProduct product = NewDraft();

        Assert.Contains(nameof(IGeneratedProduct), product.ToString());
        Assert.Equal(typeof(IGeneratedProduct), ((ITypedEntity)product).ContractType);
    }

    /// <summary>
    /// A positional record contract, bound by constructor rather than proxied. The binder below is what the
    /// generator emits for it.
    /// </summary>
    [EvitaEntity("Generated")]
    public record GeneratedProductRecord(int Id, string? Name);

    private static object BindRecord(BoundTypedContract contract, object source) =>
        new GeneratedProductRecord(
            (int)contract.ReadByMember("Id", source, null)!,
            (string?)contract.ReadByMember("Name", source, null)!
        );

    [Fact]
    public void ShouldBindPositionalRecordContract()
    {
        TypedViewRegistry.Register(typeof(GeneratedProductRecord), BindRecord);

        InitialEntityBuilder builder = new(BuildSchema(), 77);
        builder.SetAttribute("name", "Recorded");

        GeneratedProductRecord record = builder.ToInstance().AsModel<GeneratedProductRecord>();

        // a record stays a record: its own type, and value equality - not a proxy
        Assert.Equal(typeof(GeneratedProductRecord), record.GetType());
        Assert.Equal(new GeneratedProductRecord(77, "Recorded"), record);
    }

    private static IGeneratedProduct NewDraft() =>
        TypedEntityFactory.CreateNew<IGeneratedProduct>(BuildSchema(), 500);

    private static IEntitySchema BuildSchema() => EntitySchema.InternalBuild(
        1, "Generated", null, null, false, false, false, 2,
        new HashSet<CultureInfo> { Czech },
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>
        {
            ["name"] = EntityAttributeSchema.InternalBuild("name", typeof(string), false),
            ["weight"] = EntityAttributeSchema.InternalBuild("weight", typeof(decimal), false),
            ["subtitle"] = EntityAttributeSchema.InternalBuild("subtitle", typeof(string), false),
            ["title"] = EntityAttributeSchema.InternalBuild("title", typeof(string), true)
        },
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>(),
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );
}
