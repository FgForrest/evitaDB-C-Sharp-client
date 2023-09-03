using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Attributes;

public abstract class ApplyDeltaAttributeMutation : AttributeMutation
{
    protected ApplyDeltaAttributeMutation(AttributeKey attributeKey) : base(attributeKey)
    {
    }
}

public class ApplyDeltaAttributeMutation<T> : ApplyDeltaAttributeMutation
    where T : struct, IComparable<T>, IEquatable<T>, IConvertible
{
    public T Delta { get; }
    public NumberRange<T>? RequiredRangeAfterApplication { get; }

    public ApplyDeltaAttributeMutation(AttributeKey attributeKey, T delta) : base(attributeKey)
    {
        Delta = delta;
        RequiredRangeAfterApplication = null;
    }

    public ApplyDeltaAttributeMutation(string attributeName, T delta) : base(new AttributeKey(attributeName))
    {
        Delta = delta;
        RequiredRangeAfterApplication = null;
    }

    public ApplyDeltaAttributeMutation(string attributeName, CultureInfo locale, T delta) : base(
        new AttributeKey(attributeName, locale))
    {
        Delta = delta;
        RequiredRangeAfterApplication = null;
    }

    public ApplyDeltaAttributeMutation(AttributeKey attributeKey, T delta,
        NumberRange<T>? requiredRangeAfterApplication) : base(attributeKey)
    {
        Delta = delta;
        RequiredRangeAfterApplication = requiredRangeAfterApplication;
    }

    public ApplyDeltaAttributeMutation(string attributeName, T delta, @NumberRange<T>? requiredRangeAfterApplication) :
        base(new AttributeKey(attributeName))
    {
        Delta = delta;
        RequiredRangeAfterApplication = requiredRangeAfterApplication;
    }

    public ApplyDeltaAttributeMutation(string attributeName, CultureInfo locale, T delta,
        NumberRange<T>? requiredRangeAfterApplication) : base(new AttributeKey(attributeName, locale))
    {
        Delta = delta;
        RequiredRangeAfterApplication = requiredRangeAfterApplication;
    }


    public override AttributeValue MutateLocal(IEntitySchema entitySchema, AttributeValue? existingAttributeValue)
    {
        Assert.IsTrue(
            existingAttributeValue is {Value: not null},
            "Cannot apply delta to attribute " + AttributeKey.AttributeName + " when it doesn't exist!"
        );
        Assert.IsTrue(
            existingAttributeValue?.Value is T,
            "Cannot apply delta to attribute " + AttributeKey.AttributeName + " when its value is " +
            existingAttributeValue?.Value?.GetType().Name
        );
        T existingValue = (T) existingAttributeValue?.Value!;
        T newValue = existingValue switch
        {
            decimal decimalValue => (T) Convert.ChangeType(
                Convert.ToDecimal((decimal) Convert.ChangeType(existingValue, typeof(decimal)) + decimalValue),
                typeof(T)),
            byte byteValue => (T) Convert.ChangeType(
                Convert.ToByte((byte) Convert.ChangeType(existingValue, typeof(byte)) + byteValue), typeof(T)),
            short shortValue => (T) Convert.ChangeType(
                Convert.ToInt16((short) Convert.ChangeType(existingValue, typeof(short)) + shortValue), typeof(T)),
            int intValue => (T) Convert.ChangeType(
                Convert.ToInt32((int) Convert.ChangeType(existingValue, typeof(int)) + intValue), typeof(T)),
            long longValue => (T) Convert.ChangeType(
                Convert.ToInt64((long) Convert.ChangeType(existingValue, typeof(long)) + longValue), typeof(T)),
            _ => throw new InvalidMutationException("Unknown Evita data type: " + existingValue.GetType().Name)
        };

        if (RequiredRangeAfterApplication != null)
        {
            Assert.IsTrue(
                RequiredRangeAfterApplication.IsWithin(newValue),
                () => new InvalidMutationException(
                    "Applying delta " + Delta + " on " + existingValue + " produced result " + newValue +
                    " which is out of specified range " + RequiredRangeAfterApplication + "!"
                )
            );
        }

        return new AttributeValue(existingAttributeValue.Version + 1, AttributeKey, newValue);
    }
}