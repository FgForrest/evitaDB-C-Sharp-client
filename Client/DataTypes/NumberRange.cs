using Client.Exceptions;
using Client.Utils;

namespace Client.DataTypes;

public abstract class NumberRange<T> : Range<T?> where T : struct, IComparable<T>, IEquatable<T>, IConvertible
{
    public int? RetainedDecimalPlaces { get; init; }
    protected NumberRange(T? from, T? to, int? retainedDecimalPlaces, long fromToCompare, long toToCompare)
    {
        PreciseFrom = from;
        PreciseTo = to;
        RetainedDecimalPlaces = retainedDecimalPlaces;
        From = fromToCompare;
        To = toToCompare;
    }

    protected abstract long ToComparableLong(T? valueToCheck, long defaultValue);
    
    protected abstract Type SupportedType { get; }

    protected override bool IsWithin(T? valueToCheck)
    {
        Assert.NotNull(valueToCheck, "Cannot resolve within range with NULL value!");
        long valueToCompare = ToComparableLong((T? )EvitaDataTypes.ToTargetType(valueToCheck!, SupportedType), 0L);
        return From <= valueToCompare && valueToCompare <= To;
    }
    
    protected void AssertNotFloatingPointType(T? from, string argName) {
        if (typeof(T) == typeof(float) || typeof(T) == typeof(double)) {
            throw new EvitaInvalidUsageException("For " + argName + " number with floating point use decimal that keeps the precision!");
        }
    }

    protected void AssertEitherBoundaryNotNull(T? from, T? to) {
        if (from == null && to == null) {
            throw new EvitaInvalidUsageException("From and to cannot be both null at the same time in NumberRange type!");
        }
    }

    protected void AssertFromLesserThanTo(T? from, T? to) {
        if (!(from == null || to == null || ((IComparable) from.Value).CompareTo(to) <= 0)) {
            throw new EvitaInvalidUsageException("From must be before or equals to to!");
        }
    }

    public override string ToString()
    {
        T from = PreciseFrom ?? default;
        T to = PreciseTo ?? default;
        return OpenChar + from + IntervalJoin + to + CloseChar;
    }
}