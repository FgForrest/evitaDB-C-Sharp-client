using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;
using Type = System.Type;

namespace EvitaDB.Client.Converters.DataTypes;

public static class EvitaDataTypesConverter
{
    public static object ToEvitaValue(GrpcEvitaValue value)
    {
        return value.Type switch
        {
            GrpcEvitaDataType.String => value.StringValue,
            GrpcEvitaDataType.Byte => ToByte(value.IntegerValue),
            GrpcEvitaDataType.Short => ToShort(value.IntegerValue),
            GrpcEvitaDataType.Integer => value.IntegerValue,
            GrpcEvitaDataType.Long => value.LongValue,
            GrpcEvitaDataType.Boolean => value.BooleanValue,
            GrpcEvitaDataType.Character => ToCharacter(value.StringValue),
            GrpcEvitaDataType.BigDecimal => ToDecimal(value.BigDecimalValue),
            GrpcEvitaDataType.OffsetDateTime => ToDateTimeOffset(value.OffsetDateTimeValue),
            GrpcEvitaDataType.LocalDateTime => ToDateTime(value.OffsetDateTimeValue),
            GrpcEvitaDataType.LocalDate => ToDate(value.OffsetDateTimeValue),
            GrpcEvitaDataType.LocalTime => ToTime(value.OffsetDateTimeValue),
            GrpcEvitaDataType.DateTimeRange => ToDateTimeRange(value.DateTimeRangeValue),
            GrpcEvitaDataType.BigDecimalNumberRange => ToDecimalNumberRange(value.BigDecimalNumberRangeValue),
            GrpcEvitaDataType.LongNumberRange => ToLongNumberRange(value.LongNumberRangeValue),
            GrpcEvitaDataType.IntegerNumberRange => ToIntegerNumberRange(value.IntegerNumberRangeValue),
            GrpcEvitaDataType.ShortNumberRange => ToShortNumberRange(value.IntegerNumberRangeValue),
            GrpcEvitaDataType.ByteNumberRange => ToByteNumberRange(value.IntegerNumberRangeValue),
            GrpcEvitaDataType.Locale => ToLocale(value.LocaleValue),
            GrpcEvitaDataType.Currency => ToCurrency(value.CurrencyValue),
            GrpcEvitaDataType.Uuid => ToGuid(value.UuidValue),
            GrpcEvitaDataType.Predecessor => ToPredecessor(value.PredecessorValue)!,

            GrpcEvitaDataType.StringArray => ToStringArray(value.StringArrayValue),
            GrpcEvitaDataType.ByteArray => ToIntegerArray(value.IntegerArrayValue),
            GrpcEvitaDataType.ShortArray => ToIntegerArray(value.IntegerArrayValue),
            GrpcEvitaDataType.IntegerArray => ToIntegerArray(value.IntegerArrayValue),
            GrpcEvitaDataType.LongArray => ToLongArray(value.LongArrayValue),
            GrpcEvitaDataType.BooleanArray => ToBooleanArray(value.BooleanArrayValue),
            GrpcEvitaDataType.CharacterArray => ToCharacterArray(value.StringArrayValue),
            GrpcEvitaDataType.BigDecimalArray => ToDecimalArray(value.BigDecimalArrayValue),
            GrpcEvitaDataType.OffsetDateTimeArray => ToDateTimeOffsetArray(value.OffsetDateTimeArrayValue),
            GrpcEvitaDataType.LocalDateTimeArray => ToDateTimeArray(value.OffsetDateTimeArrayValue),
            GrpcEvitaDataType.LocalDateArray => ToDateArray(value.OffsetDateTimeArrayValue),
            GrpcEvitaDataType.LocalTimeArray => ToTimeArray(value.OffsetDateTimeArrayValue),
            GrpcEvitaDataType.DateTimeRangeArray => ToDateTimeRangeArray(value.DateTimeRangeArrayValue),
            GrpcEvitaDataType.BigDecimalNumberRangeArray => ToDecimalNumberRangeArray(
                value.BigDecimalNumberRangeArrayValue),
            GrpcEvitaDataType.LongNumberRangeArray => ToLongNumberRangeArray(value.LongNumberRangeArrayValue),
            GrpcEvitaDataType.IntegerNumberRangeArray => ToIntegerNumberRangeArray(value.IntegerNumberRangeArrayValue),
            GrpcEvitaDataType.ShortNumberRangeArray => ToShortNumberRangeArray(value.IntegerNumberRangeArrayValue),
            GrpcEvitaDataType.ByteNumberRangeArray => ToByteNumberRangeArray(value.IntegerNumberRangeArrayValue),
            GrpcEvitaDataType.LocaleArray => ToLocaleArray(value.LocaleArrayValue),
            GrpcEvitaDataType.CurrencyArray => ToCurrencyArray(value.CurrencyArrayValue),
            GrpcEvitaDataType.UuidArray => ToGuidArray(value.UuidArrayValue),
            _ => throw new EvitaInternalError("Unsupported Evita data type in gRPC API `" + value.ValueCase + "`")
        };
    }

