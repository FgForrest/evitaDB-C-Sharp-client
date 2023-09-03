using System.Globalization;
using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Converters.Models.Data;

public static class EntityConverter
{
    public static List<EntityReference> ToEntityReferences(IEnumerable<GrpcEntityReference> entityReferences)
    {
        return entityReferences.Select(ToEntityReference).ToList();
    }

    public static EntityReference ToEntityReference(GrpcEntityReference entityReference)
    {
        return new EntityReference(entityReference.EntityType, entityReference.PrimaryKey);
    }
    
    public static EntityReferenceWithParent ToEntityReferenceWithParent(GrpcEntityReferenceWithParent entityReferenceWithParent) {
        return new EntityReferenceWithParent(
            entityReferenceWithParent.EntityType, entityReferenceWithParent.PrimaryKey,
            entityReferenceWithParent.Parent is not null ?
                ToEntityReferenceWithParent(entityReferenceWithParent.Parent) : null
        );
    }

    public static List<SealedEntity> ToSealedEntities(IEnumerable<GrpcSealedEntity> sealedEntities,
        Func<string, int, EntitySchema> entitySchemaProvider)
    {
        return sealedEntities.Select(x => ToSealedEntity(
                entity => entitySchemaProvider.Invoke(entity.EntityType, entity.Version),
                x
            )
        ).ToList();
    }

    public static SealedEntity ToSealedEntity(Func<GrpcSealedEntity, EntitySchema> entitySchemaProvider,
        GrpcSealedEntity grpcEntity, SealedEntity? parent = null)
    {
        EntitySchema entitySchema = entitySchemaProvider.Invoke(grpcEntity);
        IEntityClassifierWithParent? parentEntity;
        if (grpcEntity.ParentEntity is not null)
        {
            parentEntity = ToSealedEntity(entitySchemaProvider, grpcEntity.ParentEntity);
        }
        else if (grpcEntity.ParentReference is not null)
        {
            parentEntity = ToEntityReferenceWithParent(grpcEntity.ParentReference);
        }
        else
        {
            parentEntity = parent;
        }
        return SealedEntity.InternalBuild(
            grpcEntity.PrimaryKey,
            grpcEntity.Version,
            entitySchema,
            grpcEntity.Parent,
            parentEntity,
            grpcEntity.References
                .Select(it => ToReference(entitySchema, entitySchemaProvider, it))
                .ToList(),
            new Attributes(
                entitySchema,
                ToAttributeValues(
                    grpcEntity.GlobalAttributes,
                    grpcEntity.LocalizedAttributes
                )
            ),
            new AssociatedData(
                entitySchema,
                ToAssociatedDataValues(
                    grpcEntity.GlobalAssociatedData,
                    grpcEntity.LocalizedAssociatedData
                )
            ),
            new Prices(
                entitySchema,
                grpcEntity.Version,
                grpcEntity.Prices.Select(ToPrice).ToList()!,
                EvitaEnumConverter.ToPriceInnerRecordHandling(grpcEntity.PriceInnerRecordHandling)
            ),
            grpcEntity.Locales
                .Select(EvitaDataTypesConverter.ToLocale)
                .ToHashSet(),
            ToPrice(grpcEntity.PriceForSale)
        );
    }

    private static ICollection<AttributeValue> ToAttributeValues(
        IDictionary<string, GrpcEvitaValue> globalAttributesMap,
        IDictionary<string, GrpcLocalizedAttribute> localizedAttributesMap
    )
    {
        List<AttributeValue> result = new(globalAttributesMap.Count + localizedAttributesMap.Count);
        foreach (var (key, localizedAttributeSet) in localizedAttributesMap)
        {
            CultureInfo locale = new CultureInfo(key);
            foreach (KeyValuePair<string, GrpcEvitaValue> attributeEntry in localizedAttributeSet.Attributes)
            {
                result.Add(
                    ToAttributeValue(new AttributeKey(attributeEntry.Key, locale),attributeEntry.Value)
                );
            }
        }
        foreach (var (attributeName, value) in globalAttributesMap)
        {
            result.Add(ToAttributeValue(new AttributeKey(attributeName), value));
        }

        return result;
    }

    private static AttributeValue ToAttributeValue(AttributeKey attributeKey, GrpcEvitaValue attributeValue)
    {
        Assert.IsTrue(attributeValue.Version.HasValue, "Missing attribute value version.");
        return new AttributeValue(
            attributeValue.Version!.Value,
            attributeKey,
            EvitaDataTypesConverter.ToEvitaValue(attributeValue)
        );
    }

