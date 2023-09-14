using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models;

public class EvitaRequestData
{
    public DateTimeOffset AlignedNow { get; init; }
    public string EntityType { get; init; }
    public CultureInfo ImplicitLocale { get; init; }
    public int[] PrimaryKeys { get; init; }
    public bool LocaleExamined { get; init; }
    public CultureInfo Locale { get; init; }
    public bool? RequiredLocales { get; init; }
    public ISet<CultureInfo> RequiredLocaleSet { get; init; }
    public QueryPriceMode QueryPriceMode { get; init; }
    public bool PriceValidInTimeSet { get; init; }
    public DateTimeOffset PriceValidInTime { get; init; }
    public bool RequiresEntity { get; init; }
    public bool RequiresParent { get; init; }
    public bool EntityAttributes { get; init; }
    public ISet<string> EntityAttributeSet { get; init; }
    public bool EntityAssociatedData { get; init; }
    public ISet<string> EntityAssociatedDataSet { get; init; }
    public bool EntityReference { get; init; }
    public PriceContentMode EntityPrices { get; init; }
    public bool CurrencySet { get; init; }
    public Currency Currency { get; init; }
    public bool RequiresPriceLists { get; init; }
    public string[] PriceLists { get; init; }
    public string[] AdditionalPriceLists { get; init; }
    public int? FirstRecordOffSet { get; init; }
    public bool? RequiredWithinHierarchy { get; init; }
    public bool? RequiresHierarchyStatistics { get; init; }
    public bool? RequiresHierarchyParents { get; init; }
    public int? Limit { get; init; }
    public bool QueryTelemetryRequested { get; init; }
    public IDictionary<string, AttributeRequest> ReferenceSet { get; init; }

    public EvitaRequestData(DateTimeOffset alignedNow, string entityType, CultureInfo implicitLocale, int[] primaryKeys, bool localeExamined, CultureInfo locale, bool? requiredLocales, ISet<CultureInfo> requiredLocaleSet, QueryPriceMode queryPriceMode, bool priceValidInTimeSet, DateTimeOffset priceValidInTime, bool requiresEntity, bool requiresParent, bool entityAttributes, ISet<string> entityAttributeSet, bool entityAssociatedData, ISet<string> entityAssociatedDataSet, bool entityReference, PriceContentMode entityPrices, bool currencySet, Currency currency, bool requiresPriceLists, string[] priceLists, string[] additionalPriceLists, int? firstRecordOffSet, bool? requiredWithinHierarchy, bool? requiresHierarchyStatistics, bool? requiresHierarchyParents, int? limit, bool queryTelemetryRequested, IDictionary<string, AttributeRequest> referenceSet)
    {
        AlignedNow = alignedNow;
        EntityType = entityType;
        ImplicitLocale = implicitLocale;
        PrimaryKeys = primaryKeys;
        LocaleExamined = localeExamined;
        Locale = locale;
        RequiredLocales = requiredLocales;
        RequiredLocaleSet = requiredLocaleSet;
        QueryPriceMode = queryPriceMode;
        PriceValidInTimeSet = priceValidInTimeSet;
        PriceValidInTime = priceValidInTime;
        RequiresEntity = requiresEntity;
        RequiresParent = requiresParent;
        EntityAttributes = entityAttributes;
        EntityAttributeSet = entityAttributeSet;
        EntityAssociatedData = entityAssociatedData;
        EntityAssociatedDataSet = entityAssociatedDataSet;
        EntityReference = entityReference;
        EntityPrices = entityPrices;
        CurrencySet = currencySet;
        Currency = currency;
        RequiresPriceLists = requiresPriceLists;
        PriceLists = priceLists;
        AdditionalPriceLists = additionalPriceLists;
        FirstRecordOffSet = firstRecordOffSet;
        RequiredWithinHierarchy = requiredWithinHierarchy;
        RequiresHierarchyStatistics = requiresHierarchyStatistics;
        RequiresHierarchyParents = requiresHierarchyParents;
        Limit = limit;
        QueryTelemetryRequested = queryTelemetryRequested;
        ReferenceSet = referenceSet;
    }
}