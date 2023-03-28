namespace Client.DataTypes;

public class ShortNumberRange : NumberRange<short>
{
    public ShortNumberRange(short? from, short? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare)
    {
    }

    protected override long ToComparableLong(short? valueToCheck, long defaultValue)
    {
        throw new NotImplementedException();
    }

    protected override Type SupportedType { get; }
}