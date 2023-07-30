using System.Globalization;
using System.Text.RegularExpressions;
using Client.DataTypes;
using Client.DataTypes.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.Converters.DataTypes;

public class JsonToComplexDataObjectConverter
{
    private static readonly Regex LongNumber = new Regex(@"^\d+$");
    private static readonly Regex BigDecimalNumber = new Regex(@"^\d.\d+$");

    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    private static IDataItem? ConvertToDataItem(JToken jsonNode)
    {
        if (jsonNode.Type == JTokenType.Null)
        {
            return null;
        }

        if (jsonNode is JArray arrayNode)
        {
            var outputElements = arrayNode.Select(ConvertToDataItem).ToArray();
            return new DataItemArray(outputElements);
        }

        if (jsonNode is JObject objectNode)
        {
            var outputElements = objectNode.Properties().ToDictionary(
                property => property.Name,
                property => ConvertToDataItem(property.Value)
            );
            return new DataItemMap(outputElements);
        }

        if (jsonNode.Type == JTokenType.Boolean)
        {
            return new DataItemValue(jsonNode.Value<bool>());
        }

        if (jsonNode.Type == JTokenType.Integer)
        {
            var value = jsonNode.Value<long>();

            if (value is >= byte.MinValue and <= byte.MaxValue)
            {
                return new DataItemValue((byte) value);
            }

            if (value is >= short.MinValue and <= short.MaxValue)
            {
                return new DataItemValue((short) value);
            }

            if (value is >= int.MinValue and <= int.MaxValue)
            {
                return new DataItemValue((int) value);
            }

            return new DataItemValue(value);
        }

        if (jsonNode.Type == JTokenType.Float)
        {
            return new DataItemValue(jsonNode.Value<decimal>());
        }

        if (jsonNode.Type == JTokenType.String)
        {
            var value = jsonNode.Value<string>();
            if (LongNumber.IsMatch(value))
            {
                return new DataItemValue(long.Parse(value));
            }

            if (BigDecimalNumber.IsMatch(value))
            {
                return new DataItemValue(decimal.Parse(value, CultureInfo.InvariantCulture));
            }

            return new DataItemValue(value);
        }

        throw new InvalidOperationException("Unexpected input JSON format.");
    }

    public ComplexDataObject FromJson(string jsonString)
    {
        var jsonNode = JToken.Parse(jsonString);
        return new ComplexDataObject(ConvertToDataItem(jsonNode) ?? throw new InvalidOperationException());
    }

    public ComplexDataObject FromMap(IDictionary<string, object> map)
    {
        var jsonNode = JToken.FromObject(map, JsonSerializer.Create(_settings));
        return new ComplexDataObject(ConvertToDataItem(jsonNode) ?? throw new InvalidOperationException());
    }
}