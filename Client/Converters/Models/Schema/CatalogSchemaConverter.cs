﻿using Client.Converters.DataTypes;
using Client.Models.Schemas.Dtos;
using Client.Utils;
using EvitaDB;

namespace Client.Converters.Models.Schema;

public class CatalogSchemaConverter
{
    public static GrpcCatalogSchema Convert(CatalogSchema catalogSchema)
    {
        return new GrpcCatalogSchema
        {
            Name = catalogSchema.Name,
            Version = catalogSchema.Version,
            Attributes = {ToGrpcGlobalAttributeSchemas(catalogSchema.Attributes)},
            Description = catalogSchema.Description
        };
    }

    public static CatalogSchema Convert(
        Func<string, EntitySchema> entitySchemaSupplier,
        GrpcCatalogSchema catalogSchema
    )
    {
        return CatalogSchema.InternalBuild(
            catalogSchema.Version,
            catalogSchema.Name,
            NamingConventionHelper.Generate(catalogSchema.Name),
            catalogSchema.Description,
            catalogSchema.Attributes.ToDictionary(
                it => it.Key,
                it => ToGlobalAttributeSchema(it.Value)
            ),
            entitySchemaSupplier
        );
    }

    private static IDictionary<string, GrpcGlobalAttributeSchema>
        ToGrpcGlobalAttributeSchemas(IDictionary<string, GlobalAttributeSchema> originalAttributeSchemas)
    {
        Dictionary<string, GrpcGlobalAttributeSchema>
            attributeSchemas = new Dictionary<string, GrpcGlobalAttributeSchema>(originalAttributeSchemas.Count);
        foreach (KeyValuePair<string, GlobalAttributeSchema> entry in originalAttributeSchemas)
        {
            attributeSchemas.Add(entry.Key, ToGrpcGlobalAttributeSchema(entry.Value));
        }

        return attributeSchemas;
    }

    private static GrpcGlobalAttributeSchema ToGrpcGlobalAttributeSchema(
        GlobalAttributeSchema attributeSchema)
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
                : EvitaDataTypesConverter.ToGrpcEvitaValue(attributeSchema.DefaultValue, null),
            Description = attributeSchema.Description,
            DeprecationNotice = attributeSchema.DeprecationNotice
        };
    }

    private static GlobalAttributeSchema ToGlobalAttributeSchema(
        GrpcGlobalAttributeSchema attributeSchema)
    {
        return GlobalAttributeSchema.InternalBuild(
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