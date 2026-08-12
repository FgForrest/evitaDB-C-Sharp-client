using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Converters.Models.Schema;

public static class CatalogSchemaConverter
{
    public static GrpcCatalogSchema Convert(CatalogSchema catalogSchema)
    {
        return new GrpcCatalogSchema
        {
            Name = catalogSchema.Name,
            Version = catalogSchema.Version,
            Attributes = { ToGrpcGlobalAttributeSchemas(catalogSchema.GetAttributes()) },
            Description = catalogSchema.Description
        };
    }

    public static CatalogSchema Convert(
        GrpcCatalogSchema catalogSchema,
        IEntitySchemaProvider entitySchemaProvider
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
            entitySchemaProvider
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
        GrpcGlobalAttributeSchema grpcAttributeSchema = new GrpcGlobalAttributeSchema
        {
            Name = attributeSchema.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Unique = EvitaEnumConverter.ToGrpcAttributeUniquenessType(attributeSchema.UniquenessType),
            UniqueGlobally =
                EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(attributeSchema.GlobalUniquenessType),
            Filterable = attributeSchema.Filterable(),
            Sortable = attributeSchema.Sortable(),
#pragma warning restore CS0612
            Localized = attributeSchema.Localized(),
            Nullable = attributeSchema.Nullable(),
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(attributeSchema.Type),
            IndexedDecimalPlaces = attributeSchema.IndexedDecimalPlaces,
            DefaultValue = attributeSchema.DefaultValue is null
                ? null
                : EvitaDataTypesConverter.ToGrpcEvitaValue(attributeSchema.DefaultValue),
            Description = attributeSchema.Description,
            DeprecationNotice = attributeSchema.DeprecationNotice
        };

        if (attributeSchema.UniquenessType != AttributeUniquenessType.NotUnique)
        {
            grpcAttributeSchema.UniqueInScopes.Add(new GrpcScopedAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcAttributeUniquenessType(attributeSchema.UniquenessType)
            });
        }

        if (attributeSchema.GlobalUniquenessType != GlobalAttributeUniquenessType.NotUnique)
        {
            grpcAttributeSchema.UniqueGloballyInScopes.Add(new GrpcScopedGlobalAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType =
                    EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(attributeSchema.GlobalUniquenessType)
            });
        }

        if (attributeSchema.Filterable())
        {
            grpcAttributeSchema.FilterableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        if (attributeSchema.Sortable())
        {
            grpcAttributeSchema.SortableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcAttributeSchema;
    }

    private static IGlobalAttributeSchema ToGlobalAttributeSchema(
        GrpcGlobalAttributeSchema attributeSchema)
    {
        return AttributeSchema.InternalBuild(
            attributeSchema.Name,
            attributeSchema.Description,
            attributeSchema.DeprecationNotice,
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
            EvitaEnumConverter.ToAttributeUniquenessType(attributeSchema.UniqueInScopes, attributeSchema.Unique),
            EvitaEnumConverter.ToGlobalAttributeUniquenessType(attributeSchema.UniqueGloballyInScopes,
                attributeSchema.UniqueGlobally),
            EvitaEnumConverter.ToScopedBooleanFlag(attributeSchema.FilterableInScopes, attributeSchema.Filterable),
            EvitaEnumConverter.ToScopedBooleanFlag(attributeSchema.SortableInScopes, attributeSchema.Sortable),
#pragma warning restore CS0612
            attributeSchema.Localized,
            attributeSchema.Nullable,
            attributeSchema.Representative,
            EvitaDataTypesConverter.ToEvitaDataType(attributeSchema.Type),
            attributeSchema.DefaultValue is not null
                ? EvitaDataTypesConverter.ToEvitaValue(attributeSchema.DefaultValue)
                : null,
            attributeSchema.IndexedDecimalPlaces
        );
    }
}
