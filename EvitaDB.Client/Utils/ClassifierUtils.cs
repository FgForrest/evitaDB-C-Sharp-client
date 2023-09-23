using System.Text.RegularExpressions;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Utils;

public static class ClassifierUtils
{
    private const string CatalogNameRegex = @"^[A-Za-z0-9_-]{1,255}$";
    private const string SupportedFormatPattern = "(^[\\p{L}][\\w_.:+\\-@/\\\\|`~]*$)";

    public static void ValidateClassifierFormat(ClassifierType classifierType, string classifier)
    {
        Assert.IsTrue(!string.IsNullOrWhiteSpace(classifier),
            () => new InvalidClassifierFormatException(classifierType, classifier, "it is empty"));
        Assert.IsTrue(classifier.Equals(classifier.Replace(" ", "")),
            () => new InvalidClassifierFormatException(classifierType, classifier,
                "it contains leading or trailing whitespace"));
        Assert.IsTrue(!IsKeyword(classifierType, classifier),
            () => new InvalidClassifierFormatException(classifierType, classifier,
                "it is reserved keyword or can be converted into reserved keyword"));
        if (classifierType == ClassifierType.Catalog)
        {
            Assert.IsTrue(Regex.IsMatch(classifier, CatalogNameRegex),
                () => new InvalidClassifierFormatException(
                    classifierType, classifier,
                    "invalid name - only alphanumeric and these ASCII characters are allowed (with maximal length of 255 characters): _-"
                )
            );
        }
        else
        {
            Assert.IsTrue(Regex.IsMatch(classifier, SupportedFormatPattern),
                () => new InvalidClassifierFormatException(
                    classifierType, classifier,
                    "invalid name - only alphanumeric and these ASCII characters are allowed: _.:+-@/\\|`~"
                )
            );
        }
    }

    private static bool IsKeyword(ClassifierType classifierType, string classifier)
    {
        if (string.IsNullOrWhiteSpace(classifier))
        {
            return false;
        }

        return ReservedKeywords.TryGetValue(classifierType, out var keywords) && keywords.Any(keyword =>
            Enum.GetValues<NamingConvention>().Select(namingConvention => StringUtils.ToSpecificCase(keyword, namingConvention))
                .Any(kw => kw == classifier || kw == StringUtils.ToCamelCase(classifier) ||
                           kw == StringUtils.ToPascalCase(classifier) || kw == StringUtils.ToSnakeCase(classifier) ||
                           kw == StringUtils.ToUpperSnakeCase(classifier) ||
                           kw == StringUtils.ToKebabCase(classifier)));
    }

    private static readonly Dictionary<ClassifierType, ISet<string>> ReservedKeywords =
        new()
        {
            {
                ClassifierType.Catalog,
                new HashSet<string>
                {
                    "system" // would collide with special system API endpoints for managing evitaDB
                }
            },
            {
                ClassifierType.Entity,
                new HashSet<string>
                {
                    "catalog", // catalog queries in GraphQL, would collide with entity types
                    "entity" // unknown single entity query in GraphQL/REST API, would collide with entity types
                }
            },
            {
                ClassifierType.Attribute,
                new HashSet<string>
                {
                    "primaryKey", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "locale", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "priceValidIn", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "priceValidNow", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "priceInCurrency", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "priceInPriceList", // argument in single entity query in GraphQL/REST API, would collide with attributes
                    "entityBodyContent", // argument in single entity query in REST API, would collide with attributes
                    "associatedDataContent", // argument in single entity query in REST API, would collide with attributes
                    "associatedDataContentAll", // argument in single entity query in REST API, would collide with attributes
                    "attributeContent", // argument in single entity query in REST API, would collide with attributes
                    "attributeContentAll", // argument in single entity query in REST API, would collide with attributes
                    "referenceContent", // argument in single entity query in REST API, would collide with attributes
                    "referenceContentAll", // argument in single entity query in REST API, would collide with attributes
                    "priceContent", // argument in single entity query in REST API, would collide with attributes
                    "dataInLocales" // argument in single entity query in REST API, would collide with attributes
                }
            },
            {
                ClassifierType.ReferenceAttribute,
                new HashSet<string>()
            },
            {
                ClassifierType.Reference,
                new HashSet<string>
                {
                    "primaryKey", // field in entity object in GraphQL/REST API, would collide with references
                    "locale", // field in entity object in GraphQL/REST API, would collide with references
                    "type", // field in entity object in GraphQL/REST API, would collide with references
                    "price", // field in entity object in GraphQL/REST API, would collide with references
                    "priceForSale", // field in entity object in GraphQL/REST API, would collide with references
                    "prices", // field in entity object in GraphQL/REST API, would collide with references
                    "attributes", // field in entity object in GraphQL/REST API, would collide with references
                    "associatedData", // field in entity object in GraphQL/REST API, would collide with references
                    "self" // field in hierarchy parents and statistics extra result in GraphQL API, would collide with hierarchy references
                }
            }
        };
}