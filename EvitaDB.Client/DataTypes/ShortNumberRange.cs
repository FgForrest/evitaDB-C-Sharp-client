using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.DataTypes;

public class ShortNumberRange : NumberRange<short>
{
    public static ShortNumberRange Between(short from, short to) => new ShortNumberRange(from, to);

    public new static ShortNumberRange From(short from) => new ShortNumberRange(from, null);

    public new static ShortNumberRange To(short to) => new ShortNumberRange(null, to);

    public static ShortNumberRange FromString(string stringFormatNumber)
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
        short? from = delimiter == 1 ? null : ParseShort(stringFormatNumber.Substring(1, delimiter));
        short? to = delimiter == stringFormatNumber.Length - 2 ? null
            : ParseShort(stringFormatNumber.Substring(delimiter + 1, stringFormatNumber.Length - 1));
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

    private static short ParseShort(string toBeNumber)
    {
        try
        {
            return Convert.ToInt16(toBeNumber);
        }
        catch (FormatException ex)
        {
            throw new DataTypeParseException("String " + toBeNumber + " is not a short number!");
        }
    }

    internal static ShortNumberRange InternalBuild(short? from, short? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) {
        return new ShortNumberRange(from, to, retainedDecimalPlaces, fromToCompare, toToCompare);
    }

    private ShortNumberRange(short? from, short? to, int? retainedDecimalPlaces, long
        fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare) {
    }

    private ShortNumberRange(short? from, short? to) : base(from, to, null, from ?? long.MinValue, to ?? long.MaxValue)
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
        AssertNotFloatingPointType(from, "from");
        AssertNotFloatingPointType(to, "to");
    }

    protected override long ToComparableLong(short? valueToCheck, long defaultValue)
    {
        return valueToCheck.HasValue ? Convert.ToInt64(valueToCheck) : defaultValue;
    }

    protected override Type SupportedType => typeof(short);
}