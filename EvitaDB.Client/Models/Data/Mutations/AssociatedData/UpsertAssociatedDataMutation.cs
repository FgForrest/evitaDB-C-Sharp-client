using System.Globalization;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.AssociatedData;

public class UpsertAssociatedDataMutation : AssociatedDataMutation
{
    public object Value { get; }

    public UpsertAssociatedDataMutation(AssociatedDataKey associatedDataKey, object value) : base(associatedDataKey)
    {
        Value = value;
    }

    public UpsertAssociatedDataMutation(string associatedDataName, object value) : base(
        new AssociatedDataKey(associatedDataName))
    {
        Value = value;
    }

    public UpsertAssociatedDataMutation(string associatedDataName, CultureInfo locale, object value) : base(
        new AssociatedDataKey(associatedDataName, locale))
    {
        Value = value;
    }

    public override AssociatedDataValue MutateLocal(IEntitySchema entitySchema, AssociatedDataValue? existingValue)
    {
        if (existingValue == null) {
            return new AssociatedDataValue(AssociatedDataKey, Value);
        }
        return !Equals(existingValue.Value, Value) ?
            new AssociatedDataValue(existingValue.Version + 1, AssociatedDataKey, Value) : existingValue;
    }
}