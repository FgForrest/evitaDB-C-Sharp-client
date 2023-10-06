using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public interface INamedSchema
{
    string Name { get; }
    string? Description { get; }
    IDictionary<NamingConvention, string?> NameVariants { get; }
    string? GetNameVariant(NamingConvention namingConvention);
}