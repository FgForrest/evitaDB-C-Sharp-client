using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Converters.Models.Schema;

public static class EntitySchemaConverter
{
    public static GrpcEntitySchema Convert(IEntitySchema entitySchema)
    {
        return new GrpcEntitySchema
        {
            Name = entitySchema.Name,
            Description = entitySchema.Description,
            DeprecationNotice = entitySchema.DeprecationNotice,
            WithGeneratedPrimaryKey = entitySchema.WithGeneratedPrimaryKey,
            WithHierarchy = entitySchema.WithHierarchy,
            WithPrice = entitySchema.WithPrice,
            IndexedPricePlaces = entitySchema.IndexedPricePlaces,
            Locales = {entitySchema.Locales.Select(EvitaDataTypesConverter.ToGrpcLocale)},
            Currencies = {entitySchema.Currencies.Select(EvitaDataTypesConverter.ToGrpcCurrency)},
            Attributes = {ToGrpcAttributeSchemas(entitySchema.Attributes)},
            AssociatedData = {ToGrpcAssociatedDataSchemas(entitySchema.AssociatedData)},
            References = {ToGrpcReferenceSchemas(entitySchema.References)},
            EvolutionMode = {entitySchema.EvolutionModes.Select(EvitaEnumConverter.ToGrpcEvolutionMode)},
            SortableAttributeCompounds =
                {ToGrpcSortableAttributeCompoundSchemas(entitySchema.GetSortableAttributeCompounds())},
            Version = entitySchema.Version,
        };
    }

    public static EntitySchema Convert(GrpcEntitySchema entitySchema)
    {
        return EntitySchema.InternalBuild(
            entitySchema.Version,
            entitySchema.Name,
            NamingConventionHelper.Generate(entitySchema.Name),
            string.IsNullOrEmpty(entitySchema.Description) ? null : entitySchema.Description,
            string.IsNullOrEmpty(entitySchema.DeprecationNotice) ? null : entitySchema.DeprecationNotice,
            entitySchema.WithGeneratedPrimaryKey,
            entitySchema.WithHierarchy,
            entitySchema.WithPrice,
            entitySchema.IndexedPricePlaces,
            entitySchema.Locales
                .Select(EvitaDataTypesConverter.ToLocale)
                .ToHashSet(),
            entitySchema.Currencies
                .Select(EvitaDataTypesConverter.ToCurrency)
                .ToHashSet(),
            entitySchema.Attributes
                .ToDictionary(x => x.Key, x => ToAttributeSchema(x.Value)),
            entitySchema.AssociatedData
                .ToDictionary(x => x.Key, x => ToAssociatedDataSchema(x.Value)),
            entitySchema.References
                .ToDictionary(x => x.Key, x => ToReferenceSchema(x.Value)),
            entitySchema.EvolutionMode
                .Select(EvitaEnumConverter.ToEvolutionMode)
                .ToHashSet(),
            entitySchema.SortableAttributeCompounds.ToDictionary(x => x.Key,
                x => ToSortableAttributeCompoundSchema(x.Value))
        );
    }

    private static IAttributeSchema ToAttributeSchema(GrpcAttributeSchema attributeSchema)
    {
        if (attributeSchema.Global)
        {
            return new GlobalAttributeSchema(
                attributeSchema.Name,
                NamingConventionHelper.Generate(attributeSchema.Name),
                string.IsNullOrEmpty(attributeSchema.Description) ? null : attributeSchema.Description,
                string.IsNullOrEmpty(attributeSchema.DeprecationNotice) ? null : attributeSchema.DeprecationNotice,
                attributeSchema.Unique,
                attributeSchema.UniqueGlobally,
                attributeSchema.Filterable,
                attributeSchema.Sortable,
                attributeSchema.Localized,
                attributeSchema.Nullable,
                EvitaDataTypesConverter.ToEvitaDataType(attributeSchema.Type),
                attributeSchema.DefaultValue is null
                    ? null
                    : EvitaDataTypesConverter.ToEvitaValue(attributeSchema.DefaultValue),
                attributeSchema.IndexedDecimalPlaces
            );
        }

        return AttributeSchema.InternalBuild(
            attributeSchema.Name,
            NamingConventionHelper.Generate(attributeSchema.Name),
            string.IsNullOrEmpty(attributeSchema.Description) ? null : attributeSchema.Description,
            string.IsNullOrEmpty(attributeSchema.DeprecationNotice) ? null : attributeSchema.DeprecationNotice,
            attributeSchema.Unique,
            attributeSchema.Filterable,
            attributeSchema.Sortable,
            attributeSchema.Localized,
            attributeSchema.Nullable,
            EvitaDataTypesConverter.ToEvitaDataType(attributeSchema.Type),
            attributeSchema.DefaultValue is null
                ? null
                : EvitaDataTypesConverter.ToEvitaValue(attributeSchema.DefaultValue),
            attributeSchema.IndexedDecimalPlaces
        );
    }

