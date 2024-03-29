﻿using System.Globalization;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Converters.Models.Data;

public static class EntityConverter
{
    public static IList<EntityReference> ToEntityReferences(IEnumerable<GrpcEntityReference> entityReferences)
    {
        return entityReferences.Select(ToEntityReference).ToList();
    }

    public static EntityReference ToEntityReference(GrpcEntityReference entityReference)
    {
        return new EntityReference(entityReference.EntityType, entityReference.PrimaryKey);
    }

    private static EntityReferenceWithParent ToEntityReferenceWithParent(
        GrpcEntityReferenceWithParent entityReferenceWithParent)
    {
        return new EntityReferenceWithParent(
            entityReferenceWithParent.EntityType, entityReferenceWithParent.PrimaryKey,
            entityReferenceWithParent.Parent is not null
                ? ToEntityReferenceWithParent(entityReferenceWithParent.Parent)
                : null
        );
    }

    public static List<T> ToEntities<T>(IEnumerable<GrpcSealedEntity> sealedEntities,
        Func<string, int, ISealedEntitySchema> entitySchemaProvider, EvitaRequest evitaRequest)
    {
        return sealedEntities.Select(x => ToEntity<T>(
                entity => entitySchemaProvider.Invoke(entity.EntityType, entity.Version),
                x,
                evitaRequest
            )
        ).ToList();
    }

