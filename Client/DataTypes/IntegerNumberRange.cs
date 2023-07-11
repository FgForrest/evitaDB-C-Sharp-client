using Client.Exceptions;
using Client.Utils;

namespace Client.DataTypes;

public class IntegerNumberRange : NumberRange<int>
{
    public static IntegerNumberRange Between(int from, int to) => new IntegerNumberRange(from, to);

    public new static IntegerNumberRange From(int from) => new IntegerNumberRange(from, null);

    public new static IntegerNumberRange To(int to) => new IntegerNumberRange(null, to);

    public static IntegerNumberRange FromString(string stringFormatNumber)
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
        int? from = delimiter == 1 ? null : ParseInteger(stringFormatNumber.Substring(1, delimiter));
        int? to = delimiter == stringFormatNumber.Length - 2 ? null
            : ParseInteger(stringFormatNumber.Substring(delimiter + 1, stringFormatNumber.Length - 1));
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

    private static int ParseInteger(string toBeNumber)
    {
        try
        {
            return Convert.ToInt32(toBeNumber);
        }
        catch (FormatException ex)
        {
            throw new DataTypeParseException("String " + toBeNumber + " is not a integer number!");
        }
    }

    public static IntegerNumberRange InternalBuild(int? from, int? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) {
        return new IntegerNumberRange(from, to, retainedDecimalPlaces, fromToCompare, toToCompare);
    }

    private IntegerNumberRange(int? from, int? to, int? retainedDecimalPlaces, long
        fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare) {
    }

    private IntegerNumberRange(int? from, int? to) : base(from, to, null, from ?? long.MinValue, to ?? long.MaxValue)
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
        AssertNotFloatingPointType(from, "from");
        AssertNotFloatingPointType(to, "to");
    }

    protected override long ToComparableLong(int? valueToCheck, long defaultValue)
    {
        return valueToCheck.HasValue ? Convert.ToInt64(valueToCheck) : defaultValue;
    }

    protected override Type SupportedType => typeof(int);
}