using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Exceptions;

public class ContextMissingException : EvitaInvalidUsageException
{
    public ContextMissingException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public ContextMissingException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public ContextMissingException(string privateMessage, string publicMessage, Exception exception) : base(
        privateMessage, publicMessage, exception)
    {
    }

    public ContextMissingException(string publicMessage) : base(publicMessage)
    {
    }

    public ContextMissingException() : base(
        "Query context is missing. You need to use method getPriceForSale(Currency, OffsetDateTime, Serializable...) " +
        "and provide the context on your own.")
    {
    }

    public static ContextMissingException HierarchyContextMissing()
    {
        return new ContextMissingException(
            "Hierarchy placement was not fetched along with the entity. You need to use `hierarchyContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException PricesNotFetched()
    {
        return new ContextMissingException(
            "Prices were not fetched along with the entity. You need to use `priceContent` requirement with constants" +
            "`" + PriceContentMode.RespectingFilter + "` or `" + PriceContentMode.All + "` in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException PricesNotFetched(Currency requiredCurrency, Currency fetchedCurrency)
    {
        return new ContextMissingException(
            "Prices in currency `" + requiredCurrency.CurrencyCode + "` were not fetched along with the entity. " +
            "Entity was fetched with following currency: `" + fetchedCurrency.CurrencyCode + "`"
        );
    }

    public static ContextMissingException PricesNotFetched(string requiredPriceList, ISet<string> fetchedPriceLists)
    {
        return new ContextMissingException(
            "Prices in price list `" + requiredPriceList + "` were not fetched along with the entity. " +
            "Entity was fetched with following price lists: " +
            string.Join(", ", fetchedPriceLists.Select(it => "`" + it + "`"))
        );
    }

    public static ContextMissingException AttributeContextMissing()
    {
        return new ContextMissingException(
            "No attributes were fetched along with the entity. You need to use `attributeContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException AttributeContextMissing(string attributeName)
    {
        return new ContextMissingException(
            "Attribute `" + attributeName +
            "` was not fetched along with the entity. You need to use `attributeContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException ReferenceAttributeContextMissing()
    {
        return new ContextMissingException(
            "No reference attributes were fetched along with the entity. You need to use `attributeContent` requirement in " +
            "`referenceContent` of your `require` part of the query."
        );
    }

    public static ContextMissingException ReferenceAttributeContextMissing(string attributeName)
    {
        return new ContextMissingException(
            "Attribute `" + attributeName +
            "` was not fetched along with the entity. You need to use `attributeContent` requirement in " +
            "`referenceContent` of your `require` part of the query."
        );
    }

    public static ContextMissingException AttributeLocalizationContextMissing(string attributeName, CultureInfo locale,
        IEnumerable<CultureInfo> fetchedLocales)
    {
        return new ContextMissingException(
            "Attribute `" + attributeName + "` in requested locale `" + locale.TwoLetterISOLanguageName +
            "` was not fetched along with the entity. " +
            "You need to use `dataInLocale` requirement with proper language tag in your `require` part of the query. " +
            string.Join(", ",
                "Entity was fetched with following locales: " + fetchedLocales.Select(x => x.TwoLetterISOLanguageName)
                    .Select(it => "`" + it + "`"))
        );
    }

    public static ContextMissingException LocaleForAttributeContextMissing(string attributeName)
    {
        return new ContextMissingException(
            "Attribute `" + attributeName + "` is localized. You need to use `entityLocaleEquals` constraint in " +
            "your filter part of the query, or you need to call `getAttribute()` method " +
            "with explicit locale argument!"
        );
    }

    public static ContextMissingException AssociatedDataContextMissing()
    {
        return new ContextMissingException(
            "No associated data were fetched along with the entity. You need to use `associatedDataContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException AssociatedDataContextMissing(string associatedDataName)
    {
        return new ContextMissingException(
            "Associated data `" + associatedDataName +
            "` was not fetched along with the entity. You need to use `associatedDataContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException AssociatedDataLocalizationContextMissing(string associatedDataName,
        CultureInfo locale, IEnumerable<CultureInfo> fetchedLocales)
    {
        return new ContextMissingException(
            "Associated data `" + associatedDataName + "` in requested locale `" + locale.TwoLetterISOLanguageName +
            "` was not fetched along with the entity. " +
            "You need to use `dataInLocale` requirement with proper language tag in your `require` part of the query. " +
            "Entity was fetched with following locales: " + string.Join(", ",
                fetchedLocales.Select(x => x.TwoLetterISOLanguageName).Select(it => "`" + it + "`")));
    }

    public static ContextMissingException LocaleForAssociatedDataContextMissing(string associatedDataName)
    {
        return new ContextMissingException(
            "Associated data `" + associatedDataName +
            "` is localized. You need to use `entityLocaleEquals` constraint in " +
            "your filter part of the query, or you need to call `getAssociatedData()` method " +
            "with explicit locale argument!"
        );
    }

    public static ContextMissingException ReferenceContextMissing()
    {
        return new ContextMissingException(
            "No references were fetched along with the entity. You need to use `referenceContent` requirement in " +
            "your `require` part of the query."
        );
    }

    public static ContextMissingException ReferenceContextMissing(string referenceName)
    {
        return new ContextMissingException(
            "Reference `" + referenceName +
            "` was not fetched along with the entity. You need to use `referenceContent` requirement in " +
            "your `require` part of the query."
        );
    }
}