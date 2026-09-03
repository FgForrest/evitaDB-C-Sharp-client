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
/// Tests of deep upsert: writing an entity together with the entities it references.
/// <remarks>
/// The upsert itself is injected (<see cref="ITypedEntityEditor.UpsertDeeplyWith"/>) rather than taken
/// from a session, so ordering, generated-key backfill and cycle detection are all testable without a
/// server. The fake below stands in for evitaDB by assigning a key to any entity that arrives without one.
/// </remarks>
/// </summary>
public class TypedModelDeepUpsertTest
{
    [EvitaEntity("Brand")]
    public interface IBrandEditor : ITypedEntityEditor
    {
        [EvitaAttribute] string? Name { get; set; }
    }

    [EvitaEntity("Referencing")]
    public interface IReferencingEditor : ITypedEntityEditor
    {
        [EvitaReference("brand")] int? BrandId { get; set; }
        [EvitaReference("backupBrand")] int? BackupBrandId { get; set; }
    }

    /// <summary>Records what was sent and assigns keys the way the server would.</summary>
    private sealed class FakeServer
    {
        private int _nextGeneratedKey = 1000;

        internal List<string> Sent { get; } = [];

        internal EntityReference Upsert(IEntityBuilder builder)
        {
            Entity instance = builder.ToInstance();
            int key = instance.PrimaryKey ?? _nextGeneratedKey++;
            Sent.Add($"{instance.Type}:{key}");
            return new EntityReference(instance.Type, key);
        }
    }

    [Fact]
    public void ShouldUpsertChildrenBeforeParents()
    {
        FakeServer server = new();
        (IReferencingEditor parent, IBrandEditor brand) = NewGraph();
        brand.Name = "Acme";
        parent.AttachReferenced("brand", brand);

        parent.UpsertDeeplyWith(server.Upsert);

        Assert.Equal(2, server.Sent.Count);
        Assert.StartsWith("Brand:", server.Sent[0]);
        Assert.Equal("Referencing:900", server.Sent[1]);
    }

    [Fact]
    public void ShouldBackfillServerGeneratedKeyIntoTheParentReference()
    {
        // this is the whole point: the child had no key of its own, so the parent could not have set the
        // reference in advance
        FakeServer server = new();
        (IReferencingEditor parent, IBrandEditor brand) = NewGraph();
        brand.Name = "Acme";
        parent.AttachReferenced("brand", brand);

        IReadOnlyList<EntityReference> upserted = parent.UpsertDeeplyWith(server.Upsert);

        Assert.Equal(1000, upserted[0].PrimaryKey);
        Assert.Equal(1000, parent.BrandId);
    }

    [Fact]
    public void ShouldReturnEverythingUpsertedWithRootLast()
    {
        FakeServer server = new();
        (IReferencingEditor parent, IBrandEditor brand) = NewGraph();
        brand.Name = "Acme";
        parent.AttachReferenced("brand", brand);

        IReadOnlyList<EntityReference> upserted = parent.UpsertDeeplyWith(server.Upsert);

        Assert.Equal("Brand", upserted[0].Type);
        Assert.Equal("Referencing", upserted[^1].Type);
        Assert.Equal(900, upserted[^1].PrimaryKey);
    }

    [Fact]
    public void ShouldKeepAnExplicitlyAssignedChildKey()
    {
        FakeServer server = new();
        (IReferencingEditor parent, _) = NewGraph();
        IBrandEditor brand = TypedEntityFactory.CreateNew<IBrandEditor>(BrandSchema(), 55);
        parent.AttachReferenced("brand", brand);

        IReadOnlyList<EntityReference> upserted = parent.UpsertDeeplyWith(server.Upsert);

        Assert.Equal(55, upserted[0].PrimaryKey);
        Assert.Equal(55, parent.BrandId);
    }

    [Fact]
    public void ShouldNotSendAnUnchangedExistingEntity()
    {
        // a new entity is always sent - creating it is the change - but an existing one opened for write
        // and left alone has no mutation, so there is nothing to send for it
        FakeServer server = new();
        (IReferencingEditor parent, _) = NewGraph();

        ISealedEntity existingBrand = new InitialEntityBuilder(BrandSchema(), 42).ToInstance();
        IBrandEditor untouched = existingBrand.OpenForWrite<IBrandEditor>();
        parent.AttachReferenced("brand", untouched);
        parent.BrandId = 1;

        parent.UpsertDeeplyWith(server.Upsert);

        Assert.DoesNotContain("Brand:42", server.Sent);
        Assert.Equal(42, parent.BrandId);
    }

    [Fact]
    public void ShouldUpsertAnEditorAttachedTwiceOnlyOnce()
    {
        // the same child under two references: it must be sent once and both references must get its key.
        // Attaching two *different* children under the SAME reference name is not the case under test -
        // InitialEntityBuilder cannot set one reference twice (a known driver limitation).
        FakeServer server = new();
        IBrandEditor shared = TypedEntityFactory.CreateNew<IBrandEditor>(BrandSchema(), null);
        shared.Name = "Acme";

        IReferencingEditor parent = TypedEntityFactory.CreateNew<IReferencingEditor>(ReferencingSchema(), 900);
        parent.AttachReferenced("brand", shared);
        parent.AttachReferenced("backupBrand", shared);

        parent.UpsertDeeplyWith(server.Upsert);

        Assert.Single(server.Sent, it => it.StartsWith("Brand:", StringComparison.Ordinal));
        Assert.Equal(1000, parent.BrandId);
        Assert.Equal(1000, parent.BackupBrandId);
    }

    [Fact]
    public void ShouldDiagnoseCyclicAttachments()
    {
        FakeServer server = new();
        IReferencingEditor first = TypedEntityFactory.CreateNew<IReferencingEditor>(ReferencingSchema(), 910);
        IReferencingEditor second = TypedEntityFactory.CreateNew<IReferencingEditor>(ReferencingSchema(), 911);
        first.AttachReferenced("brand", second);
        second.AttachReferenced("brand", first);

        EvitaInvalidUsageException exception = Assert.Throws<EvitaInvalidUsageException>(
            () => { first.UpsertDeeplyWith(server.Upsert); }
        );
        Assert.Contains("cycle", exception.Message);
    }

    // ---------------------------------------------------------------------
    // fixtures
    // ---------------------------------------------------------------------

    private static (IReferencingEditor Parent, IBrandEditor Brand) NewGraph() =>
        (TypedEntityFactory.CreateNew<IReferencingEditor>(ReferencingSchema(), 900),
            TypedEntityFactory.CreateNew<IBrandEditor>(BrandSchema(), null));

    private static IEntitySchema BrandSchema() => EntitySchema.InternalBuild(
        1, "Brand", null, null, true, false, false, 2,
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

    private static IEntitySchema ReferencingSchema() => EntitySchema.InternalBuild(
        1, "Referencing", null, null, false, false, false, 2,
        new HashSet<CultureInfo>(),
        new HashSet<Currency>(),
        new Dictionary<string, IEntityAttributeSchema>(),
        new Dictionary<string, IAssociatedDataSchema>(),
        new Dictionary<string, IReferenceSchema>
        {
            ["brand"] = ReferenceSchema.InternalBuild(
                "brand", "Brand", true, Cardinality.ZeroOrOne, null, false, true, false),
            ["backupBrand"] = ReferenceSchema.InternalBuild(
                "backupBrand", "Brand", true, Cardinality.ZeroOrOne, null, false, true, false)
        },
        new HashSet<EvolutionMode>(Enum.GetValues<EvolutionMode>()),
        new Dictionary<string, SortableAttributeCompoundSchema>()
    );
}
