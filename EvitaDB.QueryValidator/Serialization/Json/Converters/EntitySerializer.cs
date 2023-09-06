using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using Newtonsoft.Json;

namespace EvitaDB.QueryValidator.Serialization.Json.Converters;

public class EntitySerializer : JsonConverter<IEntity>
{
    public override void WriteJson(JsonWriter writer, IEntity? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteStartObject();
        writer.WritePropertyName("primaryKey");
        writer.WriteValue(value.PrimaryKey);
        if (value.ParentAvailable)
        {
            if (value.Parent != null)
            {
                Wrap(() =>
                {
                    writer.WritePropertyName("parent");
                    writer.WriteValue(value.Parent.Value);
                });
            }

            if (value.ParentEntity != null)
            {
                Wrap(() =>
                {
                    writer.WritePropertyName("parentEntity");
                    serializer.Serialize(writer, value.ParentEntity);
                });
            }
        }

        if (value.InnerRecordHandling != PriceInnerRecordHandling.Unknown)
        {
            writer.WritePropertyName("priceInnerRecordHandling");
            writer.WriteValue(value.InnerRecordHandling.ToString().ToUpper());
        }

        WriteAttributes(writer, value);
        WriteAssociatedData(writer, value, serializer);
        WritePrices(writer, value);
        WriteReferences(writer, value, serializer);
        writer.WriteEndObject();
    }