    public static Type ToEvitaDataType(GrpcEvitaDataType dataType)
    {
        return dataType switch
        {
            GrpcEvitaDataType.String => typeof(string),
            GrpcEvitaDataType.Byte => typeof(byte),
            GrpcEvitaDataType.Short => typeof(short),
            GrpcEvitaDataType.Integer => typeof(int),
            GrpcEvitaDataType.Long => typeof(long),
            GrpcEvitaDataType.Boolean => typeof(bool),
            GrpcEvitaDataType.Character => typeof(char),
            GrpcEvitaDataType.BigDecimal => typeof(decimal),
            GrpcEvitaDataType.OffsetDateTime => typeof(DateTimeOffset),
            GrpcEvitaDataType.LocalDateTime => typeof(DateTime),
            GrpcEvitaDataType.LocalDate => typeof(DateOnly),
            GrpcEvitaDataType.LocalTime => typeof(TimeOnly),
            GrpcEvitaDataType.DateTimeRange => typeof(DateTimeRange),
            GrpcEvitaDataType.BigDecimalNumberRange => typeof(DecimalNumberRange),
            GrpcEvitaDataType.LongNumberRange => typeof(LongNumberRange),
            GrpcEvitaDataType.IntegerNumberRange => typeof(IntegerNumberRange),
            GrpcEvitaDataType.ShortNumberRange => typeof(ShortNumberRange),
            GrpcEvitaDataType.ByteNumberRange => typeof(ByteNumberRange),
            GrpcEvitaDataType.Locale => typeof(CultureInfo),
            GrpcEvitaDataType.Currency => typeof(Currency),
            GrpcEvitaDataType.Uuid => typeof(Guid),
            GrpcEvitaDataType.Predecessor => typeof(Predecessor),

            GrpcEvitaDataType.StringArray => typeof(string[]),
            GrpcEvitaDataType.ByteArray => typeof(byte[]),
            GrpcEvitaDataType.ShortArray => typeof(short[]),
            GrpcEvitaDataType.IntegerArray => typeof(int[]),
            GrpcEvitaDataType.LongArray => typeof(long[]),
            GrpcEvitaDataType.BooleanArray => typeof(bool[]),
            GrpcEvitaDataType.CharacterArray => typeof(char[]),
            GrpcEvitaDataType.BigDecimalArray => typeof(decimal[]),
            GrpcEvitaDataType.OffsetDateTimeArray => typeof(DateTimeOffset[]),
            GrpcEvitaDataType.LocalDateTimeArray => typeof(DateTime[]),
            GrpcEvitaDataType.LocalDateArray => typeof(DateOnly[]),
            GrpcEvitaDataType.LocalTimeArray => typeof(TimeOnly[]),
            GrpcEvitaDataType.DateTimeRangeArray => typeof(DateTimeRange[]),
            GrpcEvitaDataType.BigDecimalNumberRangeArray => typeof(DecimalNumberRange[]),
            GrpcEvitaDataType.LongNumberRangeArray => typeof(LongNumberRange[]),
            GrpcEvitaDataType.IntegerNumberRangeArray => typeof(IntegerNumberRange[]),
            GrpcEvitaDataType.ShortNumberRangeArray => typeof(ShortNumberRange[]),
            GrpcEvitaDataType.ByteNumberRangeArray => typeof(ByteNumberRange[]),
            GrpcEvitaDataType.LocaleArray => typeof(CultureInfo[]),
            GrpcEvitaDataType.CurrencyArray => typeof(Currency[]),
            GrpcEvitaDataType.UuidArray => typeof(Guid[]),
            _ => throw new EvitaInternalError("Unsupported Evita data type in gRPC API `" + dataType + "`.")
        };
    }

    public static Type ToEvitaDataType(GrpcEvitaAssociatedDataDataType.Types.GrpcEvitaDataType dataType)
    {
        if (dataType == GrpcEvitaAssociatedDataDataType.Types.GrpcEvitaDataType.ComplexDataObject)
        {
            return typeof(ComplexDataObject);
        }

        return ToEvitaDataType(Enum.Parse<GrpcEvitaDataType>(dataType.ToString()));
    }

    public static GrpcEvitaAssociatedDataDataType.Types.GrpcEvitaDataType ToGrpcEvitaAssociatedDataDataType(
        Type dataType)
    {
        if (dataType == typeof(ComplexDataObject))
        {
            return GrpcEvitaAssociatedDataDataType.Types.GrpcEvitaDataType.ComplexDataObject;
        }

        return Enum.Parse<GrpcEvitaAssociatedDataDataType.Types.GrpcEvitaDataType>(ToGrpcEvitaDataType(dataType).ToString());
    }

    public static object ToEvitaValue(GrpcEvitaAssociatedDataValue value)
    {
        if (value.ValueCase == GrpcEvitaAssociatedDataValue.ValueOneofCase.PrimitiveValue)
        {
            return ToEvitaValue(value.PrimitiveValue);
        }

        if (value.ValueCase == GrpcEvitaAssociatedDataValue.ValueOneofCase.JsonValue)
        {
            return ComplexDataObjectConverter.ConvertJsonToComplexDataObject(value.JsonValue);
        }

        throw new EvitaInternalError("Unknown value type.");
    }

