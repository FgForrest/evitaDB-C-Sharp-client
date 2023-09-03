using System.Reflection;
using EvitaDB.Client.Utils;

namespace EvitaDB.QueryValidator.Utils;

public static class ResponseSerializerUtils
{
    public static object? ExtractValueFrom(object theObject, string[] sourceVariableParts)
    {
        if (theObject.GetType().IsAssignableToGenericType(typeof(IDictionary<,>)))
        {
            IDictionary<object, object> map = ConversionUtils.ConvertObjectToDictionary<object>(theObject);

            foreach (var (key, value) in map)
            {
                string keyAsString;
                if (key is Type klass)
                {
                    keyAsString = klass.Name;
                }
                else
                {
                    keyAsString = Convert.ToString(key)!;
                }

                if (sourceVariableParts[0].Equals(keyAsString))
                {
                    if (sourceVariableParts.Length > 1)
                    {
                        return ExtractValueFrom(
                            value,
                            sourceVariableParts.Skip(1).ToArray()
                        );
                    }

                    return value;
                }
            }

            return null;
        }

        if (theObject.GetType().IsAssignableToGenericType(typeof(IList<>)))
        {
            IList<object> list = ConversionUtils.ConvertObjectToList<object>(theObject);

            try
            {
                int index = int.Parse(sourceVariableParts[0]);
                object? theValue = list.ElementAtOrDefault(index);
                if (theValue is null)
                {
                    return null;
                }

                if (sourceVariableParts.Length > 1)
                {
                    return ExtractValueFrom(
                        theValue,
                        sourceVariableParts.Skip(1).ToArray()
                    );
                }

                return theValue;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        
        var getValue = GetPropertyValue(theObject, sourceVariableParts[0]);
        if (getValue == null)
        {
            throw new Exception("Cannot find or read getter for " + sourceVariableParts[0] + " on `" +
                                theObject.GetType() +
                                "`");
        }

        try
        {
            if (sourceVariableParts.Length > 1)
            {
                return ExtractValueFrom(
                    getValue,
                    sourceVariableParts.Skip(1).ToArray()
                );
            }

            return getValue;
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    private static object? GetPropertyValue(object obj, string sourceVariablePart)
    {
        // Get the type of the object
        Type type = obj.GetType();
        
        string propertyName = EvitaDB.Client.Utils.StringUtils.Capitalize(sourceVariablePart)!;
        PropertyInfo? propertyInfo = type.GetProperties().FirstOrDefault(x=>x.Name == propertyName);

        // Check if the property exists and has a get method
        if (propertyInfo != null && propertyInfo.CanRead)
        {
            // Get the value of the property
            return propertyInfo.GetValue(obj);
        }
        
        string fieldName = "_" +sourceVariablePart;
        FieldInfo? fieldInfo = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            return fieldInfo.GetValue(obj);
        }
        
        // The property does not exist or does not have a get method
        return null;
    }
    
    public static bool IsDefaultValue<T>(T value)
    {
        return EqualityComparer<T>.Default.Equals(value, default(T));
    }
}