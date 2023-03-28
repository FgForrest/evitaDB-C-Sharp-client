namespace Client.DataTypes;

public class ByteNumberRange : NumberRange<byte>
{
    public ByteNumberRange(byte? from, byte? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare)
    {
    }

    protected override long ToComparableLong(byte? valueToCheck, long defaultValue)
    {
        throw new NotImplementedException();
    }

    protected override Type SupportedType { get; }
}