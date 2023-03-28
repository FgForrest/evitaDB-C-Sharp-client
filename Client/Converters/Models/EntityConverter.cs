﻿using System.Globalization;
using Client.Converters.DataTypes;
using Client.Models.Data;
using Client.Models.Data.Structure;
using Client.Models.Schemas.Dtos;
using Client.Utils;
using EvitaDB;

namespace Client.Converters.Models;

public class EntityConverter
{
    public static List<EntityReference> ToEntityReferences(IEnumerable<GrpcEntityReference> entityReferences)
    {
        return entityReferences.Select(x => new EntityReference(x.EntityType, x.PrimaryKey)).ToList();
    }

    public static List<SealedEntity> ToSealedEntities(List<GrpcSealedEntity> sealedEntities,
        Func<string, int, EntitySchema> entitySchemaProvider)
    {
        return sealedEntities.Select(x => ToSealedEntity(
                entity => entitySchemaProvider.Invoke(entity.EntityType, entity.Version),
                x
            )
        ).ToList();
    }

    public static SealedEntity ToSealedEntity(Func<GrpcSealedEntity, EntitySchema> entitySchemaProvider,
        GrpcSealedEntity grpcEntity)
    {
	    EntitySchema entitySchema = entitySchemaProvider.Invoke(grpcEntity);
	    return SealedEntity.InternalBuild(
		    grpcEntity.PrimaryKey,
		    grpcEntity.Version,
		    entitySchema,
		    grpcEntity.HierarchicalPlacement is null ? null : ToHierarchicalPlacement(grpcEntity.HierarchicalPlacement),
		    grpcEntity.References
			    .Select(it => ToReference(entitySchema, it))
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
			    grpcEntity.Version,
			    grpcEntity.Prices.Select(ToPrice).ToList(),
			    EvitaEnumConverter.ToPriceInnerRecordHandling(grpcEntity.PriceInnerRecordHandling)
		    ),
		    grpcEntity.Locales
			    .Select(EvitaDataTypesConverter.ToLocale)
			    .ToHashSet()
	    );
    }
    
	private static ICollection<AttributeValue?> ToAttributeValues(
		IDictionary<string, GrpcEvitaValue> globalAttributesMap,
		IDictionary<string, GrpcLocalizedAttribute> localizedAttributesMap
	) {
		List<AttributeValue?> result = new(globalAttributesMap.Count + localizedAttributesMap.Count);
		foreach (var (attributeName, value) in globalAttributesMap) {
			result.Add(
				ToAttributeValue(new AttributeKey(attributeName), value)
			);
		}
		foreach (var (key, localizedAttributeSet) in localizedAttributesMap) {
			CultureInfo locale = new CultureInfo(key);
			foreach (KeyValuePair<string, GrpcEvitaValue> attributeEntry in localizedAttributeSet.Attributes) {
				result.Add(
					ToAttributeValue(
						new AttributeKey(attributeEntry.Key, locale),
						attributeEntry.Value
					)
				);
			}
		}
		return result;
	}

	private static AttributeValue ToAttributeValue(AttributeKey attributeKey, GrpcEvitaValue attributeValue) {
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
	) {
		List<AssociatedDataValue?> result = new(globalAssociatedDataMap.Count + localizedAssociatedDataMap.Count);
		foreach (var (associatedDataName, value) in globalAssociatedDataMap) {
			result.Add(
				ToAssociatedDataValue(
					new AssociatedDataKey(associatedDataName),
					value
				)
			);
		}
		foreach (var (key, localizedAssociatedDataSet) in localizedAssociatedDataMap) {
			CultureInfo locale = new CultureInfo(key);
			foreach (KeyValuePair<string, GrpcEvitaAssociatedDataValue> associatedDataEntry in localizedAssociatedDataSet.AssociatedData) {
				result.Add(
					ToAssociatedDataValue(
						new AssociatedDataKey(associatedDataEntry.Key, locale),
						associatedDataEntry.Value
					)
				);
			}
		}
		return result;
	}
			
	private static AssociatedDataValue ToAssociatedDataValue(
		AssociatedDataKey associatedDataKey,
		GrpcEvitaAssociatedDataValue associatedDataValue
	) {
		Assert.IsTrue(associatedDataValue.Version.HasValue, "Missing attribute value version.");
		return new AssociatedDataValue(
			associatedDataValue.Version!.Value,
			associatedDataKey,
			associatedDataValue.PrimitiveValue is not null ?
				EvitaDataTypesConverter.ToEvitaValue(associatedDataValue.PrimitiveValue) :
				ComplexDataObjectConverter.ConvertJsonToComplexDataObject(associatedDataValue.JsonValue)
		);
	}
			
	private static Price ToPrice(GrpcPrice grpcPrice) {
		return new Price(
			grpcPrice.Version,
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
			grpcPrice.Sellable
		);
	}
	
	private static AttributesHolder BuildAttributes(
		ICollection<AttributeValue> entityAttributes
	) {
		Dictionary<CultureInfo, GrpcLocalizedAttribute> localizedAttributes = new Dictionary<CultureInfo, GrpcLocalizedAttribute>();
		Dictionary<string, GrpcEvitaValue> globalAttributes = new Dictionary<string, GrpcEvitaValue>();
		foreach (AttributeValue attribute in entityAttributes) {
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
			} else {
				globalAttributes.Add(
					attribute.Key.AttributeName,
					EvitaDataTypesConverter.ToGrpcEvitaValue(attribute.Value, attribute.Version)
				);
			}
		}

		return new AttributesHolder(
			localizedAttributes
				.ToDictionary(x=>x.Key.IetfLanguageTag, x=>x.Value),
			globalAttributes
		);
	}
		
	public static HierarchicalPlacement ToHierarchicalPlacement(
		GrpcHierarchicalPlacement hierarchicalPlacement
	) {
		return new HierarchicalPlacement(
			hierarchicalPlacement.Version,
			hierarchicalPlacement.ParentPrimaryKey,
			hierarchicalPlacement.OrderAmongSiblings
		);
	}
	
	public static Reference ToReference(
		EntitySchema entitySchema,
		GrpcReference grpcReference
	) {
		return new Reference(
			entitySchema,
			grpcReference.Version,
			grpcReference.ReferenceName,
			grpcReference.ReferencedEntityReference.PrimaryKey,
			grpcReference.ReferencedEntityReference.EntityType,
			EvitaEnumConverter.ToCardinality(grpcReference.ReferenceCardinality),
			grpcReference.GroupReferencedEntity != null ?
				new GroupEntityReference(
					grpcReference.GroupReferencedEntityReference.EntityType,
					grpcReference.GroupReferencedEntityReference.PrimaryKey,
					grpcReference.GroupReferencedEntityReference.Version
				) :
				null,
			ToAttributeValues(
				grpcReference.GlobalAttributes,
				grpcReference.LocalizedAttributes
			)
		);
	}

	private record AttributesHolder(Dictionary<string, GrpcLocalizedAttribute> LocalizedAttributes,
		Dictionary<string, GrpcEvitaValue> GlobalAttributes);
}