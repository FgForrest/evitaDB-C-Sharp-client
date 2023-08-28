using Client.DataTypes;
using Client.Models.Data.Structure;
using Newtonsoft.Json;

using static QueryValidator.Utils.ResponseSerializerUtils;

namespace QueryValidator.Serialization.Json.Converters;

public class StripListSerializer : JsonConverter<StripList<SealedEntity>>
{
    public override void WriteJson(JsonWriter writer, StripList<SealedEntity>? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            return;
        }
        writer.WriteStartObject();
        writer.WritePropertyName("data");
        writer.WriteStartArray();
        if (value.Data is not null)
        {
            foreach (var entity in value.Data)
            {
                serializer.Serialize(writer, entity);
            }
        }
        writer.WriteEndArray();

        if (!IsDefaultValue(value.Limit))
        {
            writer.WritePropertyName("limit");
            serializer.Serialize(writer, value.Limit);
        }
        if (!IsDefaultValue(value.Offset))
        {
            writer.WritePropertyName("offset");
            serializer.Serialize(writer, value.Offset);
        }
        if (!IsDefaultValue(value.TotalRecordCount))
        {
            writer.WritePropertyName("totalRecordCount");
            serializer.Serialize(writer, value.TotalRecordCount);
        }
        writer.WriteEndObject();
    }

    public override StripList<SealedEntity>? ReadJson(JsonReader reader, Type objectType, StripList<SealedEntity>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}