    private static IAssociatedDataSchema ToAssociatedDataSchema(GrpcAssociatedDataSchema associatedDataSchema)
    {
        Type type = EvitaDataTypesConverter.ToEvitaDataType(associatedDataSchema.Type);
        return AssociatedDataSchema.InternalBuild(
            associatedDataSchema.Name,
            NamingConventionHelper.Generate(associatedDataSchema.Name),
            string.IsNullOrEmpty(associatedDataSchema.Description) ? null : associatedDataSchema.Description,
            string.IsNullOrEmpty(associatedDataSchema.DeprecationNotice)
                ? null
                : associatedDataSchema.DeprecationNotice,
            associatedDataSchema.Localized,
            associatedDataSchema.Nullable,
            type
        );
    }

    private static IReferenceSchema ToReferenceSchema(GrpcReferenceSchema referenceSchema)
    {
        return ReferenceSchema.InternalBuild(
            referenceSchema.Name,
            NamingConventionHelper.Generate(referenceSchema.Name),
            string.IsNullOrEmpty(referenceSchema.Description) ? null : referenceSchema.Description,
            string.IsNullOrEmpty(referenceSchema.DeprecationNotice) ? null : referenceSchema.DeprecationNotice,
            referenceSchema.EntityType,
            referenceSchema.EntityTypeRelatesToEntity
                ? new Dictionary<NamingConvention, string>()
                : NamingConventionHelper.Generate(referenceSchema.EntityType),
            referenceSchema.EntityTypeRelatesToEntity,
            EvitaEnumConverter.ToCardinality(referenceSchema.Cardinality) ?? new Cardinality(),
            referenceSchema.GroupType,
            referenceSchema.GroupTypeRelatesToEntity
                ? new Dictionary<NamingConvention, string>()
                : NamingConventionHelper.Generate(referenceSchema.GroupType),
            referenceSchema.GroupTypeRelatesToEntity,
            referenceSchema.Indexed,
            referenceSchema.Faceted,
            referenceSchema.Attributes.ToDictionary(
                x => x.Key,
                x => ToAttributeSchema(x.Value)
            ),
            referenceSchema.SortableAttributeCompounds.ToDictionary(
                x => x.Key,
                x => ToSortableAttributeCompoundSchema(x.Value)
            )
        );
    }

    private static IDictionary<string, GrpcSortableAttributeCompoundSchema> ToGrpcSortableAttributeCompoundSchemas(
        IDictionary<string, SortableAttributeCompoundSchema> originalSortableAttributeCompoundSchemas)
    {
        IDictionary<string, GrpcSortableAttributeCompoundSchema> attributeSchemas =
            new Dictionary<string, GrpcSortableAttributeCompoundSchema>();
        foreach (var entry in originalSortableAttributeCompoundSchemas)
        {
            attributeSchemas.Add(entry.Key, ToGrpcSortableAttributeCompoundSchema(entry.Value));
        }

        return attributeSchemas;
    }

    private static GrpcSortableAttributeCompoundSchema ToGrpcSortableAttributeCompoundSchema(
        ISortableAttributeCompoundSchema attributeSchema)
    {
        return new GrpcSortableAttributeCompoundSchema
        {
            Name = attributeSchema.Name,
            AttributeElements = {ToGrpcAttributeElement(attributeSchema.AttributeElements)},
            Description = attributeSchema.Description,
            DeprecationNotice = attributeSchema.DeprecationNotice
        };
    }

    private static SortableAttributeCompoundSchema ToSortableAttributeCompoundSchema(
        GrpcSortableAttributeCompoundSchema sortableAttributeCompound)
    {
        return SortableAttributeCompoundSchema.InternalBuild(
            sortableAttributeCompound.Name,
            NamingConventionHelper.Generate(sortableAttributeCompound.Name),
            sortableAttributeCompound.Description,
            sortableAttributeCompound.DeprecationNotice,
            ToAttributeElement(sortableAttributeCompound.AttributeElements)
        );
    }