    public static GrpcEvitaValue ToGrpcEvitaValue(object? value, int? version = null)
    {
        GrpcEvitaValue result = new();
        if (value is null)
            return result;

        if (version.HasValue)
            result.Version = version.Value;

        switch (value)
        {
            case string stringValue:
                result.StringValue = stringValue;
                result.Type = GrpcEvitaDataType.String;
                break;
            case char characterValue:
                result.StringValue = ToGrpcCharacter(characterValue);
                result.Type = GrpcEvitaDataType.Character;
                break;
            case int integerValue:
                result.IntegerValue = integerValue;
                result.Type = GrpcEvitaDataType.Integer;
                break;
            case short shortValue:
                result.IntegerValue = shortValue;
                result.Type = GrpcEvitaDataType.Short;
                break;
            case byte byteValue:
                result.IntegerValue = byteValue;
                result.Type = GrpcEvitaDataType.Byte;
                break;
            case long longValue:
                result.LongValue = longValue;
                result.Type = GrpcEvitaDataType.Long;
                break;
            case bool booleanValue:
                result.BooleanValue = booleanValue;
                result.Type = GrpcEvitaDataType.Boolean;
                break;
            case decimal decimalValue:
                result.BigDecimalValue = ToGrpcBigDecimal(decimalValue);
                result.Type = GrpcEvitaDataType.BigDecimal;
                break;
            case DateTimeOffset offsetDateTimeValue:
                result.OffsetDateTimeValue = ToGrpcDateTime(offsetDateTimeValue);
                result.Type = GrpcEvitaDataType.OffsetDateTime;
                break;
            case DateTime dateTimeValue:
                result.OffsetDateTimeValue = ToGrpcDateTime(dateTimeValue);
                result.Type = GrpcEvitaDataType.LocalDateTime;
                break;
            case DateOnly dateValue:
                result.OffsetDateTimeValue = ToGrpcDate(dateValue);
                result.Type = GrpcEvitaDataType.LocalDate;
                break;
            case TimeOnly timeValue:
                result.OffsetDateTimeValue = ToGrpcTime(timeValue);
                result.Type = GrpcEvitaDataType.LocalDateTime;
                break;
            case DateTimeRange dateTimeRangeValue:
                result.DateTimeRangeValue = ToGrpcDateTimeRange(dateTimeRangeValue);
                result.Type = GrpcEvitaDataType.DateTimeRange;
                break;
            case DecimalNumberRange decimalNumberRangeValue:
                result.BigDecimalNumberRangeValue = ToGrpcBigDecimalNumberRange(decimalNumberRangeValue);
                result.Type = GrpcEvitaDataType.BigDecimalNumberRange;
                break;
            case LongNumberRange longNumberRangeValue:
                result.LongNumberRangeValue = ToGrpcLongNumberRange(longNumberRangeValue);
                result.Type = GrpcEvitaDataType.LongNumberRange;
                break;
            case IntegerNumberRange integerNumberRangeValue:
                result.IntegerNumberRangeValue = ToGrpcIntegerNumberRange(integerNumberRangeValue);
                result.Type = GrpcEvitaDataType.IntegerNumberRange;
                break;
            case ShortNumberRange shortNumberRangeValue:
                result.IntegerNumberRangeValue = ToGrpcIntegerNumberRange(shortNumberRangeValue);
                result.Type = GrpcEvitaDataType.ShortNumberRange;
                break;
            case ByteNumberRange byteNumberRangeValue:
                result.IntegerNumberRangeValue = ToGrpcIntegerNumberRange(byteNumberRangeValue);
                result.Type = GrpcEvitaDataType.ByteNumberRange;
                break;
            case CultureInfo localeValue:
                result.LocaleValue = ToGrpcLocale(localeValue);
                result.Type = GrpcEvitaDataType.Locale;
                break;
            case Currency currencyValue:
                result.CurrencyValue = ToGrpcCurrency(currencyValue);
                result.Type = GrpcEvitaDataType.Currency;
                break;
            case Guid guidValue:
                result.UuidValue = ToGrpcUuid(guidValue);
                result.Type = GrpcEvitaDataType.Uuid;
                break;
            case Predecessor predecessorValue:
                result.PredecessorValue = ToGrpcPredecessor(predecessorValue);
                result.Type = GrpcEvitaDataType.Predecessor;
                break;

            case string[] stringArrayValue:
                result.StringArrayValue = ToGrpcStringArray(stringArrayValue);
                result.Type = GrpcEvitaDataType.StringArray;
                break;
            case char[] characterArrayValue:
                result.StringArrayValue = ToGrpcCharacterArray(characterArrayValue);
                result.Type = GrpcEvitaDataType.CharacterArray;
                break;
            case int[] integerArrayValue:
                result.IntegerArrayValue = ToGrpcIntegerArray(integerArrayValue);
                result.Type = GrpcEvitaDataType.IntegerArray;
                break;
            case short[] shortArrayArrayValue:
                result.IntegerArrayValue = ToGrpcShortArray(shortArrayArrayValue);
                result.Type = GrpcEvitaDataType.ShortArray;
                break;
            case byte[] byteArrayValue:
                result.IntegerArrayValue = ToGrpcByteArray(byteArrayValue);
                result.Type = GrpcEvitaDataType.ByteArray;
                break;
            case long[] longArrayValue:
                result.LongArrayValue = ToGrpcLongArray(longArrayValue);
                result.Type = GrpcEvitaDataType.LongArray;
                break;
            case bool[] booleanArrayValue:
                result.BooleanArrayValue = ToGrpcBooleanArray(booleanArrayValue);
                result.Type = GrpcEvitaDataType.BooleanArray;
                break;
            case decimal[] decimalArrayValue:
                result.BigDecimalArrayValue = ToGrpcBigDecimalArray(decimalArrayValue);
                result.Type = GrpcEvitaDataType.BigDecimalArray;
                break;
            case DateTimeOffset[] offsetDateTimeArrayValue:
                result.OffsetDateTimeArrayValue = ToGrpcDateTimeArray(offsetDateTimeArrayValue);
                result.Type = GrpcEvitaDataType.OffsetDateTimeArray;
                break;
            case DateTime[] dateTimeArrayValue:
                result.OffsetDateTimeArrayValue = ToGrpcDateTimeArray(dateTimeArrayValue);
                result.Type = GrpcEvitaDataType.LocalDateTimeArray;
                break;
            case DateOnly[] dateArrayValue:
                result.OffsetDateTimeArrayValue = ToGrpcDateArray(dateArrayValue);
                result.Type = GrpcEvitaDataType.LocalDate;
                break;
            case TimeOnly[] timeArrayValue:
                result.OffsetDateTimeArrayValue = ToGrpcTimeArray(timeArrayValue);
                result.Type = GrpcEvitaDataType.LocalDateTime;
                break;
            case DateTimeRange[] dateTimeRangeArrayValue:
                result.DateTimeRangeArrayValue = ToGrpcDateTimeRangeArray(dateTimeRangeArrayValue);
                result.Type = GrpcEvitaDataType.DateTimeRangeArray;
                break;
            case DecimalNumberRange[] decimalNumberRangeArrayValue:
                result.BigDecimalNumberRangeArrayValue = ToGrpcBigDecimalNumberRangeArray(decimalNumberRangeArrayValue);
                result.Type = GrpcEvitaDataType.BigDecimalNumberRangeArray;
                break;
            case LongNumberRange[] longNumberRangeArrayValue:
                result.LongNumberRangeArrayValue = ToGrpcLongNumberRangeArray(longNumberRangeArrayValue);
                result.Type = GrpcEvitaDataType.LongNumberRangeArray;
                break;
            case IntegerNumberRange[] integerNumberRangeArrayValue:
                result.IntegerNumberRangeArrayValue = ToGrpcIntegerNumberRangeArray(integerNumberRangeArrayValue);
                result.Type = GrpcEvitaDataType.IntegerNumberRangeArray;
                break;
            case ShortNumberRange[] shortNumberRangeArrayValue:
                result.IntegerNumberRangeArrayValue = ToGrpcIntegerNumberRangeArray(shortNumberRangeArrayValue);
                result.Type = GrpcEvitaDataType.ShortNumberRangeArray;
                break;
            case ByteNumberRange[] byteNumberRangeArrayValue:
                result.IntegerNumberRangeArrayValue = ToGrpcIntegerNumberRangeArray(byteNumberRangeArrayValue);
                result.Type = GrpcEvitaDataType.ByteNumberRangeArray;
                break;
            case CultureInfo[] localeArrayValue:
                result.LocaleArrayValue = ToGrpcLocaleArray(localeArrayValue);
                result.Type = GrpcEvitaDataType.LocaleArray;
                break;
            case Currency[] currencyArrayValue:
                result.CurrencyArrayValue = ToGrpcCurrencyArray(currencyArrayValue);
                result.Type = GrpcEvitaDataType.CurrencyArray;
                break;
            case Guid[] guidArrayValue:
                result.UuidArrayValue = ToGrpcUuidArray(guidArrayValue);
                result.Type = GrpcEvitaDataType.CurrencyArray;
                break;
        }

        return result;
    }

