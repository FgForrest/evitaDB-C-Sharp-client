using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Client.Utils;

public class StringUtils
{
    private static readonly Regex StringWithCaseWordSplittingPattern = new Regex("([^\\s\\-_A-Z]+)|([A-Z]+[^\\s\\-_A-Z]*)");

    public static string HashChars(string toHash)
    {
        var inputBytes = Encoding.ASCII.GetBytes(toHash);
        var hashBytes = MD5.Create().ComputeHash(inputBytes);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    public static string Uncapitalize(string str) {
        if (str.Length == 0) {
            return str;
        }

        var firstCharacter = char.ToLower(str.ElementAt(0));
        if (firstCharacter == str.ElementAt(0)) {
            return str;
        }

        var chars = str.ToCharArray();
        chars[0] = firstCharacter;
        return new string(chars, 0, chars.Length);
    }
    
    public static string? Capitalize(string? str) {
        if (string.IsNullOrEmpty(str)) {
            return str;
        }

        char firstCharacter = char.ToUpper(str.ElementAt(0));
        if (firstCharacter == str.ElementAt(0)) {
            return str;
        }

        char[] chars = str.ToCharArray();
        chars[0] = firstCharacter;
        return new string(chars, 0, chars.Length);
    }
    
    public static string ToSpecificCase(string s, NamingConvention targetNamingConvention) {
        return targetNamingConvention switch {
            NamingConvention.CamelCase => StringUtils.ToCamelCase(s),
            NamingConvention.PascalCase => StringUtils.ToPascalCase(s),
            NamingConvention.SnakeCase => StringUtils.ToSnakeCase(s),
            NamingConvention.UpperSnakeCase => StringUtils.ToUpperSnakeCase(s),
            NamingConvention.KebabCase => StringUtils.ToKebabCase(s)
        };
    }
    
    public static string ToCamelCase(string s) {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return Uncapitalize(string.Join("", words.Select(word => word.ToLower()).ToList()));
    }
    
    public static string ToPascalCase(string s) {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("", words.Select(word => Capitalize(word.ToLower())));
    }
    
    public static string ToSnakeCase(string s) {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("_", words.Select(word => word.ToLower()));
    }
    
    public static string ToUpperSnakeCase(string s) {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("_", words.Select(word => word.ToUpper()));
    }
    
    public static string ToKebabCase(string s) {
        IEnumerable<string> words = SplitStringWithCaseIntoWords(s);
        return string.Join("-", words.Select(word=>word.ToLower()));
    }
    
    private static IEnumerable<string> SplitStringWithCaseIntoWords(string s) {
        if (string.IsNullOrWhiteSpace(s)) {
            return new List<string>();
        }

        s = Regex.Replace(s, "[.:+\\-@/\\\\|`~]", " ");

        var list = StringWithCaseWordSplittingPattern.Matches(s)
            .Select(x=>x.Groups)
            .Select(x=>x.Values)
            .Select(x=>x.ToString())
            .ToList();
        list.RemoveAll(x=>x == null);
        return list!;
    }
}