    public static T ToEntity<T>(Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaProvider,
        GrpcSealedEntity grpcEntity, EvitaRequest evitaRequest, ISealedEntity? parent = null)
    {
        ISealedEntitySchema entitySchema = entitySchemaProvider.Invoke(grpcEntity);
        IEntityClassifierWithParent? parentEntity;
        if (grpcEntity.ParentEntity is not null)
        {
            parentEntity = ToEntity<ISealedEntity>(entitySchemaProvider, grpcEntity.ParentEntity, evitaRequest);
        }
        else if (grpcEntity.ParentReference is not null)
        {
            parentEntity = ToEntityReferenceWithParent(grpcEntity.ParentReference);
        }
        else
        {
            parentEntity = parent;
        }

        List<Reference> references = grpcEntity.References
            .Select(it => ToReference(entitySchema, entitySchemaProvider, it, evitaRequest))
            .ToList();

        if (IDevelopmentConstants.IsTestRun)
        {
            references.Sort((x, y) => x.ReferenceKey.CompareTo(y.ReferenceKey));
        }

        return (T)Entity.InternalBuild(
            grpcEntity.PrimaryKey,
            grpcEntity.Version,
            entitySchema,
            grpcEntity.Parent,
            parentEntity,
            grpcEntity.References
                .Select(it => ToReference(entitySchema, entitySchemaProvider, it, evitaRequest))
                .ToList(),
            new EntityAttributes(
                entitySchema,
                ToAttributeValues(
                    grpcEntity.GlobalAttributes,
                    grpcEntity.LocalizedAttributes
                ),
                entitySchema.Attributes
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
            evitaRequest,
            false,
            ToPrice(grpcEntity.PriceForSale)
        );
    }

    private static IDictionary<AttributeKey, AttributeValue> ToAttributeValues(
        IDictionary<string, GrpcEvitaValue> globalAttributesMap,
        IDictionary<string, GrpcLocalizedAttribute> localizedAttributesMap
    )
    {
        AttributeValue[] attributeValueTuples = new AttributeValue[globalAttributesMap.Count + localizedAttributesMap.Values.Select(x => x.Attributes.Count).Sum()];
        int index = 0;
        foreach (var (key, localizedAttributeSet) in localizedAttributesMap)
        {
            CultureInfo locale = new CultureInfo(key);
            foreach (KeyValuePair<string, GrpcEvitaValue> attributeEntry in localizedAttributeSet.Attributes)
            {
                attributeValueTuples[index++] =
                    ToAttributeValue(new AttributeKey(attributeEntry.Key, locale), attributeEntry.Value);
            }
        }

        foreach (var (attributeName, value) in globalAttributesMap)
        {
            attributeValueTuples[index++] = ToAttributeValue(new AttributeKey(attributeName), value);
        }

        if (IDevelopmentConstants.IsTestRun)
        {
            Array.Sort(attributeValueTuples, (x, y) => x.Key.CompareTo(y.Key));
        }
        
        IDictionary<AttributeKey, AttributeValue> result = new Dictionary<AttributeKey, AttributeValue>(attributeValueTuples.Length);
        foreach (var attributeValue in attributeValueTuples)
        {
            result.Add(attributeValue.Key, attributeValue);
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
    
    private static IDictionary<AssociatedDataKey, AssociatedDataValue> ToAssociatedDataValues(
        IDictionary<string, GrpcEvitaAssociatedDataValue> globalAssociatedDataMap,
        IDictionary<string, GrpcLocalizedAssociatedData> localizedAssociatedDataMap
    )
    {
        AssociatedDataValue[] associatedDataValueTuples = new AssociatedDataValue[globalAssociatedDataMap.Count + localizedAssociatedDataMap.Values.Select(x => x.AssociatedData.Count).Sum()];
        int index = 0;
        foreach (var (key, localizedAssociatedDataSet) in localizedAssociatedDataMap)
        {
            CultureInfo locale = new CultureInfo(key);
            foreach (KeyValuePair<string, GrpcEvitaAssociatedDataValue> associatedDataEntry in localizedAssociatedDataSet.AssociatedData)
            {
                associatedDataValueTuples[index++] =
                    ToAssociatedDataValue(new AssociatedDataKey(associatedDataEntry.Key, locale), associatedDataEntry.Value);
            }
        }

        foreach (var (attributeName, value) in globalAssociatedDataMap)
        {
            associatedDataValueTuples[index++] = ToAssociatedDataValue(new AssociatedDataKey(attributeName), value);
        }

        if (IDevelopmentConstants.IsTestRun)
        {
            Array.Sort(associatedDataValueTuples, (x, y) => x.Key.CompareTo(y.Key));
        }
        
        IDictionary<AssociatedDataKey, AssociatedDataValue> result = new Dictionary<AssociatedDataKey, AssociatedDataValue>(associatedDataValueTuples.Length);
        foreach (var associatedDataValue in associatedDataValueTuples)
        {
            result.Add(associatedDataValue.Key, associatedDataValue);
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

    private static Reference ToReference(
        ISealedEntitySchema entitySchema,
        Func<GrpcSealedEntity, ISealedEntitySchema> entitySchemaProvider,
        GrpcReference grpcReference,
        EvitaRequest evitaRequest)
    {
        GroupEntityReference? group;
        if (grpcReference.GroupReferencedEntityReference is not null)
        {
            group = new GroupEntityReference(
                grpcReference.GroupReferencedEntityReference.EntityType,
                grpcReference.GroupReferencedEntityReference.PrimaryKey,
                grpcReference.GroupReferencedEntityReference.Version
            );
        }
        else if (grpcReference.GroupReferencedEntity is not null)
        {
            group = new GroupEntityReference(
                grpcReference.GroupReferencedEntity.EntityType,
                grpcReference.GroupReferencedEntity.PrimaryKey,
                grpcReference.GroupReferencedEntity.Version
            );
        }
        else
        {
            group = null;
        }

        return new Reference(
            entitySchema,
            grpcReference.Version,
            grpcReference.ReferenceName,
            grpcReference.ReferencedEntityReference?.PrimaryKey ?? grpcReference.ReferencedEntity.PrimaryKey,
            grpcReference.ReferencedEntityReference?.EntityType ?? grpcReference.ReferencedEntity.EntityType,
            EvitaEnumConverter.ToCardinality(grpcReference.ReferenceCardinality),
            group,
            ToAttributeValues(
                grpcReference.GlobalAttributes,
                grpcReference.LocalizedAttributes
            ),
            grpcReference.ReferencedEntity == null
                ? null
                : ToEntity<ISealedEntity>(entitySchemaProvider, grpcReference.ReferencedEntity, evitaRequest),
            grpcReference.GroupReferencedEntity == null
                ? null
                : ToEntity<ISealedEntity>(entitySchemaProvider, grpcReference.GroupReferencedEntity, evitaRequest)
        );
    }
}
