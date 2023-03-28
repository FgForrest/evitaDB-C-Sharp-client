namespace Client.DataTypes;

public class LongNumberRange : NumberRange<long>
{
    public LongNumberRange(long? from, long? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare)
    {
    }

    protected override long ToComparableLong(long? valueToCheck, long defaultValue)
    {
        throw new NotImplementedException();
    }

    protected override Type SupportedType { get; }
}