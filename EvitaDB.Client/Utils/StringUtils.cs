using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EvitaDB.Client.Utils;

public class StringUtils
{
    private static readonly Regex StringWithCaseWordSplittingPattern =
        new Regex("([^\\s\\-_A-Z]+)|([A-Z]+[^\\s\\-_A-Z]*)");

    public static string HashChars(string toHash)
    {
        var inputBytes = Encoding.ASCII.GetBytes(toHash);
        var hashBytes = MD5.Create().ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public static string Uncapitalize(string str)
    {
        if (str.Length == 0)
        {
            return str;
        }

        var firstCharacter = char.ToLower(str.ElementAt(0));
        if (firstCharacter == str.ElementAt(0))
        {
            return str;
        }

        var chars = str.ToCharArray();
        chars[0] = firstCharacter;
        return new string(chars, 0, chars.Length);
    }

    public static string? Capitalize(string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        char firstCharacter = char.ToUpper(str.ElementAt(0));
        if (firstCharacter == str.ElementAt(0))
        {
            return str;
        }

        char[] chars = str.ToCharArray();
        chars[0] = firstCharacter;
        return new string(chars, 0, chars.Length);
    }

    public static string ToSpecificCase(string s, NamingConvention targetNamingConvention)
    {
        return targetNamingConvention switch
        {
            NamingConvention.CamelCase => ToCamelCase(s),
            NamingConvention.PascalCase => ToPascalCase(s),
            NamingConvention.SnakeCase => ToSnakeCase(s),
            NamingConvention.UpperSnakeCase => ToUpperSnakeCase(s),
            NamingConvention.KebabCase => ToKebabCase(s),
            _ => throw new ArgumentOutOfRangeException(nameof(targetNamingConvention), targetNamingConvention, null)
        };
    }

    public static string ToCamelCase(string s)
    {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return Uncapitalize(string.Join("", words.Select(word => word.ToLower()).ToList()));
    }

    public static string ToPascalCase(string s)
    {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("", words.Select(word => Capitalize(word.ToLower())));
    }

    public static string ToSnakeCase(string s)
    {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("_", words.Select(word => word.ToLower()));
    }

    public static string ToUpperSnakeCase(string s)
    {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("_", words.Select(word => word.ToUpper()));
    }

    public static string ToKebabCase(string s)
    {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("-", words.Select(word => word.ToLower()));
    }

    public static List<string> SplitStringWithCaseIntoWords(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return new List<string>();
        }

        s = Regex.Replace(s, "[.:+\\-@/\\\\|`~]", " ");

        return StringWithCaseWordSplittingPattern.Matches(s).Select(m => m.Value).ToList();
    }
}