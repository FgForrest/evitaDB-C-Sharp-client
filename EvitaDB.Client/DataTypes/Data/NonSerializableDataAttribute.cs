namespace EvitaDB.Client.DataTypes.Data;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class NonSerializableDataAttribute : Attribute
{
}