using Client.Utils;

namespace Client.Queries.Filter;

public class AttributeInRange<T, TNumber> : AbstractAttributeFilterConstraintLeaf
    where T : IComparable where TNumber : struct, IComparable
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

    public TNumber? TheValue => Arguments is [_, TNumber theValue and (byte or short or int or long or decimal)]
        ? theValue
        : null;
}