    public static GrpcEvitaAssociatedDataValue ToGrpcEvitaAssociatedDataValue(object? value, int? version = null)
    {
        GrpcEvitaAssociatedDataValue grpcEvitaAssociatedDataValue = new();

        if (value is ComplexDataObject complexDataObject)
        {
            grpcEvitaAssociatedDataValue.JsonValue = ComplexDataObjectConverter
                .ConvertComplexDataObjectToJson(complexDataObject).ToString();
        }
        else
        {
            grpcEvitaAssociatedDataValue.PrimitiveValue = ToGrpcEvitaValue(value, version);
        }

        if (version != null)
        {
            grpcEvitaAssociatedDataValue.Version = version;
        }

        return grpcEvitaAssociatedDataValue;
    }

    public static GrpcEvitaDataType ToGrpcEvitaDataType(Type type)
    {
        if (type == typeof(string))
        {
            return GrpcEvitaDataType.String;
        }

        if (type == typeof(char))
        {
            return GrpcEvitaDataType.Character;
        }

        if (type == typeof(int))
        {
            return GrpcEvitaDataType.Integer;
        }

        if (type == typeof(short))
        {
            return GrpcEvitaDataType.Short;
        }

        if (type == typeof(byte))
        {
            return GrpcEvitaDataType.Byte;
        }

        if (type == typeof(long))
        {
            return GrpcEvitaDataType.Long;
        }

        if (type == typeof(bool))
        {
            return GrpcEvitaDataType.Boolean;
        }

        if (type == typeof(decimal))
        {
            return GrpcEvitaDataType.BigDecimal;
        }

        if (type == typeof(DateTimeOffset))
        {
            return GrpcEvitaDataType.OffsetDateTime;
        }

        if (type == typeof(DateTime))
        {
            return GrpcEvitaDataType.LocalDateTime;
        }

        if (type == typeof(DateOnly))
        {
            return GrpcEvitaDataType.LocalDate;
        }

        if (type == typeof(TimeOnly))
        {
            return GrpcEvitaDataType.LocalTime;
        }

        if (type == typeof(DateTimeRange))
        {
            return GrpcEvitaDataType.DateTimeRange;
        }

        if (type == typeof(DecimalNumberRange))
        {
            return GrpcEvitaDataType.BigDecimalNumberRange;
        }

        if (type == typeof(LongNumberRange))
        {
            return GrpcEvitaDataType.LongNumberRange;
        }

        if (type == typeof(IntegerNumberRange))
        {
            return GrpcEvitaDataType.IntegerNumberRange;
        }

        if (type == typeof(ShortNumberRange))
        {
            return GrpcEvitaDataType.ShortNumberRange;
        }

        if (type == typeof(ByteNumberRange))
        {
            return GrpcEvitaDataType.ByteNumberRange;
        }

        if (type == typeof(CultureInfo))
        {
            return GrpcEvitaDataType.Locale;
        }

        if (type == typeof(Currency))
        {
            return GrpcEvitaDataType.Currency;
        }

        if (type == typeof(Guid))
        {
            return GrpcEvitaDataType.Uuid;
        }

        if (type == typeof(string[]))
        {
            return GrpcEvitaDataType.StringArray;
        }

        if (type == typeof(char[]))
        {
            return GrpcEvitaDataType.CharacterArray;
        }

        if (type == typeof(int[]))
        {
            return GrpcEvitaDataType.IntegerArray;
        }

        if (type == typeof(short[]))
        {
            return GrpcEvitaDataType.ShortArray;
        }

        if (type == typeof(byte[]))
        {
            return GrpcEvitaDataType.ByteArray;
        }

        if (type == typeof(long[]))
        {
            return GrpcEvitaDataType.LongArray;
        }

        if (type == typeof(bool[]))
        {
            return GrpcEvitaDataType.BooleanArray;
        }

        if (type == typeof(decimal[]))
        {
            return GrpcEvitaDataType.BigDecimalArray;
        }

        if (type == typeof(DateTimeOffset[]))
        {
            return GrpcEvitaDataType.OffsetDateTimeArray;
        }

        if (type == typeof(DateTime[]))
        {
            return GrpcEvitaDataType.LocalDateTimeArray;
        }

        if (type == typeof(DateOnly[]))
        {
            return GrpcEvitaDataType.LocalDateArray;
        }

        if (type == typeof(TimeOnly[]))
        {
            return GrpcEvitaDataType.LocalTimeArray;
        }

        if (type == typeof(DateTimeRange[]))
        {
            return GrpcEvitaDataType.DateTimeRangeArray;
        }

        if (type == typeof(DecimalNumberRange[]))
        {
            return GrpcEvitaDataType.BigDecimalNumberRangeArray;
        }

        if (type == typeof(LongNumberRange[]))
        {
            return GrpcEvitaDataType.LongNumberRangeArray;
        }

        if (type == typeof(IntegerNumberRange[]))
        {
            return GrpcEvitaDataType.IntegerNumberRangeArray;
        }

        if (type == typeof(ShortNumberRange[]))
        {
            return GrpcEvitaDataType.ShortNumberRangeArray;
        }

        if (type == typeof(ByteNumberRange[]))
        {
            return GrpcEvitaDataType.ByteNumberRangeArray;
        }

        if (type == typeof(CultureInfo[]))
        {
            return GrpcEvitaDataType.LocaleArray;
        }

        if (type == typeof(Currency[]))
        {
            return GrpcEvitaDataType.CurrencyArray;
        }

        if (type == typeof(Guid[]))
        {
            return GrpcEvitaDataType.UuidArray;
        }

        throw new EvitaInvalidUsageException("Unsupported Evita data type in gRPC API `" + type.Name + "`.");
    }

