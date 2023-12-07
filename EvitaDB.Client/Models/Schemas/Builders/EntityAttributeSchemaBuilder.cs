using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class EntityAttributeSchemaBuilder :
    AbstractAttributeSchemaBuilder<IEntityAttributeSchemaBuilder, IEntityAttributeSchema>, IEntityAttributeSchemaBuilder
{
    private IList<IEntitySchemaMutation> Mutations { get; } = new List<IEntitySchemaMutation>();

    internal EntityAttributeSchemaBuilder(IEntitySchema? entitySchema,
        IEntityAttributeSchema existingSchema) : base(null, entitySchema, existingSchema)
    {
    }

    internal EntityAttributeSchemaBuilder(IEntitySchema? entitySchema, string name, Type type) : base(null,
        entitySchema, EntityAttributeSchema.InternalBuild(name, type, false))
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
                BaseSchema.Representative,
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

    public override ICollection<IAttributeSchemaMutation> ToAttributeMutation()
    {
        return Mutations.Select(it => (IAttributeSchemaMutation) it).ToList();
    }

    public IEntityAttributeSchemaBuilder Representative()
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaRepresentativeMutation(
                BaseSchema.Name,
                true
            )
        );
        return this;
    }

    public IEntityAttributeSchemaBuilder Representative(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaRepresentativeMutation(
                BaseSchema.Name,
                decider.Invoke()
            )
        );
        return this;
    }

    protected override Type GetAttributeSchemaType()
    {
        return typeof(IEntityAttributeSchema);
    }


    public ICollection<IEntitySchemaMutation> ToMutation()
    {
        return Mutations;
    }
    
    public override bool UniqueWithinLocale => base.ToInstance().UniqueWithinLocale;
    public override AttributeUniquenessType UniquenessType => base.ToInstance().UniquenessType;
}
