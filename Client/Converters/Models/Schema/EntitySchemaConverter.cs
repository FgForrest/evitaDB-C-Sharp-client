using Client.Converters.DataTypes;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using Client.Utils;
using EvitaDB;

namespace Client.Converters.Models.Schema;

public static class EntitySchemaConverter
{
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
}