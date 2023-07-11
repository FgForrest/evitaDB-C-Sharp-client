using Client.DataTypes;
using Client.Exceptions;
using Client.Utils;

namespace Client.DataTypes;

public class LongNumberRange : NumberRange<long>
{
    public static LongNumberRange Between(long from, long to) => new LongNumberRange(from, to);

    public new static LongNumberRange From(long from) => new LongNumberRange(from, null);

    public new static LongNumberRange To(long to) => new LongNumberRange(null, to);

    public static LongNumberRange FromString(string stringFormatNumber)
    {
        Assert.IsTrue(
            stringFormatNumber.StartsWith(OpenChar) && stringFormatNumber.EndsWith(CloseChar),
            () => new DataTypeParseException("NumberRange must start with " + OpenChar + " and end with " +
                                             CloseChar + "!")
        );
        int delimiter = stringFormatNumber.IndexOf(IntervalJoin, 1, StringComparison.Ordinal);
        Assert.IsTrue(
            delimiter > -1,
            () => new DataTypeParseException("NumberRange must contain " + IntervalJoin +
                                             " to separate from and to dates!")
        );
        long? from = delimiter == 1 ? null : ParseLong(stringFormatNumber.Substring(1, delimiter));
        long? to = delimiter == stringFormatNumber.Length - 2 ? null
            : ParseLong(stringFormatNumber.Substring(delimiter + 1, stringFormatNumber.Length - 1));
        if (from == null && to != null)
        {
            return To(to.Value);
        }

        if (from != null && to == null)
        {
            return From(from.Value);
        }

        if (from != null && to != null)
        {
            return Between(from.Value, to!.Value);
        }

        throw new DataTypeParseException("Range has no sense with both limits open to infinity!");
    }

    private static long ParseLong(string toBeNumber)
    {
        try
        {
            return Convert.ToInt64(toBeNumber);
        }
        catch (FormatException ex)
        {
            throw new DataTypeParseException("String " + toBeNumber + " is not a long number!");
        }
    }

    public static LongNumberRange InternalBuild(long? from, long? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) {
        return new LongNumberRange(from, to, retainedDecimalPlaces, fromToCompare, toToCompare);
    }

    private LongNumberRange(long? from, long? to, int? retainedDecimalPlaces, long
        fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare) {
    }

    private LongNumberRange(long? from, long? to) : base(from, to, null, from ?? long.MinValue, to ?? long.MaxValue)
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
        AssertNotFloatingPointType(from, "from");
        AssertNotFloatingPointType(to, "to");
    }

    public Range<long?> ConeWithDifferentBounds(long? from, long? to)
    {
        return new LongNumberRange(from, to);
    }

    protected override long ToComparableLong(long? valueToCheck, long defaultValue)
    {
        return valueToCheck ?? defaultValue;
    }

    protected override Type SupportedType => typeof(long);
}