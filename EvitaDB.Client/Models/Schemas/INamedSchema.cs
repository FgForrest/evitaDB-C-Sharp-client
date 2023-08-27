using Client.Utils;

namespace Client.Models.Schemas;

public interface INamedSchema
{
    string Name { get; }
    string? Description { get; }
    IDictionary<NamingConvention, string> NameVariants { get; }
    string GetNameVariant(NamingConvention namingConvention);
}