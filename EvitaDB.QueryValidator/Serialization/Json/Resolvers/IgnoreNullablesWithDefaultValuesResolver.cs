using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EvitaDB.QueryValidator.Serialization.Json.Resolvers;

public class IgnoreNullablesWithDefaultValuesResolver : IgnoreEmptyEnumerableResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        if (Nullable.GetUnderlyingType(property.PropertyType!) is not null)
        {
            // is nullable
            property.ShouldSerialize = instance =>
            {
                var value = property.ValueProvider!.GetValue(instance);
                if (value is not null && value.GetType().IsValueType)
                {
                    ValueType valueType = (ValueType)value;
                    return !valueType.Equals(Activator.CreateInstance(value.GetType()));
                }
                return true;
            };
        }

        return property;
    }
}