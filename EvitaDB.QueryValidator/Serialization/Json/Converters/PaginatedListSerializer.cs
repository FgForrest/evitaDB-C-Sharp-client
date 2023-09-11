using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using Newtonsoft.Json;

using static EvitaDB.QueryValidator.Utils.ResponseSerializerUtils;

namespace EvitaDB.QueryValidator.Serialization.Json.Converters;

public class PaginatedListSerializer : JsonConverter<PaginatedList<ISealedEntity>>
{
    public override void WriteJson(JsonWriter writer, PaginatedList<ISealedEntity>? value, JsonSerializer serializer)
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
        if (!IsDefaultValue(value.First))
        {
            writer.WritePropertyName("first");
            serializer.Serialize(writer, value.First);
        }
        if (!IsDefaultValue(value.FirstPageItemNumber))
        {
            writer.WritePropertyName("firstPageItemNumber");
            serializer.Serialize(writer, value.FirstPageItemNumber);
        }
        if (!IsDefaultValue(value.Last))
        {
            writer.WritePropertyName("last");
            serializer.Serialize(writer, value.Last);
        }
        if (!IsDefaultValue(value.LastPageItemNumber))
        {
            writer.WritePropertyName("lastPageItemNumber");
            serializer.Serialize(writer, value.LastPageItemNumber);
        }
        if (!IsDefaultValue(value.LastPageNumber))
        {
            writer.WritePropertyName("lastPageNumber");
            serializer.Serialize(writer, value.LastPageNumber);
        }
        if (!IsDefaultValue(value.PageNumber))
        {
            writer.WritePropertyName("pageNumber");
            serializer.Serialize(writer, value.PageNumber);
        }
        if (!IsDefaultValue(value.PageSize))
        {
            writer.WritePropertyName("pageSize");
            serializer.Serialize(writer, value.PageSize);
        }
        if (!IsDefaultValue(value.SinglePage))
        {
            writer.WritePropertyName("singlePage");
            serializer.Serialize(writer, value.SinglePage);
        }
        if (!IsDefaultValue(value.TotalRecordCount))
        {
            writer.WritePropertyName("totalRecordCount");
            serializer.Serialize(writer, value.TotalRecordCount);
        }
        writer.WriteEndObject();
    }

    public override PaginatedList<ISealedEntity> ReadJson(JsonReader reader, Type objectType, PaginatedList<ISealedEntity>? existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        throw new NotSupportedException();
    }
}