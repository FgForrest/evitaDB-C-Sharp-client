using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Converters.Models.Schema;

public static class CatalogSchemaConverter
{
    public static GrpcCatalogSchema Convert(ICatalogSchema catalogSchema)
    {
        return new GrpcCatalogSchema
        {
            Name = catalogSchema.Name,
            Version = catalogSchema.Version,
            Attributes = {ToGrpcGlobalAttributeSchemas(catalogSchema.GetAttributes())},
            Description = catalogSchema.Description
        };
    }

    public static CatalogSchema Convert(
        Func<string, IEntitySchema> entitySchemaSupplier,
        GrpcCatalogSchema catalogSchema
    )
    {
        return CatalogSchema.InternalBuild(
            catalogSchema.Version,
            catalogSchema.Name,
            NamingConventionHelper.Generate(catalogSchema.Name),
            catalogSchema.Description,
            catalogSchema.CatalogEvolutionMode.Select(EvitaEnumConverter.ToCatalogEvolutionMode).ToHashSet(),
            catalogSchema.Attributes.ToDictionary(
                it => it.Key,
                it => ToGlobalAttributeSchema(it.Value)
            ),
            entitySchemaSupplier
        );
    }

    private static IDictionary<string, GrpcGlobalAttributeSchema>
        ToGrpcGlobalAttributeSchemas(IDictionary<string, IGlobalAttributeSchema> originalAttributeSchemas)
    {
        Dictionary<string, GrpcGlobalAttributeSchema>
            attributeSchemas = new Dictionary<string, GrpcGlobalAttributeSchema>(originalAttributeSchemas.Count);
        foreach (KeyValuePair<string, IGlobalAttributeSchema> entry in originalAttributeSchemas)
        {
            attributeSchemas.Add(entry.Key, ToGrpcGlobalAttributeSchema(entry.Value));
        }

        return attributeSchemas;
    }

    private static GrpcGlobalAttributeSchema ToGrpcGlobalAttributeSchema(
        IGlobalAttributeSchema attributeSchema)
    {
        return new GrpcGlobalAttributeSchema
        {
            Name = attributeSchema.Name,
            Unique = attributeSchema.Unique,
            UniqueGlobally = attributeSchema.UniqueGlobally,
            Filterable = attributeSchema.Filterable,
            Sortable = attributeSchema.Sortable,
            Localized = attributeSchema.Localized,
            Nullable = attributeSchema.Nullable,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(attributeSchema.Type),
            IndexedDecimalPlaces = attributeSchema.IndexedDecimalPlaces,
            DefaultValue = attributeSchema.DefaultValue is null
                ? null
                : EvitaDataTypesConverter.ToGrpcEvitaValue(attributeSchema.DefaultValue),
            Description = attributeSchema.Description,
            DeprecationNotice = attributeSchema.DeprecationNotice
        };
    }

    private static IGlobalAttributeSchema ToGlobalAttributeSchema(
        GrpcGlobalAttributeSchema attributeSchema)
    {
        return AttributeSchema.InternalBuild(
            attributeSchema.Name,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
            attributeSchema.Unique,
            attributeSchema.UniqueGlobally,
            attributeSchema.Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            EvitaDataTypesConverter.ToEvitaDataType(attributeSchema.Type),
            attributeSchema.DefaultValue is not null
                ? EvitaDataTypesConverter.ToEvitaValue(attributeSchema.DefaultValue)
                : null,
            attributeSchema.IndexedDecimalPlaces
        );
    }
}