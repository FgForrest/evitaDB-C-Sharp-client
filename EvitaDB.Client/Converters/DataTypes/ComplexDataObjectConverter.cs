using Client.DataTypes;
using Client.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.Converters.DataTypes;

public static class ComplexDataObjectConverter
{
    public static object ConvertJsonToComplexDataObject(string associatedDataValueJson)
    {
        JsonToComplexDataObjectConverter converter = new JsonToComplexDataObjectConverter();
        try
        {
            return converter.FromJson(associatedDataValueJson);
        }
        catch (JsonException ex)
        {
            throw new EvitaInvalidUsageException("Invalid associated data json format.", ex);
        }
    }

    public static JToken ConvertComplexDataObjectToJson(ComplexDataObject complexDataObject)
    {
        ComplexDataObjectToJsonConverter converter = new ComplexDataObjectToJsonConverter();
        complexDataObject.Accept(converter);
        return converter.RootNode!;
    }
}