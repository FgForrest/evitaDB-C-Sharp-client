using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EvitaDB.QueryValidator.Serialization.Json.Resolvers;

public class OrderPropertiesResolver : IgnoreNullablesWithDefaultValuesResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> baseProperty = base.CreateProperties(type, memberSerialization);
        return baseProperty.OrderBy(p => p.PropertyName, StringComparer.Ordinal).ToList();
    }
}