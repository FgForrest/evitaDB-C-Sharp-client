using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Models.Schemas.Builders;

public class GlobalAttributeSchemaBuilder :
    AbstractAttributeSchemaBuilder<IGlobalAttributeSchemaBuilder, IGlobalAttributeSchema>, IGlobalAttributeSchemaBuilder
{
    private new ICatalogSchema? CatalogSchema { get; }
    private List<ILocalCatalogSchemaMutation> Mutations { get; } = new();

    internal GlobalAttributeSchemaBuilder(ICatalogSchema? catalogSchema, IEntitySchema? entitySchema,
        IGlobalAttributeSchema existingSchema) : base(catalogSchema, entitySchema, existingSchema)
    {
        CatalogSchema = catalogSchema;
    }

    internal GlobalAttributeSchemaBuilder(ICatalogSchema? catalogSchema, IGlobalAttributeSchema existingSchema)
        : base(catalogSchema, null, existingSchema)
    {
        CatalogSchema = catalogSchema;
    }

    internal GlobalAttributeSchemaBuilder(
        ICatalogSchema catalogSchema,
        string name,
        Type ofType
    ) : base(catalogSchema, null, GlobalAttributeSchema.InternalBuild(name, ofType, false))
    {
        CatalogSchema = catalogSchema;
        Mutations.Add(
            new CreateGlobalAttributeSchemaMutation(
                BaseSchema.Name,
                BaseSchema.Description,
                BaseSchema.DeprecationNotice,
                BaseSchema.Unique,
                BaseSchema.UniqueGlobally,
                BaseSchema.Filterable,
                BaseSchema.Sortable,
                BaseSchema.Localized,
                BaseSchema.Nullable,
                BaseSchema.Type,
                BaseSchema.DefaultValue,
                BaseSchema.IndexedDecimalPlaces
            )
        );
    }

    public override bool AddMutations(IAttributeSchemaMutation mutation)
    {
        return SchemaBuilderHelper.AddMutations(
            CatalogSchema!,
            Mutations,
            (ILocalCatalogSchemaMutation) mutation
        );
    }

    public override ICollection<IAttributeSchemaMutation> ToAttributeMutation()
    {
        return Mutations.Select(it => (IAttributeSchemaMutation) it).ToList();
    }

    public IGlobalAttributeSchemaBuilder UniqueGlobally()
    {
        Mutations.Add(
            new SetAttributeSchemaGloballyUniqueMutation(
                ToInstance().Name,
                true
            )
        );
        return this;
    }

    public IGlobalAttributeSchemaBuilder UniqueGlobally(Func<bool> decider)
    {
        UpdatedSchemaDirty = AddMutations(
            new SetAttributeSchemaGloballyUniqueMutation(
                ToInstance().Name,
                decider.Invoke()
            )
        );
        return this;
    }

    public ICollection<ILocalCatalogSchemaMutation> ToMutation()
    {
        return Mutations;
    }

    public new IGlobalAttributeSchema ToInstance()
    {
        return base.ToInstance();
    }
}