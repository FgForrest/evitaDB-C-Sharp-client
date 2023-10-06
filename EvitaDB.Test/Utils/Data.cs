using System.Globalization;
using EvitaDB.Client.DataTypes;

namespace EvitaDB.Test.Utils;

public static class Data
{
    public static readonly CultureInfo CzechLocale = new CultureInfo("cs-CZ");
    public static readonly CultureInfo EnglishLocale = new CultureInfo("en-US");
    public static readonly CultureInfo GermanLocale = new CultureInfo("de-GE");
    public static readonly CultureInfo FrenchLocale = new CultureInfo("fr-FR");
    public const string AttributeName = "name";
    public const string AttributeCode = "code";
    public const string AttributeUrl = "url";
    public const string AttributeEan = "ean";
    public const string AttributePriority = "priority";
    public const string AttributeValidity = "validity";
    public const string AttributeQuantity = "quantity";
    public const string AttributeAlias = "alias";
    public const string AttributeCategoryPriority = "categoryPriority";
    public const string AssociatedDataReferencedFiles = "referencedFiles";
    public const string AssociatedDataLabels = "labels";
    public static readonly Currency CurrencyCzk = new Currency("CZK");
    public static readonly Currency CurrencyEur = new Currency("EUR");
    public static readonly Currency CurrencyUsd = new Currency("USD");
    public static readonly Currency CurrencyGbp = new Currency("GBP");
    public const string PriceListBasic = "basic";
    public const string PriceListReference = "reference";
    public const string PriceListSellout = "sellout";
    public const string PriceListVip = "vip";
    public const string PriceListB2B = "b2b";
    public const string PriceListIntroduction = "introduction";

    public const string TestCatalog = nameof(TestCatalog);

    public static readonly string[] PriceListNames =
    {
        PriceListBasic,
        PriceListReference,
        PriceListSellout,
        PriceListVip,
        PriceListB2B,
        PriceListIntroduction
    };

    public static readonly Currency[] Currencies =
    {
        CurrencyCzk, CurrencyEur, CurrencyUsd, CurrencyGbp
    };

    public static readonly ISet<Currency> CurrenciesSet = new HashSet<Currency>(Currencies);

    public static readonly ISet<CultureInfo> LocalesSet =
        new HashSet<CultureInfo>(new [] {CzechLocale, EnglishLocale, GermanLocale, FrenchLocale});
}