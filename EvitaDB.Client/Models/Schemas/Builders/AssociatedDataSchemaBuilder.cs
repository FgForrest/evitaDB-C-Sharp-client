using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

/// <summary>
///  Internal <see cref="IAssociatedDataSchema"/> builder used solely from within <see cref="IEntitySchemaEditor"/>.
/// </summary>
public class AssociatedDataSchemaBuilder : IAssociatedDataSchemaEditor
{
    private ICatalogSchema CatalogSchema { get; }
    private IEntitySchema EntitySchema { get; }
    private IAssociatedDataSchema BaseSchema { get; }
    private List<IEntitySchemaMutation> Mutations { get; } = new();

    private bool UpdatedSchemaDirty { get; set; }
    private IAssociatedDataSchema? UpdatedSchema { get; set; }

    public string Name => _instance.Name;
    public string? Description => _instance.Description;
    public IDictionary<NamingConvention, string?> NameVariants => _instance.NameVariants;
    public string? DeprecationNotice => _instance.DeprecationNotice;

    public bool Nullable => _instance.Nullable;
    public bool Localized => _instance.Localized;
    public Type Type => _instance.Type;
    
    private readonly IAssociatedDataSchema _instance;

    internal AssociatedDataSchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema entitySchema,
        IAssociatedDataSchema existingSchema
    )
    {
        CatalogSchema = catalogSchema;
        EntitySchema = entitySchema;
        BaseSchema = existingSchema;
        _instance ??= ToInstance();
    }

    internal AssociatedDataSchemaBuilder(
        ICatalogSchema catalogSchema,
        IEntitySchema entitySchema,
        string name,
        Type type
    )
    {
        CatalogSchema = catalogSchema;
        EntitySchema = entitySchema;
        BaseSchema = AssociatedDataSchema.InternalBuild(
            name, type
        );
        Mutations.Add(
            new CreateAssociatedDataSchemaMutation(
                BaseSchema.Name,
                BaseSchema.Description,
                BaseSchema.DeprecationNotice,
                BaseSchema.Type,
                BaseSchema.Localized,
                BaseSchema.Nullable
            )
        );
        _instance ??= ToInstance();
    }

    public string? GetNameVariant(NamingConvention namingConvention)
    {
        return NameVariants.TryGetValue(namingConvention, out string? nameVariant)
            ? nameVariant
            : null;
    }

    public IAssociatedDataSchemaEditor WithDescription(string? description)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifyAssociatedDataSchemaDescriptionMutation(
                Name,
                description
            )
        );
        return this;
    }

    public IAssociatedDataSchemaEditor Deprecated(string deprecationNotice)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifyAssociatedDataSchemaDeprecationNoticeMutation(
                Name,
                deprecationNotice
            )
        );
        return this;
    }

    public IAssociatedDataSchemaEditor NotDeprecatedAnymore()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new ModifyAssociatedDataSchemaDeprecationNoticeMutation(
                Name,
                null
            )
        );
        return this;
    }

    IAssociatedDataSchemaEditor IAssociatedDataSchemaEditor.Localized()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new SetAssociatedDataSchemaLocalizedMutation(
                Name,
                true
            )
        );
        return this;
    }

    IAssociatedDataSchemaEditor IAssociatedDataSchemaEditor.Localized(Func<bool> decider)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new SetAssociatedDataSchemaLocalizedMutation(
                Name,
                decider.Invoke()
            )
        );
        return this;
    }

    IAssociatedDataSchemaEditor IAssociatedDataSchemaEditor.Nullable()
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new SetAssociatedDataSchemaNullableMutation(
                Name,
                true
            )
        );
        return this;
    }

    IAssociatedDataSchemaEditor IAssociatedDataSchemaEditor.Nullable(Func<bool> decider)
    {
        UpdatedSchemaDirty = SchemaBuilderHelper.AddMutations(
            CatalogSchema, EntitySchema, Mutations,
            new SetAssociatedDataSchemaNullableMutation(
                Name,
                decider.Invoke()
            )
        );
        return this;
    }
    
    public IAssociatedDataSchema ToInstance() {
        if (UpdatedSchema == null || UpdatedSchemaDirty) {
            IAssociatedDataSchema? currentSchema = BaseSchema;
            foreach (IEntitySchemaMutation mutation in Mutations) {
                currentSchema = ((IAssociatedDataSchemaMutation) mutation).Mutate(currentSchema);
                if (currentSchema == null) {
                    throw new EvitaInternalError("Attribute unexpectedly removed from inside!");
                }
            }
            UpdatedSchema = currentSchema;
            UpdatedSchemaDirty = false;
        }
        return UpdatedSchema;
    }
    
    public ICollection<IEntitySchemaMutation> ToMutation() {
        return Mutations;
    }

}