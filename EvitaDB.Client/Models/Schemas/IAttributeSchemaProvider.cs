using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public interface IAttributeSchemaProvider<T> where T : IAttributeSchema
{
    IDictionary<string, T> GetAttributes();
    T? GetAttribute(string name);
    T? GetAttributeByName(string name, NamingConvention namingConvention);
}