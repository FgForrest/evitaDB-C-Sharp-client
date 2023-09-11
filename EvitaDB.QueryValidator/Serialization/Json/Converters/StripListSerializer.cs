using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using Newtonsoft.Json;

using static EvitaDB.QueryValidator.Utils.ResponseSerializerUtils;

namespace EvitaDB.QueryValidator.Serialization.Json.Converters;

public class StripListSerializer : JsonConverter<StripList<ISealedEntity>>
{
    public override void WriteJson(JsonWriter writer, StripList<ISealedEntity>? value, JsonSerializer serializer)
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

    public override StripList<ISealedEntity> ReadJson(JsonReader reader, Type objectType, StripList<ISealedEntity>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}