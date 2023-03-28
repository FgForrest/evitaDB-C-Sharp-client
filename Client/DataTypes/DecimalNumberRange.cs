namespace Client.DataTypes;

public class DecimalNumberRange : NumberRange<decimal>
{
    public static DecimalNumberRange Between(decimal from, decimal to) => new DecimalNumberRange(from, to);

    public static DecimalNumberRange Between(decimal from, decimal to, int retainedDecimalPlaces) =>
        new DecimalNumberRange(from, to, retainedDecimalPlaces);
    
    public new static DecimalNumberRange From(decimal from) => new DecimalNumberRange(from, null);
    
    public new static DecimalNumberRange From(decimal from, int retainedDecimalPlaces) =>
        new DecimalNumberRange(from, null, retainedDecimalPlaces);
    
    public new static DecimalNumberRange To(decimal to) => new DecimalNumberRange(null, to);
    
    public new static DecimalNumberRange To(decimal to, int retainedDecimalPlaces) =>
        new DecimalNumberRange(null, to, retainedDecimalPlaces);

    private DecimalNumberRange(decimal? from, decimal? to, int? retainedDecimalPlaces, long fromToCompare,
        long toToCompare) : base(from, to, retainedDecimalPlaces, fromToCompare, toToCompare)
    {
    }

    private DecimalNumberRange(decimal? from, decimal? to) : base(from, to, null,
        ToComparableLong(from, ResolveDefaultRetainedDecimalPlaces(from, to), long.MinValue),
        ToComparableLong(to, ResolveDefaultRetainedDecimalPlaces(from, to), long.MaxValue))
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
    }

    private DecimalNumberRange(decimal? from, decimal? to, int retainedDecimalPlaces) : base(from, to,
        retainedDecimalPlaces,
        ToComparableLong(from, retainedDecimalPlaces, long.MinValue),
        ToComparableLong(to, retainedDecimalPlaces, long.MaxValue))
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThanTo(from, to);
    }


    protected override long ToComparableLong(decimal? valueToCheck, long defaultValue)
    {
        return ToComparableLong(valueToCheck, RetainedDecimalPlaces ?? 0, defaultValue);
    }

    public static long ToComparableLong(decimal theValue, int retainedDecimalPlaces)
    {
        return (long) (decimal
            .Multiply(decimal.Round(theValue, retainedDecimalPlaces, MidpointRounding.AwayFromZero),
                (decimal) Math.Pow(10, retainedDecimalPlaces)));
    }

    public static long ToComparableLong(decimal? theValue, int retainedDecimalPlaces, long nullValue)
    {
        return theValue.HasValue ? ToComparableLong(theValue.Value, retainedDecimalPlaces) : nullValue;
    }

    protected override Type SupportedType => typeof(decimal);

    private static int ResolveDefaultRetainedDecimalPlaces(decimal? from, decimal? to)
    {
        if (from == null && to == null)
            return 0;
        if (from == null)
            return to!.Value.Scale;
        if (to == null)
            return from!.Value.Scale;
        return Math.Max(from!.Value.Scale, to!.Value.Scale);
    }
}