    public static List<GrpcAttributeElement> ToGrpcAttributeElement(ICollection<AttributeElement> attributeElementsList)
    {
        return attributeElementsList
            .Select(
                it => new GrpcAttributeElement
                {
                    AttributeName = it.AttributeName,
                    Direction = EvitaEnumConverter.ToGrpcOrderDirection(it.Direction),
                    Behaviour = EvitaEnumConverter.ToGrpcOrderBehaviour(it.Behaviour)
                }
            )
            .ToList();
    }

    public static List<AttributeElement> ToAttributeElement(ICollection<GrpcAttributeElement> attributeElementsList)
    {
        return attributeElementsList
            .Select(it => new AttributeElement(it.AttributeName, EvitaEnumConverter.ToOrderDirection(it.Direction),
                EvitaEnumConverter.ToOrderBehaviour(it.Behaviour)))
            .ToList();
    }

    private static IDictionary<string, GrpcAttributeSchema> ToGrpcAttributeSchemas(
        IDictionary<string, IAttributeSchema> originalAttributeSchemas)
    {
        IDictionary<string, GrpcAttributeSchema> attributeSchemas = new Dictionary<string, GrpcAttributeSchema>();
        foreach (var entry in originalAttributeSchemas)
        {
            attributeSchemas.Add(entry.Key, ToGrpcAttributeSchema(entry.Value));
        }

        return attributeSchemas;
    }

    private static GrpcAttributeSchema ToGrpcAttributeSchema(IAttributeSchema attributeSchema)
    {
        bool isGlobal = attributeSchema is IGlobalAttributeSchema;
        return new GrpcAttributeSchema
        {
            Name = attributeSchema.Name,
            Global = isGlobal,
            Unique = attributeSchema.Unique,
            UniqueGlobally = isGlobal && ((IGlobalAttributeSchema) attributeSchema).UniqueGlobally,
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

    private static IDictionary<string, GrpcAssociatedDataSchema> ToGrpcAssociatedDataSchemas(
        IDictionary<string, IAssociatedDataSchema> originalAssociatedDataSchemas)
    {
        IDictionary<string, GrpcAssociatedDataSchema> associatedDataSchemas =
            new Dictionary<string, GrpcAssociatedDataSchema>();
        foreach (var entry in originalAssociatedDataSchemas)
        {
            associatedDataSchemas.Add(entry.Key, ToGrpcAssociatedDataSchema(entry.Value));
        }

        return associatedDataSchemas;
    }

    private static GrpcAssociatedDataSchema ToGrpcAssociatedDataSchema(IAssociatedDataSchema associatedDataSchema)
    {
        return new GrpcAssociatedDataSchema
        {
            Name = associatedDataSchema.Name,
            Type = EvitaDataTypesConverter.ToGrpcEvitaAssociatedDataDataType(associatedDataSchema.Type),
            Localized = associatedDataSchema.Localized,
            Nullable = associatedDataSchema.Nullable,
            Description = associatedDataSchema.Description,
            DeprecationNotice = associatedDataSchema.DeprecationNotice
        };
    }

    private static IDictionary<string, GrpcReferenceSchema> ToGrpcReferenceSchemas(
        IDictionary<string, IReferenceSchema> originalReferenceSchemas)
    {
        IDictionary<string, GrpcReferenceSchema> referenceSchemas = new Dictionary<string, GrpcReferenceSchema>();
        foreach (var entry in originalReferenceSchemas)
        {
            referenceSchemas.Add(entry.Key, ToGrpcReferenceSchema(entry.Value));
        }

        return referenceSchemas;
    }

    private static GrpcReferenceSchema ToGrpcReferenceSchema(IReferenceSchema referenceSchema)
    {
        return new GrpcReferenceSchema
        {
            Name = referenceSchema.Name,
            Cardinality = EvitaEnumConverter.ToGrpcCardinality(referenceSchema.Cardinality),
            EntityType = referenceSchema.ReferencedEntityType,
            EntityTypeRelatesToEntity = referenceSchema.ReferencedEntityTypeManaged,
            GroupType = referenceSchema.ReferencedGroupType,
            GroupTypeRelatesToEntity = referenceSchema.ReferencedGroupTypeManaged,
            Indexed = referenceSchema.IsIndexed,
            Faceted = referenceSchema.IsFaceted,
            Attributes = {ToGrpcAttributeSchemas(referenceSchema.GetAttributes())},
            SortableAttributeCompounds =
                {ToGrpcSortableAttributeCompoundSchemas(referenceSchema.GetSortableAttributeCompounds())},
            Description = referenceSchema.Description,
            DeprecationNotice = referenceSchema.DeprecationNotice
        };
    }
}