    public override IEntity ReadJson(JsonReader reader, Type objectType, IEntity? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    /**
     * Wraps and rethrows possible {@link IOException} declared on lambda.
     */
    private static void Wrap(Action lambda)
    {
        try
        {
            lambda.Invoke();
        }
        catch (IOException e)
        {
            throw new Exception(e.Message);
        }
    }

    /**
     * Writes a generic number into a JSON.
     */
    private static void WriteNumber<T>(JsonWriter writer, T? theValue)
        where T : struct, IComparable<T>, IEquatable<T>, IConvertible
    {
        Wrap(() =>
        {
            if (theValue is byte byteNumber)
            {
                writer.WriteValue(byteNumber);
            }
            else if (theValue is short shortNumber)
            {
                writer.WriteValue(shortNumber);
            }
            else if (theValue is int intNumber)
            {
                writer.WriteValue(intNumber);
            }
            else if (theValue is long longNumber)
            {
                writer.WriteValue(longNumber);
            }
            else if (theValue is decimal decimalNumber)
            {
                writer.WriteValue(decimalNumber);
            }
            else
            {
                throw new Exception("Unsupported number type: " + theValue?.GetType().Name);
            }
        });
    }

    /**
     * Writes {@link OffsetDateTime} to a JSON.
     */
    private static void WriteDateTime(JsonWriter writer, DateTimeOffset value)
    {
        Wrap(() => writer.WriteValue(EvitaDataTypes.FormatValue(value)));
    }

    /**
     * Writes {@link NumberRange} of a specific number type to a JSON.
     */
    private static void WriteNumberRange<T>(JsonWriter writer, NumberRange<T> range)
        where T : struct, IComparable<T>, IEquatable<T>, IConvertible
    {
        try
        {
            writer.WriteStartArray();
            if (range.PreciseFrom == null)
            {
                writer.WriteNull();
            }
            else
            {
                WriteNumber(writer, range.PreciseFrom);
            }

            if (range.PreciseTo == null)
            {
                writer.WriteNull();
            }
            else
            {
                WriteNumber(writer, range.PreciseTo);
            }

            writer.WriteEndArray();
        }
        catch (IOException e)
        {
            throw new Exception(e.Message);
        }
    }

    /**
     * Writes {@link DateTimeRange} to a JSON.
     */
    private static void WriteDateTimeRange(JsonWriter writer, DateTimeRange range)
    {
        try
        {
            writer.WriteStartArray();
            if (range.PreciseFrom == null)
            {
                writer.WriteNull();
            }
            else
            {
                WriteDateTime(writer, range.PreciseFrom.Value);
            }

            if (range.PreciseTo == null)
            {
                writer.WriteNull();
            }
            else
            {
                WriteDateTime(writer, range.PreciseTo.Value);
            }

            writer.WriteEndArray();
        }
        catch (IOException e)
        {
            throw new Exception(e.Message);
        }
    }

    /**
     * Writes all attributes from {@link AttributesContract} to a JSON.
     */
    private static void WriteAttributes(JsonWriter writer, IAttributes value)
    {
        if (value.AttributesAvailable() && value.GetAttributeValues().Any())
        {
            Wrap(() =>
            {
                writer.WritePropertyName("attributes");
                writer.WriteStartObject();
                foreach (AttributeValue attributeValue in value.GetAttributeValues())
                {
                    object? theValue = attributeValue.Value;
                    string fieldName = attributeValue.Key.ToString();
                    writer.WritePropertyName(fieldName);
                    if (theValue is byte byteNumber)
                    {
                        WriteNumber<byte>(writer, byteNumber);
                    }

                    else if (theValue is short shortNumber)
                    {
                        WriteNumber<short>(writer, shortNumber);
                    }

                    else if (theValue is int intNumber)
                    {
                        WriteNumber<int>(writer, intNumber);
                    }

                    else if (theValue is long longNumber)
                    {
                        WriteNumber<long>(writer, longNumber);
                    }

                    else if (theValue is decimal decimalNumber)
                    {
                        WriteNumber<decimal>(writer, decimalNumber);
                    }
                    else if (theValue is string stringValue)
                    {
                        writer.WriteValue(stringValue);
                    }
                    else if (theValue is bool booleanValue)
                    {
                        writer.WriteValue(booleanValue);
                    }
                    else if (theValue is char character)
                    {
                        writer.WriteValue(character.ToString());
                    }
                    else if (theValue is DateTimeOffset dateTime)
                    {
                        WriteDateTime(writer, dateTime);
                    }
                    else if (theValue is DateTimeRange range)
                    {
                        WriteDateTimeRange(writer, range);
                    }
                    else if (theValue is ByteNumberRange byteRange)
                    {
                        WriteNumberRange(writer, byteRange);
                    }
                    else
                    {
                        writer.WriteValue(EvitaDataTypes.FormatValue(theValue));
                    }
                }

                writer.WriteEndObject();
            });
        }
    }

    /**
     * Writes all associated data from {@link AssociatedDataContract} to a JSON.
     */
    private static void WriteAssociatedData(JsonWriter writer, IAssociatedData value, JsonSerializer serializer)
    {
        if (value.AssociatedDataAvailable() && value.GetAssociatedDataValues().Any())
        {
            Wrap(() =>
            {
                writer.WritePropertyName("associatedData");
                writer.WriteStartObject();
                foreach (AssociatedDataValue associatedDataValue in value.GetAssociatedDataValues())
                {
                    object? theValue = associatedDataValue.Value;
                    string fieldName = associatedDataValue.Key.ToString();
                    if (theValue is byte byteNumber)
                    {
                        WriteNumber<byte>(writer, byteNumber);
                    }

                    else if (theValue is short shortNumber)
                    {
                        WriteNumber<short>(writer, shortNumber);
                    }

                    else if (theValue is int intNumber)
                    {
                        WriteNumber<int>(writer, intNumber);
                    }

                    else if (theValue is long longNumber)
                    {
                        WriteNumber<long>(writer, longNumber);
                    }

                    else if (theValue is decimal decimalNumber)
                    {
                        WriteNumber<decimal>(writer, decimalNumber);
                    }
                    else if (theValue is string stringValue)
                    {
                        writer.WriteValue(stringValue);
                    }
                    else if (theValue is bool booleanValue)
                    {
                        writer.WriteValue(booleanValue);
                    }
                    else if (theValue is char character)
                    {
                        writer.WriteValue(character.ToString());
                    }
                    else if (theValue is DateTimeOffset dateTime)
                    {
                        WriteDateTime(writer, dateTime);
                    }
                    else if (theValue is DateTimeRange range)
                    {
                        WriteDateTimeRange(writer, range);
                    }
                    else if (theValue is ByteNumberRange byteRange)
                    {
                        WriteNumberRange(writer, byteRange);
                    }
                    else if (theValue is ShortNumberRange shortRange)
                    {
                        WriteNumberRange(writer, shortRange);
                    }
                    else if (theValue is IntegerNumberRange intRange)
                    {
                        WriteNumberRange(writer, intRange);
                    }
                    else if (theValue is LongNumberRange longRange)
                    {
                        WriteNumberRange(writer, longRange);
                    }
                    else if (theValue is DecimalNumberRange decimalRange)
                    {
                        WriteNumberRange(writer, decimalRange);
                    }
                    else if (theValue is ComplexDataObject complexDataObject)
                    {
                        ComplexDataObjectToJsonConverter converter = new ComplexDataObjectToJsonConverter();
                        complexDataObject.Accept(converter);
                        writer.WritePropertyName(fieldName);
                        serializer.Serialize(writer, converter.RootNode);
                    }
                    else
                    {
                        writer.WriteValue(EvitaDataTypes.FormatValue(theValue));
                    }
                }

                writer.WriteEndObject();
            });
        }
    }

    /**
     * Writes all reference from {@link ReferenceContract} to a JSON.
     */
    private static void WriteReferences(JsonWriter writer, IEntity value, JsonSerializer serializer)
    {
        if (value.ReferencesAvailable && value.GetReferences().Any())
        {
            Wrap(() =>
            {
                writer.WritePropertyName("references");
                writer.WriteStartObject();
                foreach (var groupBy in value.GetReferences().GroupBy(x => x.ReferenceName).OrderBy(x=>x.Key))
                {
                    WriteReference(writer, serializer, groupBy.Key, groupBy.ToList());
                }

                writer.WriteEndObject();
            });
        }
    }

    /**
     * Writes all references of particular name to a JSON.
     */
    private static void WriteReference(JsonWriter writer, JsonSerializer jsonSerializer, string referenceName, List<IReference> references)
    {
        Wrap(() =>
        {
            writer.WritePropertyName(referenceName);
            writer.WriteStartArray();
            foreach (IReference reference in references)
            {
                writer.WriteStartObject();
                if (reference.Group != null)
                {
                    Wrap(() =>
                    {
                        writer.WritePropertyName("group");
                        writer.WriteValue(reference.Group.PrimaryKey);
                    });
                }

                if (reference.GroupEntity != null)
                {
                    Wrap(() =>
                    {
                        writer.WritePropertyName("groupEntity");
                        jsonSerializer.Serialize(writer, reference.GroupEntity);
                    });
                }

                writer.WritePropertyName("referencedKey");
                writer.WriteValue(reference.ReferenceKey.PrimaryKey);
                if (reference.ReferencedEntity != null)
                {
                    Wrap(() =>
                    {
                        writer.WritePropertyName("referencedEntity");
                        jsonSerializer.Serialize(writer, reference.ReferencedEntity);
                    });
                }

                WriteAttributes(writer, reference);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        });
    }

    /**
     * Writes all prices from {@link PriceContract} to a JSON.
     */
    private static void WritePrices(JsonWriter writer, IEntity value)
    {
        if (value.PricesAvailable)
        {
            IPrice? priceForSale = value.PriceForSale;

            if (priceForSale != null)
            {
                Wrap(() => writer.WritePropertyName("priceForSale"));
                WritePrice(writer, priceForSale);
            }

            if (value.GetPrices().Any())
            {
                Wrap(() =>
                {
                    writer.WritePropertyName("prices");
                    writer.WriteStartArray();
                    foreach (IPrice price in value.GetPrices().OrderBy(x=>x.Key.PriceId))
                    {
                        WritePrice(writer, price);
                    }

                    writer.WriteEndArray();
                });
            }
        }
    }

    /**
     * Writes all {@link PriceContract} to a JSON.
     */
    private static void WritePrice(JsonWriter writer, IPrice value)
    {
        Wrap(() =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("currency");
            writer.WriteValue(value.Currency.CurrencyCode);

            writer.WritePropertyName("priceList");
            writer.WriteValue(value.PriceList);

            writer.WritePropertyName("priceWithoutTax");
            writer.WriteValue(value.PriceWithoutTax);

            writer.WritePropertyName("priceWithTax");
            writer.WriteValue(value.PriceWithTax);
            if (value.Validity is not null)
            {
                Wrap(() => writer.WritePropertyName("validity"));
                WriteDateTimeRange(writer, value.Validity!);
            }

            writer.WriteEndObject();
        });
    }
}