using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.DataTypes;

public class ByteNumberRange : NumberRange<byte>
{
    public static ByteNumberRange Between(byte from, byte to) => new ByteNumberRange(from, to);

    public new static ByteNumberRange From(byte from) => new ByteNumberRange(from, null);

    public new static ByteNumberRange To(byte to) => new ByteNumberRange(null, to);

    public static ByteNumberRange FromString(string stringFormatNumber)
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
        byte? from = delimiter == 1 ? null : ParseByte(stringFormatNumber.Substring(1, delimiter));
        byte? to = delimiter == stringFormatNumber.Length - 2 ? null
            : ParseByte(stringFormatNumber.Substring(delimiter + 1, stringFormatNumber.Length - 1));
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

    private static byte ParseByte(string toBeNumber)
    {
        try
        {
            return Convert.ToByte(toBeNumber);
        }
        catch (FormatException ex)
        {
            throw new DataTypeParseException("String " + toBeNumber + " is not a byte number!");
        }
    }

    internal static ByteNumberRange InternalBuild(byte? from, byte? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) {
        return new ByteNumberRange(from, to, retainedDecimalPlaces, fromToCompare, toToCompare);
    }

    private ByteNumberRange(byte? from, byte? to, int? retainedDecimalPlaces, long
        fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare) {
    }

    private ByteNumberRange(byte? from, byte? to) : base(from, to, null, from ?? long.MinValue, to ?? long.MaxValue)
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
        AssertNotFloatingPointType(from, "from");
        AssertNotFloatingPointType(to, "to");
    }

    protected override long ToComparableLong(byte? valueToCheck, long defaultValue)
    {
        return valueToCheck.HasValue ? Convert.ToInt64(valueToCheck) : defaultValue;
    }

    protected override Type SupportedType => typeof(byte);
}