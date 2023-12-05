using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `inRange` is query that compares value of the attribute with name passed in first argument with the date
/// and time passed in the second argument. First argument must be <see cref="string"/>, second argument must be
/// <see cref="DateTimeOffset"/> type. If second argument is not passed - current date and time (now) is used.
/// Type of the attribute value must implement <see cref="Range{T}"/> class.
/// Function returns true if second argument is greater than or equal to range start (from), and is lesser than
/// or equal to range end (to).
/// Example:
/// <code>
/// inRange("valid", 2020-07-30T20:37:50+00:00)
/// inRange("age", 18)
/// </code>
/// Function supports attribute arrays and when attribute is of array type `inRange` returns true if any of attribute
/// values has range, that envelopes the passed value the value in the query. If we have the attribute `age` with value
/// `[[18, 25],[60,65]]` all these constraints will match:
/// <code>
/// inRange("age", 18)
/// inRange("age", 24)
/// inRange("age", 63)
/// </code>
/// </summary>
public class AttributeInRange<T> : AbstractAttributeFilterConstraintLeaf
    where T : IComparable
{
    private AttributeInRange(params object?[] arguments) : base(arguments)
    {
    }

    private AttributeInRange(string attributeName, T value) : base(attributeName, value)
    {
        Assert.IsTrue(
            value is DateTimeOffset or int or long or decimal or short or byte,
            $"Value is query {this} has unsupported type {typeof(T)}. Supported are: DateTimeOffset, int, long, decimal, short, byte");
    }

    public AttributeInRange(string attributeName) : base(attributeName)
    {
    }

    public AttributeInRange(string attributeName, DateTimeOffset theMoment) : base(attributeName, theMoment)
    {
    }

    public AttributeInRange(string attributeName, int value) : base(attributeName, value)
    {
    }

    public AttributeInRange(string attributeName, long value) : base(attributeName, value)
    {
    }

    public AttributeInRange(string attributeName, decimal value) : base(attributeName, value)
    {
    }

    public AttributeInRange(string attributeName, short value) : base(attributeName, value)
    {
    }

    public AttributeInRange(string attributeName, byte value) : base(attributeName, value)
    {
    }

    public object? UnknownArgument => Arguments.Length == 2 ? Arguments[1] : null;

    public DateTimeOffset? TheMoment => Arguments is [_, DateTimeOffset theMoment] ? theMoment : null;

    public TNumber? TheValue<TNumber>() where TNumber : struct, IComparable => Arguments is [_, TNumber theValue and (byte or short or int or long or decimal)]
        ? theValue
        : null;
}
