using System.Collections.Immutable;

namespace EvitaDB.Client.Utils;

public enum NamingConvention
{
    CamelCase,
    PascalCase,
    SnakeCase,
    UpperSnakeCase,
    KebabCase,
}

public static class NamingConventionHelper
{
    private static NamingConvention[] AllConventions =>
        Enum.GetValues<NamingConvention>();

    public static IDictionary<NamingConvention, string?> Generate(string name)
    {
        return AllConventions.ToDictionary(
            x => x,
            x => StringUtils.ToSpecificCase(name, x)
        ).ToImmutableDictionary(x=>x.Key, x=>x.Value)!;
    }
}