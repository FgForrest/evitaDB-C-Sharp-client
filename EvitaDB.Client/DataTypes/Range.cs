using EvitaDB.Client.Utils;

namespace EvitaDB.Client.DataTypes;

public abstract class Range<T>
{
    protected static string OpenChar => "[";
    protected static string CloseChar => "]";
    protected static string IntervalJoin => ",";

    protected long From { get; init; }
    protected long To { get; init; }
    public T? PreciseFrom { get; init; }
    public T? PreciseTo { get; init; }

    public abstract bool IsWithin(T valueToCheck);

    public bool Overlaps(Range<T> otherRange)
    {
        Assert.IsTrue(GetType() == otherRange.GetType(), $"Ranges {GetType().Name} and {otherRange.GetType().Name} are not comparable!");
        return (From >= otherRange.From && To <= otherRange.To) ||
               (From <= otherRange.From && To >= otherRange.To) ||
               (From >= otherRange.From && From <= otherRange.To) ||
               (To <= otherRange.To && To >= otherRange.From);
    }
}