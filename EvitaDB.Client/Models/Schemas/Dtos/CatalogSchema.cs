using System.Collections.Immutable;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Dtos;

public class CatalogSchema : ICatalogSchema
{
    public int Version { get; }
    public string Name { get; }
    public IDictionary<NamingConvention, string?> NameVariants { get; }
    public string? Description { get; }
    public IDictionary<string, IGlobalAttributeSchema> Attributes { get; }
    private IDictionary<string, IGlobalAttributeSchema[]> AttributeNameIndex { get; }
    public Func<string, IEntitySchema?> EntitySchemaAccessor { get; }
    
    public ISet<CatalogEvolutionMode> CatalogEvolutionModes { get; }
    
    internal static CatalogSchema InternalBuild(
        string name, IDictionary<NamingConvention, string?> nameVariants,
        ISet<CatalogEvolutionMode> catalogEvolutionModes,
        Func<string, IEntitySchema> entitySchemaAccessor
    )
    {
        return new CatalogSchema(
            1, name, nameVariants, null, catalogEvolutionModes, new Dictionary<string, IGlobalAttributeSchema>(), entitySchemaAccessor
        );
    }

    internal static CatalogSchema InternalBuild(
        int version,
        string name,
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        ISet<CatalogEvolutionMode> catalogEvolutionModes,
        IDictionary<string, IGlobalAttributeSchema> attributes,
        Func<string, IEntitySchema?> entitySchemaAccessor
    )
    {
        return new CatalogSchema(
            version, name, nameVariants, description, catalogEvolutionModes,
            attributes,
            entitySchemaAccessor
        );
    }

    internal static CatalogSchema InternalBuildWithUpdatedVersion(
        CatalogSchema baseSchema
    )
    {
        return new CatalogSchema(
            baseSchema.Version + 1,
            baseSchema.Name,
            baseSchema.NameVariants,
            baseSchema.Description,
            baseSchema.CatalogEvolutionModes,
            baseSchema.Attributes,
            baseSchema.GetEntitySchema
        );
    }

    private static IGlobalAttributeSchema ToAttributeSchema(IGlobalAttributeSchema attributeSchema)
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
        IDictionary<NamingConvention, string?> nameVariants,
        string? description,
        ISet<CatalogEvolutionMode> catalogEvolutionModes,
        IDictionary<string, IGlobalAttributeSchema> attributes,
        Func<string, IEntitySchema?> entitySchemaAccessor
    )
    {
        Version = version;
        Name = name;
        NameVariants = nameVariants;
        Description = description;
        CatalogEvolutionModes = catalogEvolutionModes.ToImmutableHashSet();
        Attributes = attributes.ToDictionary(
            x => x.Key,
            x => ToAttributeSchema(x.Value)
        );
        AttributeNameIndex = EntitySchema.InternalGenerateNameVariantIndex(
            Attributes.Values, x => x.NameVariants
        );
        EntitySchemaAccessor = entitySchemaAccessor;
    }

    public IEntitySchema? GetEntitySchema(string entityType) => EntitySchemaAccessor.Invoke(entityType);

    public string? GetNameVariant(NamingConvention namingConvention) => NameVariants.TryGetValue(namingConvention, out var nameVariant) ? nameVariant : Name;

    public IDictionary<string, IGlobalAttributeSchema> GetAttributes()
    {
        return Attributes;
    }

    public IGlobalAttributeSchema? GetAttribute(string attributeName) => Attributes.TryGetValue(attributeName, out var attributeSchema) ? attributeSchema : null;
    public IGlobalAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
    {
        return AttributeNameIndex.TryGetValue(name, out var nameVariants)
            ? nameVariants.FirstOrDefault(x => x.NameVariants[namingConvention] == name)
            : null;
    }

    public IGlobalAttributeSchema? GetAttribute(string attributeName, NamingConvention namingConvention)
    {
        var nameVariants = AttributeNameIndex[attributeName];
        return nameVariants.FirstOrDefault(x => x.NameVariants[namingConvention] == attributeName);
    }

    public IEntitySchema GetEntitySchemaOrThrowException(string entityType) =>
        GetEntitySchema(entityType) ??
        throw new EvitaInvalidUsageException("Schema for entity with name `" + entityType + "` was not found!");


    public bool DiffersFrom(ICatalogSchema? otherCatalogSchema)
    {
        if (this != otherCatalogSchema) return false;
        return !(
            Version == otherCatalogSchema.Version &&
            Name == otherCatalogSchema.Name &&
            Attributes.SequenceEqual(otherCatalogSchema.GetAttributes())
        );
    }
}