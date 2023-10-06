using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.DataTypes;

public static class EvitaDataTypes
{
    private static readonly char CharStringDelimiter = '\'';
    private static readonly string StringDelimiter = "" + CharStringDelimiter;
    public static ISet<Type> SupportedTypes { get; }

    static EvitaDataTypes()
    {
        ISet<Type> queryDataTypes = new HashSet<Type>();
        queryDataTypes.Add(typeof(string));
        queryDataTypes.Add(typeof(byte));
        queryDataTypes.Add(typeof(short));
        queryDataTypes.Add(typeof(int));
        queryDataTypes.Add(typeof(long));
        queryDataTypes.Add(typeof(bool));
        queryDataTypes.Add(typeof(char));
        queryDataTypes.Add(typeof(decimal));
        queryDataTypes.Add(typeof(DateTimeOffset));
        queryDataTypes.Add(typeof(DateTime));
        queryDataTypes.Add(typeof(DateOnly));
        queryDataTypes.Add(typeof(TimeOnly));
        queryDataTypes.Add(typeof(DateTimeRange));
        queryDataTypes.Add(typeof(DecimalNumberRange));
        queryDataTypes.Add(typeof(LongNumberRange));
        queryDataTypes.Add(typeof(IntegerNumberRange));
        queryDataTypes.Add(typeof(ShortNumberRange));
        queryDataTypes.Add(typeof(ByteNumberRange));
        queryDataTypes.Add(typeof(CultureInfo));
        queryDataTypes.Add(typeof(Currency));
        queryDataTypes.Add(typeof(Guid));
        queryDataTypes.Add(typeof(Predecessor));
        SupportedTypes = queryDataTypes.ToImmutableHashSet();
    }

    public static bool IsSupportedType(Type? type)
    {
        return type is not null && SupportedTypes.Contains(type);
    }

    public static bool IsSupportedTypeOrItsArray(Type type)
    {
        Type? typeToCheck = type.IsArray ? type.GetElementType() : type;
        return IsSupportedType(typeToCheck);
    }

    public static object? ToSupportedType(object? unknownObject)
    {
        if (unknownObject == null)
        {
            // nulls are allowed
            return null;
        }

        if (unknownObject is float or double)
        {
            // normalize floats and doubles to big decimal
            return Convert.ToDecimal(unknownObject);
        }

        if (unknownObject is DateTime dateTime)
        {
            // always convert local date time to zoned date time
            return new DateTimeOffset(dateTime, TimeSpan.Zero);
        }

        if (unknownObject.GetType().IsEnum)
        {
            return unknownObject;
        }

        if (SupportedTypes.Contains(unknownObject.GetType()))
        {
            return unknownObject;
        }

        throw new UnsupportedDataTypeException(unknownObject.GetType());
    }

    public static object? ToTargetType(object? unknownObject, Type requestedType, int allowedDecimalPlaces = 0)
    {
        if (requestedType.IsInstanceOfType(unknownObject))
        {
            return unknownObject;
        }

        Type? baseRequestedType = requestedType.IsArray ? requestedType.GetElementType() : requestedType;
        Assert.IsTrue(IsSupportedType(baseRequestedType),
            "Requested type `" + requestedType + "` is not supported by Evita!");
        if (requestedType.IsInstanceOfType(unknownObject) || unknownObject is null)
        {
            return unknownObject;
        }

        object result;
        if (unknownObject.GetType().IsArray)
        {
            int inputArrayLength = (unknownObject as object[])!.Length;
            result = Array.CreateInstance(baseRequestedType, inputArrayLength);
            for (int i = 0; i < inputArrayLength; i++)
            {
                (result as object[])[i] = ConvertSingleObject(
                    (unknownObject as object[])[i],
                    baseRequestedType,
                    allowedDecimalPlaces
                );
            }
        }
        else
        {
            result = ConvertSingleObject(unknownObject, baseRequestedType, allowedDecimalPlaces);
        }

        if (requestedType.IsArray)
        {
            if (result.GetType().IsArray)
            {
                return result;
            }

            object[] wrappedResult = Array.CreateInstance(baseRequestedType, 1) as object[];
            wrappedResult[0] = result;
            return wrappedResult;
        }

        return result;
    }

