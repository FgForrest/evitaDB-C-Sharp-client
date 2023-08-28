using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace QueryValidator.Serialization.Json.Resolvers;

public class OrderPropertiesResolver : IgnoreNullablesWithDefaultValuesResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var @base = base.CreateProperties(type, memberSerialization);
        var ordered = @base.OrderBy(p => p.PropertyName).ToList();
        return ordered;
    }
}