    public static string[] ToStringArray(GrpcStringArray arrayValue)
    {
        return arrayValue.Value.ToArray();
    }

    public static char ToCharacter(string stringValue)
    {
        return stringValue.ElementAt(0);
    }

    public static char[] ToCharacterArray(GrpcStringArray arrayValue)
    {
        return arrayValue.Value.Select(x => x[0]).ToArray();
    }

    public static byte ToByte(int intValue)
    {
        return (byte) intValue;
    }

    public static byte[] ToByteArray(GrpcIntegerArray arrayValue)
    {
        return arrayValue.Value.Select(x => (byte) x).ToArray();
    }

    public static short ToShort(int intValue)
    {
        return (short) intValue;
    }

    public static short[] ToShortArray(GrpcIntegerArray arrayValue)
    {
        return arrayValue.Value.Select(x => (short) x).ToArray();
    }

    public static int[] ToIntegerArray(GrpcIntegerArray arrayValue)
    {
        return arrayValue.Value.ToArray();
    }

    public static long[] ToLongArray(GrpcLongArray arrayValue)
    {
        return arrayValue.Value.ToArray();
    }

    public static bool[] ToBooleanArray(GrpcBooleanArray arrayValue)
    {
        return arrayValue.Value.ToArray();
    }

    public static CultureInfo ToLocale(GrpcLocale localeValue)
    {
        return new CultureInfo(localeValue.LanguageTag);
    }

    public static CultureInfo[] ToLocaleArray(GrpcLocaleArray arrayValue)
    {
        return arrayValue.Value.Select(x => new CultureInfo(x.LanguageTag)).ToArray();
    }

    public static Currency ToCurrency(GrpcCurrency currencyValue)
    {
        return new Currency(currencyValue.Code);
    }

    public static Currency[] ToCurrencyArray(GrpcCurrencyArray arrayValue)
    {
        return arrayValue.Value.Select(x => new Currency(x.Code)).ToArray();
    }

    public static DateTimeOffset ToDateTimeOffset(GrpcOffsetDateTime offsetDateTimeValue)
    {
        TimeSpan hourOffset = offsetDateTimeValue.Offset == "Z" ? 
            TimeSpan.Zero : 
            TimeSpan.FromHours(int.Parse(offsetDateTimeValue.Offset.Substring(1, 2)));
        bool add = offsetDateTimeValue.Offset.ElementAt(0) == '+';
        TimeSpan offset = add ? hourOffset : hourOffset.Negate();
        return ToDateTimeOffset(offsetDateTimeValue.Timestamp)
            .ToOffset(offset);
    }
    
    private static DateTimeOffset ToDateTimeOffset(Timestamp timestamp)
    {
        int milliseconds = timestamp.Nanos / 1000000;
        return DateTimeOffset.UnixEpoch.AddSeconds(timestamp.Seconds).AddMilliseconds(milliseconds);
    }

    public static DateTimeOffset[] ToDateTimeOffsetArray(GrpcOffsetDateTimeArray arrayValue)
    {
        return arrayValue.Value.Select(ToDateTimeOffset).ToArray();
    }

    public static DateTime ToDateTime(GrpcOffsetDateTime offsetDateTimeValue)
    {
        return ToDateTimeOffset(offsetDateTimeValue.Timestamp).DateTime;
    }

    public static DateTime[] ToDateTimeArray(GrpcOffsetDateTimeArray arrayValue)
    {
        return arrayValue.Value.Select(ToDateTime).ToArray();
    }

    public static DateTime ToLocalDateTime(GrpcOffsetDateTime offsetDateTimeValue)
    {
        return DateTimeOffset.FromUnixTimeSeconds(offsetDateTimeValue.Timestamp.Seconds).LocalDateTime;
    }

    public static DateTime[] ToLocalDateTimeArray(GrpcOffsetDateTimeArray arrayValue)
    {
        return arrayValue.Value.Select(ToLocalDateTime).ToArray();
    }