    private static ICollection<AssociatedDataValue?> ToAssociatedDataValues(
        IDictionary<string, GrpcEvitaAssociatedDataValue> globalAssociatedDataMap,
        IDictionary<string, GrpcLocalizedAssociatedData> localizedAssociatedDataMap
    )
    {
        List<AssociatedDataValue?> result = new(globalAssociatedDataMap.Count + localizedAssociatedDataMap.Count);

        foreach (var (key, localizedAssociatedDataSet) in localizedAssociatedDataMap)
        {
            CultureInfo locale = new CultureInfo(key);
            foreach (KeyValuePair<string, GrpcEvitaAssociatedDataValue> associatedDataEntry in
                     localizedAssociatedDataSet.AssociatedData)
            {
                result.Add(
                    ToAssociatedDataValue(
                        new AssociatedDataKey(associatedDataEntry.Key, locale),
                        associatedDataEntry.Value
                    )
                );
            }
        }
        foreach (var (associatedDataName, value) in globalAssociatedDataMap)
        {
            result.Add(
                ToAssociatedDataValue(
                    new AssociatedDataKey(associatedDataName),
                    value
                )
            );
        }

        return result;
    }

    private static AssociatedDataValue ToAssociatedDataValue(
        AssociatedDataKey associatedDataKey,
        GrpcEvitaAssociatedDataValue associatedDataValue
    )
    {
        Assert.IsTrue(associatedDataValue.Version.HasValue, "Missing attribute value version.");
        return new AssociatedDataValue(
            associatedDataValue.Version!.Value,
            associatedDataKey,
            associatedDataValue.PrimitiveValue is not null
                ? EvitaDataTypesConverter.ToEvitaValue(associatedDataValue.PrimitiveValue)
                : ComplexDataObjectConverter.ConvertJsonToComplexDataObject(associatedDataValue.JsonValue)
        );
    }

    private static IPrice? ToPrice(GrpcPrice? grpcPrice)
    {
        if (grpcPrice == null)
        {
            return null;
        }
        return new Price(
            new PriceKey(
                grpcPrice.PriceId,
                grpcPrice.PriceList,
                EvitaDataTypesConverter.ToCurrency(grpcPrice.Currency)
            ),
            grpcPrice.InnerRecordId,
            EvitaDataTypesConverter.ToDecimal(grpcPrice.PriceWithoutTax),
            EvitaDataTypesConverter.ToDecimal(grpcPrice.TaxRate),
            EvitaDataTypesConverter.ToDecimal(grpcPrice.PriceWithTax),
            grpcPrice.Validity is not null ? EvitaDataTypesConverter.ToDateTimeRange(grpcPrice.Validity) : null,
            grpcPrice.Sellable,
            grpcPrice.Version
        );
    }

    private static AttributesHolder BuildAttributes(
        IEnumerable<AttributeValue> entityAttributes
    )
    {
        Dictionary<CultureInfo, GrpcLocalizedAttribute> localizedAttributes =
            new Dictionary<CultureInfo, GrpcLocalizedAttribute>();
        Dictionary<string, GrpcEvitaValue> globalAttributes = new Dictionary<string, GrpcEvitaValue>();
        foreach (AttributeValue attribute in entityAttributes)
        {
            if (attribute.Key.Localized)
            {
                if (!localizedAttributes.TryGetValue(attribute.Key.Locale!, out var localizedAttr))
                {
                    localizedAttr = new GrpcLocalizedAttribute();
                }

                localizedAttr.Attributes.Add(
                    attribute.Key.AttributeName,
                    EvitaDataTypesConverter.ToGrpcEvitaValue(attribute.Value, attribute.Version)
                );
                localizedAttributes[attribute.Key.Locale!] = localizedAttr;
            }
            else
            {
                globalAttributes.Add(
                    attribute.Key.AttributeName,
                    EvitaDataTypesConverter.ToGrpcEvitaValue(attribute.Value, attribute.Version)
                );
            }
        }

        return new AttributesHolder(
            localizedAttributes
                .ToDictionary(x => x.Key.IetfLanguageTag, x => x.Value),
            globalAttributes
        );
    }

    public static Reference ToReference(
        EntitySchema entitySchema,
        Func<GrpcSealedEntity, EntitySchema> entitySchemaProvider,
        GrpcReference grpcReference
    )
    {
        return new Reference(
            entitySchema,
            grpcReference.Version,
            grpcReference.ReferenceName,
            grpcReference.ReferencedEntityReference?.PrimaryKey ?? grpcReference.ReferencedEntity.PrimaryKey,
            grpcReference.ReferencedEntityReference?.EntityType ?? grpcReference.ReferencedEntity.EntityType,
            EvitaEnumConverter.ToCardinality(grpcReference.ReferenceCardinality),
            grpcReference.GroupReferencedEntity is not null
                ? new GroupEntityReference(
                    grpcReference.GroupReferencedEntityReference.EntityType,
                    grpcReference.GroupReferencedEntityReference.PrimaryKey,
                    grpcReference.GroupReferencedEntityReference.Version
                )
                : null,
            ToAttributeValues(
                grpcReference.GlobalAttributes,
                grpcReference.LocalizedAttributes
            ),
            grpcReference.ReferencedEntity == null ? null : ToSealedEntity(entitySchemaProvider, grpcReference.ReferencedEntity),
            grpcReference.GroupReferencedEntity == null ? null : ToSealedEntity(entitySchemaProvider, grpcReference.GroupReferencedEntity)
        );
    }

    private record AttributesHolder(Dictionary<string, GrpcLocalizedAttribute> LocalizedAttributes,
        Dictionary<string, GrpcEvitaValue> GlobalAttributes);
}