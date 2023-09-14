using System.Collections;
using System.Reflection;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.DataTypes.Data;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EvitaDB.Client.Converters.DataTypes;

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

    public static object GetSerializableForm(object? obj)
    {
        if (EvitaDataTypes.IsSupportedType(obj?.GetType()))
        {
            return obj!;
        }
        return ConvertToGenericType(obj);
    }

    public static ComplexDataObject ConvertToGenericType<T>(T container)
    {
        IDataItem rootNode = ConvertToIDataItem(container);

        ComplexDataObject result = new ComplexDataObject(rootNode);
        Assert.IsTrue(
            !result.Empty,
            "No usable properties found on " + container.GetType().Name + ". This is probably a problem."
        );
        return result;
    }

    private static IDataItem? ConvertToIDataItem(object obj)
    {
        if (obj.GetType().IsValueType || (EvitaDataTypes.IsSupportedType(obj.GetType()) && !obj.GetType().IsArray))
        {
            // The object is a value type, so create a DataItemValue
            return new DataItemValue(obj);
        }
        // Use reflection to get the properties of the input object
        PropertyInfo[] properties = obj.GetType().GetProperties();

        // Check if the object is a collection type
        if (obj is IList list)
        {
            // The object is a list, so create a DataItemArray
            List<IDataItem> dataItemArray = new List<IDataItem>();

            // Add each item in the list to the DataItemArray
            foreach (object item in list)
            {
                dataItemArray.Add(ConvertToIDataItem(item));
            }

            return new DataItemArray(dataItemArray.ToArray());
        }

        if (obj is IDictionary dictionary)
        {
            // The object is a dictionary, so create a DataItemMap
            Dictionary<string, IDataItem> dataItemMap = new Dictionary<string, IDataItem>();

            // Add each key-value pair in the dictionary to the DataItemMap
            foreach (DictionaryEntry entry in dictionary)
            {
                dataItemMap.Add(entry.Key.ToString(), ConvertToIDataItem(entry.Value));
            }

            return new DataItemMap(dataItemMap);
        }

        if (properties.Length > 0)
        {
            // The object is a complex type, so create a DataItemMap
            Dictionary<string, IDataItem> dataItemMap = new Dictionary<string, IDataItem>();

            // Iterate over the properties
            foreach (PropertyInfo property in properties)
            {
                // Get the value of the property
                object? value = property.GetValue(obj);

                // Add the property to the DataItemMap
                dataItemMap.Add(property.Name, ConvertToIDataItem(value));
            }

            return new DataItemMap(dataItemMap);
        }

        return null;
    }
    
    public static object? ConvertFromComplexDataObject(ComplexDataObject complexDataObject, Type type)
    {
        return ConvertFromIComplexDataObject(complexDataObject.Root, type);
    }
    
    private static object? ConvertFromIComplexDataObject(IDataItem dataItem, Type type)
    {
        if (dataItem is DataItemValue dataItemValue)
        {
            // Convert the DataItemValue to the specified type
            return Convert.ChangeType(dataItemValue.Value, type);
        }

        if (dataItem is DataItemArray dataItemArray)
        {
            // Create a list to hold the elements
            Type arrayType = type.IsArray ? type.GetElementType() : type;
            Array array = (Array)Activator.CreateInstance(arrayType.MakeArrayType(), dataItemArray.Children.Length);

            // Convert each IDataItem in the DataItemArray and add it to the list
            for (int i = 0; i < dataItemArray.Children.Length; i++)
            {
                IDataItem? item = dataItemArray.Children[i];
                array.SetValue(ConvertFromIComplexDataObject(item, type.GetElementType()), i);
            }

            return array;
        }

        if (dataItem is DataItemMap dataItemMap)
        {
            // Create an instance of the specified type
            object? obj = Activator.CreateInstance(type);
            // Use reflection to set the properties of the new object
            foreach (KeyValuePair<string, IDataItem> entry in dataItemMap.ChildrenIndex)
            {
                PropertyInfo? property = type.GetProperty(StringUtils.Capitalize(entry.Key));
                if (property != null)
                {
                    property.SetValue(obj, ConvertFromIComplexDataObject(entry.Value, property.PropertyType));
                }
            }

            return obj;
        }
        throw new ArgumentException("Unsupported IDataItem type");
    }
}