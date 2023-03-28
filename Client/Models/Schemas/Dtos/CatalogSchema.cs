using Client.Exceptions;
using Client.Utils;

namespace Client.Models.Schemas.Dtos;

public class CatalogSchema
{
    public int Version { get; }
    public string Name { get; }
    public IDictionary<NamingConvention, string> NameVariants { get; }
    public string? Description { get; }
    public IDictionary<string, GlobalAttributeSchema> Attributes { get; }
    private IDictionary<string, GlobalAttributeSchema[]> AttributeNameIndex { get; }
    public Func<string, EntitySchema?> EntitySchemaAccessor { get; }

    public static CatalogSchema InternalBuild(
        string name, IDictionary<NamingConvention, string> nameVariants,
        Func<string, EntitySchema> entitySchemaAccessor
    )
    {
        return new CatalogSchema(
            1, name, nameVariants, null, new Dictionary<string, GlobalAttributeSchema>(), entitySchemaAccessor
        );
    }

    public static CatalogSchema InternalBuild(
        int version,
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        IDictionary<string, GlobalAttributeSchema> attributes,
        Func<string, EntitySchema?> entitySchemaAccessor
    )
    {
        return new CatalogSchema(
            version, name, nameVariants, description,
            attributes,
            entitySchemaAccessor
        );
    }

    public static CatalogSchema InternalBuildWithUpdatedVersion(
        CatalogSchema baseSchema
    )
    {
        return new CatalogSchema(
            baseSchema.Version + 1,
            baseSchema.Name,
            baseSchema.NameVariants,
            baseSchema.Description,
            baseSchema.Attributes,
            baseSchema.GetEntitySchema
        );
    }

    private static GlobalAttributeSchema ToAttributeSchema(GlobalAttributeSchema attributeSchema)
    {
        return new GlobalAttributeSchema(
            attributeSchema.Name, attributeSchema.NameVariants, attributeSchema.Description,
            attributeSchema.DeprecationNotice, attributeSchema.Unique, attributeSchema.UniqueGlobally,
            attributeSchema.Filterable, attributeSchema.Sortable, attributeSchema.Localized, attributeSchema.Nullable,
            attributeSchema.Type, attributeSchema.DefaultValue, attributeSchema.IndexedDecimalPlaces
        );
    }

    private CatalogSchema(
        int version,
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        IDictionary<string, GlobalAttributeSchema> attributes,
        Func<string, EntitySchema?> entitySchemaAccessor
    )
    {
        Version = version;
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        Attributes = attributes.ToDictionary(
            x => x.Key,
            x => ToAttributeSchema(x.Value)
        );
        AttributeNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            Attributes.Values, x => x.NameVariants
        );
        EntitySchemaAccessor = entitySchemaAccessor;
    }

    public EntitySchema? GetEntitySchema(string entityType) => EntitySchemaAccessor.Invoke(entityType);

    public string GetNameVariant(NamingConvention namingConvention) => NameVariants[namingConvention];

    public GlobalAttributeSchema? GetAttribute(string attributeName) => Attributes[attributeName];

    public GlobalAttributeSchema? GetAttribute(string attributeName, NamingConvention namingConvention)
    {
        var nameVariants = AttributeNameIndex[attributeName];
        return nameVariants.FirstOrDefault(x => x.NameVariants[namingConvention] == attributeName);
    }

    public EntitySchema GetEntitySchemaOrThrowException(string entityType) =>
        GetEntitySchema(entityType) ??
        throw new EvitaInvalidUsageException("Schema for entity with name `" + entityType + "` was not found!");


    public bool DiffersFrom(CatalogSchema otherCatalogSchema)
    {
        if (this != otherCatalogSchema) return false;
        return !(
            Version == otherCatalogSchema.Version &&
            Name == otherCatalogSchema.Name &&
            Attributes.SequenceEqual(otherCatalogSchema.Attributes)
        );
    }
}