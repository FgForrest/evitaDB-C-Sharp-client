using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Models.Schemas.Builders;

/// <summary>
/// Internal <see cref="IAttributeSchemaBuilder"/> builder used solely from within <see cref="InternalEntitySchemaBuilder"/>.
/// </summary>
public class AttributeSchemaBuilder : AbstractAttributeSchemaBuilder<IAttributeSchemaBuilder, IAttributeSchema>,
    IAttributeSchemaBuilder
{
    private List<IEntitySchemaMutation> Mutations { get; } = new();

    internal AttributeSchemaBuilder(
        IEntitySchema entitySchema,
        IAttributeSchema existingSchema
    ) : base(null, entitySchema, existingSchema)
    {
    }

    internal AttributeSchemaBuilder(
        IEntitySchema entitySchema,
        string name,
        Type ofType
    ) : base(null, entitySchema, AttributeSchema.InternalBuild(name, ofType, false))
    {
        Mutations.Add(
            new CreateAttributeSchemaMutation(
                BaseSchema.Name,
                BaseSchema.Description,
                BaseSchema.DeprecationNotice,
                BaseSchema.UniquenessType,
                BaseSchema.Filterable,
                BaseSchema.Sortable,
                BaseSchema.Localized,
                BaseSchema.Nullable,
                false,
                BaseSchema.Type,
                BaseSchema.DefaultValue,
                BaseSchema.IndexedDecimalPlaces
            )
        );
    }

    public override bool AddMutations(IAttributeSchemaMutation mutation)
    {
        return SchemaBuilderHelper.AddMutations(
            CatalogSchema!, EntitySchema!, Mutations, (IEntitySchemaMutation) mutation
        );
    }

    public ICollection<IEntitySchemaMutation> ToMutation()
    {
        return Mutations;
    }

    public override ICollection<IAttributeSchemaMutation> ToAttributeMutation()
    {
        return Mutations
            .Select(it => (IAttributeSchemaMutation) it)
            .ToList();
    }

    public ICollection<IReferenceSchemaMutation> ToReferenceMutation(string referenceName)
    {
        return new List<IReferenceSchemaMutation>(Mutations
            .Select(it =>
                new ModifyReferenceAttributeSchemaMutation(referenceName, (it as IReferenceSchemaMutation)!)));
    }

    public override bool UniqueWithinLocale => base.ToInstance().UniqueWithinLocale;
    public override AttributeUniquenessType UniquenessType => base.ToInstance().UniquenessType;

    protected override Type GetAttributeSchemaType()
    {
        return typeof(IAttributeSchema);
    }
}
