namespace Client.DataTypes;

public class IntegerNumberRange : NumberRange<int>
{
    public IntegerNumberRange(int? from, int? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare)
    {
    }

    protected override long ToComparableLong(int? valueToCheck, long defaultValue)
    {
        throw new NotImplementedException();
    }

    protected override Type SupportedType { get; }
}