    private static object ConvertSingleObject(object unknownObject, Type requestedType, int allowedDecimalPlaces)
    {
        object result;
        try
        {
            if (typeof(string).IsAssignableFrom(requestedType))
            {
                result = unknownObject.ToString()!;
            }
            else if (typeof(byte).IsAssignableFrom(requestedType))
            {
                result = Convert.ToByte(unknownObject);
            }
            else if (typeof(short).IsAssignableFrom(requestedType))
            {
                result = Convert.ToInt16(unknownObject);
            }
            else if (typeof(int).IsAssignableFrom(requestedType))
            {
                result = Convert.ToInt32(unknownObject);
            }
            else if (typeof(long).IsAssignableFrom(requestedType))
            {
                result = Convert.ToInt64(unknownObject);
            }
            else if (typeof(decimal).IsAssignableFrom(requestedType))
            {
                result = Convert.ToDecimal(unknownObject);
            }
            else if (typeof(bool).IsAssignableFrom(requestedType))
            {
                result = Convert.ToBoolean(unknownObject);
            }
            else if (typeof(char).IsAssignableFrom(requestedType))
            {
                result = Convert.ToChar(unknownObject);
            }
            else if (typeof(DateTimeOffset).IsAssignableFrom(requestedType))
            {
                result = DateTimeOffset.Parse(unknownObject.ToString()!);
            }
            else if (typeof(DateTime).IsAssignableFrom(requestedType))
            {
                result = DateTime.Parse(unknownObject.ToString()!);
            }
            else if (typeof(DateOnly).IsAssignableFrom(requestedType))
            {
                result = DateOnly.Parse(unknownObject.ToString()!);
            }
            else if (typeof(TimeOnly).IsAssignableFrom(requestedType))
            {
                result = TimeOnly.Parse(unknownObject.ToString()!);
            }
            else if (typeof(DateTimeRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as DateTimeRange)!;
            }
            else if (typeof(IntegerNumberRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as IntegerNumberRange)!;
            }
            else if (typeof(ShortNumberRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as ShortNumberRange)!;
            }
            else if (typeof(ByteNumberRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as ByteNumberRange)!;
            }
            else if (typeof(LongNumberRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as LongNumberRange)!;
            }
            else if (typeof(DecimalNumberRange).IsAssignableFrom(requestedType))
            {
                result = (unknownObject as DecimalNumberRange)!;
            }
            else if (typeof(CultureInfo).IsAssignableFrom(requestedType))
            {
                result = CultureInfo.GetCultureInfo(unknownObject.ToString()!);
            }
            else if (typeof(Currency).IsAssignableFrom(requestedType))
            {
                result = unknownObject as Currency ?? new Currency(unknownObject.ToString()!);
            }
            else
            {
                throw new UnsupportedDataTypeException(unknownObject.GetType());
            }
        }
        catch (FormatException e)
        {
            throw new EvitaInternalError("Badly formatted input: " + unknownObject, e);
        }
        catch (ArgumentNullException e)
        {
            throw new EvitaInternalError("Null input: " + unknownObject, e);
        }
        catch (InvalidCastException e)
        {
            throw new EvitaInternalError("Badly casted input: " + unknownObject, e);
        }
        catch (EvitaInternalError e)
        {
            throw new EvitaInternalError("Too big value for the requested data type: " + unknownObject, e);
        }

        return result!;
    }

    public static string FormatValue(object? value)
    {
        if (value is string stringValue)
        {
            return CharStringDelimiter + stringValue.Replace(StringDelimiter, "\\\\'") + StringDelimiter;
        }
        if (value is char charValue)
        {
            return CharStringDelimiter + charValue.ToString().Replace(StringDelimiter, "\\\\'") + StringDelimiter;
        }
        if (value is byte or short or int or long)
        {
            return value.ToString()!;
        }
        if (value is decimal decimalValue)
        {
            string stringDecimal = decimalValue.ToString(CultureInfo.InvariantCulture)
                .Replace("E", "e")
                .Replace("e0", "")
                .Replace("+", "");
            return stringDecimal.StartsWith('.') ? "0" + stringDecimal : stringDecimal;
        }
        if (value is bool boolValue)
        {
            return boolValue.ToString()!;
        }
        if (value is Range range)
        {
            return range.ToString();
        }
        if (value is DateTimeOffset dateTimeOffsetValue)
        {
            return dateTimeOffsetValue.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        }
        if (value is DateTime dateTimeValue)
        {
            return dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }
        if (value is DateOnly dateOnlyValue)
        {
            return dateOnlyValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        if (value is TimeOnly timeOnlyValue)
        {
            return timeOnlyValue.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }
        if (value is CultureInfo cultureInfoValue)
        {
            return CharStringDelimiter + cultureInfoValue.IetfLanguageTag + CharStringDelimiter;
        }
        if (value is Currency currencyValue)
        {
            return CharStringDelimiter + currencyValue.CurrencyCode + CharStringDelimiter;
        }
        if (value is Enum enumValue)
        {
            return enumValue.ToString();
        }
        if (value is Guid guidValue)
        {
            return guidValue.ToString();
        }
        if (value is Predecessor predecessorValue)
        {
            return predecessorValue.ToString()!;
        }
        if (value is null)
        {
            throw new EvitaInternalError(
                "Null argument value should never ever happen. Null values are excluded in constructor of the class!"
            );
        }

        throw new UnsupportedDataTypeException(value.GetType());
    }
}