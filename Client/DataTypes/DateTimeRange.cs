using Client.Exceptions;

namespace Client.DataTypes;

public class DateTimeRange : Range<DateTimeOffset?>, IComparable<DateTimeRange>
{
    private DateTimeRange(DateTimeOffset? from, DateTimeOffset? to)
    {
        AssertEitherBoundaryNotNull(from, to);
        AssertFromLesserThatTo(from, to);
        From = ToComparableLong(from ?? DateTimeOffset.MinValue);
        To = ToComparableLong(to ?? DateTimeOffset.MaxValue);
        PreciseFrom = from;
        PreciseTo = to;
    }

    private void AssertEitherBoundaryNotNull(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (from is null && to is null)
        {
            throw new EvitaInvalidUsageException(
                "From and to cannot be both null at the same time in DateTimeRange type!");
        }
    }

    private void AssertFromLesserThatTo(DateTimeOffset? from, DateTimeOffset? to)
    {
        if (!(from is null || to is null || from.Equals(to) || from.Value.CompareTo(to.Value) <= 0))
            throw new EvitaInvalidUsageException("From must be before or equals to To!");
    }
    
    public static long ToComparableLong(DateTimeOffset theMoment) => theMoment.ToUnixTimeSeconds();
    
    public override bool IsWithin(DateTimeOffset? valueToCheck)
    {
        if (valueToCheck is null)
            return false;
        long comparedValue = ToComparableLong(valueToCheck.Value);
        return From <= comparedValue && comparedValue <= To;
    }

    public int CompareTo(DateTimeRange? other)
    {
        if (other is null)
            return 1;
        int leftBoundCompare = From.CompareTo(other.From);
        int rightBoundCompare = To.CompareTo(other.To);
        return leftBoundCompare != 0 ? leftBoundCompare : rightBoundCompare;
    }
    
    public bool ValidFor(DateTimeOffset theMoment)
    {
        long comparedValue = theMoment.ToUnixTimeSeconds();
        return From <= comparedValue && To >= comparedValue;
    }
    
    public static DateTimeRange Between(DateTimeOffset from, DateTimeOffset to)
    {
        return new DateTimeRange(from, to);
    }
    
    public static DateTimeRange Until(DateTimeOffset to)
    {
        return new DateTimeRange(null, to);
    }

    public static DateTimeRange Since(DateTimeOffset from)
    {
        return new DateTimeRange(from, null);
    }
}