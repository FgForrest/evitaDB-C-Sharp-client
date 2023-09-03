using EvitaDB.Client.Utils;
using EvitaDB.QueryValidator.Utils;
using Newtonsoft.Json;

namespace EvitaDB.QueryValidator.Serialization.Json.Converters;

public class OrderedJsonSerializer : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType.IsAssignableToGenericType(typeof(IDictionary<,>));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var dictionary = new Dictionary<string, object>();
        serializer.Populate(reader, dictionary);
        return dictionary;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            return;
        }
        if (value.GetType().IsAssignableToGenericType(typeof(IDictionary<,>)))
        {
            IDictionary<string, object>? dictionary = ConversionUtils.ConvertObjectToDictionary<string>(value);
            writer.WriteStartObject();
            foreach (var kvp in dictionary.OrderBy(kvp => kvp.Key))
            {
                writer.WritePropertyName(kvp.Key);
                if (kvp.Value.GetType().IsAssignableToGenericType(typeof(IList<>)))
                {
                    WriteJson(writer, kvp.Value, serializer);
                }
                else
                {
                    serializer.Serialize(writer, kvp.Value);
                }
            }
            writer.WriteEndObject();
        }
        else if (value.GetType().IsAssignableToGenericType(typeof(IList<>)))
        {
            IList<object>? list = ConversionUtils.ConvertObjectToList<object>(value);
            writer.WriteStartArray();
            foreach (var kvp in list.OrderBy(kvp => kvp))
            {
                serializer.Serialize(writer, kvp);
            }
            writer.WriteEndArray();
        }
    }
}