    public static DateOnly ToDate(GrpcOffsetDateTime offsetDateTimeValue)
    {
        return DateOnly.FromDateTime(ToDateTime(offsetDateTimeValue));
    }

    public static DateOnly[] ToDateArray(GrpcOffsetDateTimeArray arrayValue)
    {
        return arrayValue.Value.Select(ToDate).ToArray();
    }

    public static TimeOnly ToTime(GrpcOffsetDateTime offsetDateTimeValue)
    {
        return TimeOnly.FromDateTime(ToDateTime(offsetDateTimeValue));
    }

    public static TimeOnly[] ToTimeArray(GrpcOffsetDateTimeArray arrayValue)
    {
        return arrayValue.Value.Select(ToTime).ToArray();
    }

    public static decimal ToDecimal(GrpcBigDecimal bigDecimalValue)
    {
        return decimal.Parse(bigDecimalValue.ValueString, CultureInfo.InvariantCulture);
    }

    public static decimal[] ToDecimalArray(GrpcBigDecimalArray arrayValue)
    {
        return arrayValue.Value.Select(ToDecimal).ToArray();
    }

    public static DateTimeRange ToDateTimeRange(GrpcDateTimeRange grpcDateTimeRange)
    {
        bool fromSet = grpcDateTimeRange.From is not null;
        bool toSet = grpcDateTimeRange.To is not null;
        if (!fromSet && toSet)
            return DateTimeRange.Until(ToDateTimeOffset(grpcDateTimeRange.To!));
        if (fromSet && !toSet)
            return DateTimeRange.Since(ToDateTimeOffset(grpcDateTimeRange.From!));
        return DateTimeRange.Between(ToDateTimeOffset(grpcDateTimeRange.From!),
            ToDateTimeOffset(grpcDateTimeRange.To!));
    }

