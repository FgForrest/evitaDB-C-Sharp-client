using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Serialization;

namespace EvitaDB.QueryValidator.Serialization.Json.Binders;

public class AllowedSerializationBinder : ISerializationBinder
{
    private readonly Type[] _allowedTypes;
    public AllowedSerializationBinder(params Type[] allowedTypes)
    {
        _allowedTypes = allowedTypes;
    }
    
    public Type BindToType(string? assemblyName, string typeName)
    {
        var type = Type.GetType($"{typeName}, {assemblyName}");
        if (_allowedTypes.Any(t => t.IsAssignableFrom(type)))
        {
            return type!;
        }

        throw new Exception($"Type {typeName} is not allowed to be deserialized");
    }

    public void BindToName(Type serializedType, [UnscopedRef] out string? assemblyName, [UnscopedRef] out string? typeName)
    {
        assemblyName = null;
        typeName = serializedType.FullName;
    }
}