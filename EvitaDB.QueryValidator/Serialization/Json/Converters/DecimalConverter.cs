using Newtonsoft.Json;

namespace EvitaDB.QueryValidator.Serialization.Json.Converters;

public class DecimalConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is decimal decimalValue)
        {
            writer.WriteValue(decimal.Parse(decimalValue.ToString("F2"))); // Example: Two decimal places
        }
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        // Implement if needed for deserialization
        throw new NotImplementedException();
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(decimal);
    }
}