    public static DateTimeRange[] ToDateTimeRangeArray(GrpcDateTimeRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToDateTimeRange).ToArray();
    }

    public static ShortNumberRange ToShortNumberRange(GrpcIntegerNumberRange grpcShortNumberRange)
    {
        bool fromSet = grpcShortNumberRange.From.HasValue;
        bool toSet = grpcShortNumberRange.To.HasValue;
        short from = ToShort(grpcShortNumberRange.From!.Value);
        short to = ToShort(grpcShortNumberRange.To!.Value);
        if (!fromSet && toSet)
            return ShortNumberRange.To(to);
        if (fromSet && !toSet)
            return ShortNumberRange.From(from);
        return ShortNumberRange.Between(from, to);
    }

    public static ShortNumberRange[] ToShortNumberRangeArray(GrpcIntegerNumberRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToShortNumberRange).ToArray();
    }

    public static ByteNumberRange ToByteNumberRange(GrpcIntegerNumberRange grpcByteNumberRange)
    {
        bool fromSet = grpcByteNumberRange.From.HasValue;
        bool toSet = grpcByteNumberRange.To.HasValue;
        byte from = ToByte(grpcByteNumberRange.From!.Value);
        byte to = ToByte(grpcByteNumberRange.To!.Value);
        if (!fromSet && toSet)
            return ByteNumberRange.To(to);
        if (fromSet && !toSet)
            return ByteNumberRange.From(from);
        return ByteNumberRange.Between(from, to);
    }

    public static ByteNumberRange[] ToByteNumberRangeArray(GrpcIntegerNumberRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToByteNumberRange).ToArray();
    }

    public static IntegerNumberRange ToIntegerNumberRange(GrpcIntegerNumberRange grpcIntegerNumberRange)
    {
        bool fromSet = grpcIntegerNumberRange.From.HasValue;
        bool toSet = grpcIntegerNumberRange.To.HasValue;
        int from = grpcIntegerNumberRange.From!.Value;
        int to = grpcIntegerNumberRange.To!.Value;
        if (!fromSet && toSet)
            return IntegerNumberRange.To(to);
        if (fromSet && !toSet)
            return IntegerNumberRange.From(from);
        return IntegerNumberRange.Between(from, to);
    }

    public static IntegerNumberRange[] ToIntegerNumberRangeArray(GrpcIntegerNumberRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToIntegerNumberRange).ToArray();
    }

    public static LongNumberRange ToLongNumberRange(GrpcLongNumberRange grpcLongNumberRange)
    {
        bool fromSet = grpcLongNumberRange.From.HasValue;
        bool toSet = grpcLongNumberRange.To.HasValue;
        long from = grpcLongNumberRange.From!.Value;
        long to = grpcLongNumberRange.To!.Value;
        if (!fromSet && toSet)
            return LongNumberRange.To(to);
        if (fromSet && !toSet)
            return LongNumberRange.From(from);
        return LongNumberRange.Between(from, to);
    }

    public static LongNumberRange[] ToLongNumberRangeArray(GrpcLongNumberRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToLongNumberRange).ToArray();
    }

    public static DecimalNumberRange ToDecimalNumberRange(GrpcBigDecimalNumberRange grpcDecimalNumberRange)
    {
        bool fromSet = grpcDecimalNumberRange.From is not null;
        bool toSet = grpcDecimalNumberRange.To is not null;
        if (!fromSet && toSet)
            return DecimalNumberRange.To(ToDecimal(grpcDecimalNumberRange.To!));
        if (fromSet && !toSet)
            return DecimalNumberRange.From(ToDecimal(grpcDecimalNumberRange.From!));
        return DecimalNumberRange.Between(ToDecimal(grpcDecimalNumberRange.From!),
            ToDecimal(grpcDecimalNumberRange.To!));
    }

    public static DecimalNumberRange[] ToDecimalNumberRangeArray(GrpcBigDecimalNumberRangeArray arrayValue)
    {
        return arrayValue.Value.Select(ToDecimalNumberRange).ToArray();
    }

    public static Guid ToGuid(GrpcUuid grpcUuid)
    {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(grpcUuid.MostSignificantBits).CopyTo(bytes, 0);
        BitConverter.GetBytes(grpcUuid.LeastSignificantBits).CopyTo(bytes, 8);
        return new Guid(bytes);
    }

    public static Guid[] ToGuidArray(GrpcUuidArray grpcUuidArray)
    {
        return grpcUuidArray.Value.Select(ToGuid).ToArray();
    }

    public static GrpcStringArray ToGrpcStringArray(string[] stringArrayValues)
    {
        return new GrpcStringArray {Value = {stringArrayValues}};
    }

    public static string ToGrpcCharacter(char charValue)
    {
        return charValue.ToString();
    }

    public static GrpcStringArray ToGrpcCharacterArray(char[] charArrayValues)
    {
        return new GrpcStringArray {Value = {charArrayValues.Select(x => x.ToString()).ToArray()}};
    }

    public static GrpcBooleanArray ToGrpcBooleanArray(bool[] booleanArrayValues)
    {
        return new GrpcBooleanArray {Value = {booleanArrayValues}};
    }

    public static GrpcLongArray ToGrpcLongArray(long[] longArrayValues)
    {
        return new GrpcLongArray {Value = {longArrayValues}};
    }

    public static GrpcIntegerArray ToGrpcByteArray(byte[] byteArrayValues)
    {
        return new GrpcIntegerArray {Value = {byteArrayValues.Select(x => (int) x).ToArray()}};
    }

    public static GrpcIntegerArray ToGrpcShortArray(short[] shortArrayValues)
    {
        return new GrpcIntegerArray {Value = {shortArrayValues.Select(x => (int) x).ToArray()}};
    }

    public static GrpcIntegerArray ToGrpcIntegerArray(int[] intArrayValues)
    {
        return new GrpcIntegerArray {Value = {intArrayValues}};
    }

    public static GrpcBigDecimal ToGrpcBigDecimal(decimal decimalValue)
    {
        return new GrpcBigDecimal
        {
            ValueString = EvitaDataTypes.FormatValue(decimalValue)
        };
    }

    public static GrpcBigDecimalArray ToGrpcBigDecimalArray(decimal[] decimalArrayValues)
    {
        return new GrpcBigDecimalArray {Value = {decimalArrayValues.Select(ToGrpcBigDecimal).ToArray()}};
    }

    public static GrpcOffsetDateTime ToGrpcDateTime(DateTimeOffset dateTimeValue)
    {
        TimeSpan offset = dateTimeValue.Offset;
        return new GrpcOffsetDateTime
        {
            Timestamp = new Timestamp {Seconds = dateTimeValue.ToUnixTimeSeconds(), Nanos = dateTimeValue.Nanosecond},
            Offset = $"{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}"
        };
    }

    public static GrpcOffsetDateTimeArray ToGrpcDateTimeArray(DateTimeOffset[] dateTimeArrayValues)
    {
        return new GrpcOffsetDateTimeArray {Value = {dateTimeArrayValues.Select(ToGrpcDateTime).ToArray()}};
    }

    public static GrpcOffsetDateTime ToGrpcDateTime(DateTime dateTimeValue)
    {
        DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTimeValue, TimeSpan.Zero);
        return ToGrpcDateTime(dateTimeOffset);
    }

    public static GrpcOffsetDateTimeArray ToGrpcDateTimeArray(DateTime[] dateTimeArrayValues)
    {
        return new GrpcOffsetDateTimeArray {Value = {dateTimeArrayValues.Select(ToGrpcDateTime).ToArray()}};
    }

    public static GrpcOffsetDateTime ToGrpcDate(DateOnly dateOnly)
    {
        DateTimeOffset dateTimeOffset =
            new DateTimeOffset(dateOnly.Year, dateOnly.Month, dateOnly.Day, 0, 0, 0, TimeSpan.Zero);
        return ToGrpcDateTime(dateTimeOffset);
    }

    public static GrpcOffsetDateTimeArray ToGrpcDateArray(DateOnly[] dateOnlyArrayValues)
    {
        return new GrpcOffsetDateTimeArray {Value = {dateOnlyArrayValues.Select(ToGrpcDate).ToArray()}};
    }

    public static GrpcOffsetDateTime ToGrpcTime(TimeOnly timeOnly)
    {
        DateTimeOffset dateTimeOffset = new DateTimeOffset(0, 1, 1, timeOnly.Hour, timeOnly.Minute, timeOnly.Second,
            timeOnly.Millisecond, TimeSpan.Zero);
        return ToGrpcDateTime(dateTimeOffset);
    }

    public static GrpcOffsetDateTimeArray ToGrpcTimeArray(TimeOnly[] timeOnlyArrayValues)
    {
        return new GrpcOffsetDateTimeArray {Value = {timeOnlyArrayValues.Select(ToGrpcTime).ToArray()}};
    }

    public static GrpcDateTimeRange ToGrpcDateTimeRange(DateTimeRange dateTimeRange)
    {
        GrpcDateTimeRange grpcDateTimeRange = new GrpcDateTimeRange();
        if (dateTimeRange.PreciseFrom is not null)
        {
            grpcDateTimeRange.From = ToGrpcDateTime(dateTimeRange.PreciseFrom.Value);
        }

        if (dateTimeRange.PreciseTo is not null)
        {
            grpcDateTimeRange.To = ToGrpcDateTime(dateTimeRange.PreciseTo.Value);
        }

        return grpcDateTimeRange;
    }

    public static GrpcDateTimeRangeArray ToGrpcDateTimeRangeArray(DateTimeRange[] dateTimeRangeArrayValues)
    {
        return new GrpcDateTimeRangeArray {Value = {dateTimeRangeArrayValues.Select(ToGrpcDateTimeRange).ToArray()}};
    }

    public static GrpcBigDecimalNumberRange ToGrpcBigDecimalNumberRange(DecimalNumberRange decimalNumberRange)
    {
        GrpcBigDecimalNumberRange grpcBigDecimalNumberRange = new GrpcBigDecimalNumberRange();
        if (decimalNumberRange.PreciseFrom is not null)
        {
            grpcBigDecimalNumberRange.From = ToGrpcBigDecimal(decimalNumberRange.PreciseFrom.Value);
        }

        if (decimalNumberRange.PreciseTo is not null)
        {
            grpcBigDecimalNumberRange.To = ToGrpcBigDecimal(decimalNumberRange.PreciseTo.Value);
        }

        int? retainedDecimalPlaces = decimalNumberRange.RetainedDecimalPlaces;
        grpcBigDecimalNumberRange.DecimalPlacesToCompare = retainedDecimalPlaces ?? 0;
        return grpcBigDecimalNumberRange;
    }

    public static GrpcBigDecimalNumberRangeArray ToGrpcBigDecimalNumberRangeArray(
        DecimalNumberRange[] decimalNumberRangeArrayValues)
    {
        return new GrpcBigDecimalNumberRangeArray
            {Value = {decimalNumberRangeArrayValues.Select(ToGrpcBigDecimalNumberRange).ToArray()}};
    }

    public static GrpcLongNumberRange ToGrpcLongNumberRange(LongNumberRange longNumberRange)
    {
        GrpcLongNumberRange grpcLongNumberRange = new GrpcLongNumberRange();
        if (longNumberRange.PreciseFrom is not null)
        {
            grpcLongNumberRange.From = longNumberRange.PreciseFrom.Value;
        }

        if (longNumberRange.PreciseTo is not null)
        {
            grpcLongNumberRange.To = longNumberRange.PreciseTo.Value;
        }

        return grpcLongNumberRange;
    }

    public static GrpcLongNumberRangeArray ToGrpcLongNumberRangeArray(LongNumberRange[] longNumberRangeArrayValues)
    {
        return new GrpcLongNumberRangeArray
            {Value = {longNumberRangeArrayValues.Select(ToGrpcLongNumberRange).ToArray()}};
    }

    public static GrpcIntegerNumberRange ToGrpcIntegerNumberRange<T>(NumberRange<T> numberRange)
        where T : struct, IComparable<T>, IEquatable<T>, IConvertible
    {
        GrpcIntegerNumberRange grpcIntegerNumberRange = new GrpcIntegerNumberRange();
        if (numberRange.PreciseFrom is not null)
        {
            grpcIntegerNumberRange.From = Convert.ToInt32(numberRange.PreciseFrom.Value);
        }

        if (numberRange.PreciseTo is not null)
        {
            grpcIntegerNumberRange.To = Convert.ToInt32(numberRange.PreciseTo.Value);
        }

        return grpcIntegerNumberRange;
    }

    public static GrpcIntegerNumberRangeArray ToGrpcIntegerNumberRangeArray(
        IntegerNumberRange[] integerRangeArrayValues)
    {
        return new GrpcIntegerNumberRangeArray
            {Value = {integerRangeArrayValues.Select(ToGrpcIntegerNumberRange).ToArray()}};
    }

    public static GrpcIntegerNumberRangeArray ToGrpcIntegerNumberRangeArray(ShortNumberRange[] shortRangeArrayValues)
    {
        return new GrpcIntegerNumberRangeArray
            {Value = {shortRangeArrayValues.Select(ToGrpcIntegerNumberRange).ToArray()}};
    }

    public static GrpcIntegerNumberRangeArray ToGrpcIntegerNumberRangeArray(ByteNumberRange[] byteRangeArrayValues)
    {
        return new GrpcIntegerNumberRangeArray
            {Value = {byteRangeArrayValues.Select(ToGrpcIntegerNumberRange).ToArray()}};
    }

    public static GrpcLocale ToGrpcLocale(CultureInfo locale)
    {
        return new GrpcLocale {LanguageTag = locale.IetfLanguageTag};
    }

    public static GrpcLocaleArray ToGrpcLocaleArray(CultureInfo[] localeArrayValues)
    {
        return new GrpcLocaleArray {Value = {localeArrayValues.Select(ToGrpcLocale).ToArray()}};
    }

    public static GrpcCurrency ToGrpcCurrency(Currency currency)
    {
        return new GrpcCurrency {Code = currency.CurrencyCode};
    }

    public static GrpcCurrencyArray ToGrpcCurrencyArray(Currency[] currencyArrayValues)
    {
        return new GrpcCurrencyArray {Value = {currencyArrayValues.Select(ToGrpcCurrency).ToArray()}};
    }

    public static GrpcUuid ToGrpcUuid(Guid guid)
    {
        byte[] bytes = guid.ToByteArray();
        long mostSignificantBits = BitConverter.ToInt64(bytes, 0);
        long leastSignificantBits = BitConverter.ToInt64(bytes, 8);
        return new GrpcUuid
        {
            MostSignificantBits = mostSignificantBits,
            LeastSignificantBits = leastSignificantBits
        };
    }

    public static GrpcUuidArray ToGrpcUuidArray(Guid[] guidArrayValues)
    {
        return new GrpcUuidArray {Value = {guidArrayValues.Select(ToGrpcUuid).ToArray()}};
    }
    
    public static GrpcPredecessor ToGrpcPredecessor(Predecessor predecessor)
    {
        if (predecessor.IsHead)
        {
            return new GrpcPredecessor
            {
                Head = true
            };
        }

        return new GrpcPredecessor
        {
            PredecessorId = predecessor.PredecessorId
        };
    }
    
    public static Predecessor? ToPredecessor(GrpcPredecessor predecessor) {
        return predecessor.Head ? Predecessor.Head :
            predecessor.PredecessorId.HasValue ? new Predecessor(predecessor.PredecessorId.Value) : null;
    }
}