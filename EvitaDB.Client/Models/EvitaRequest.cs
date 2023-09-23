using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Head;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models;

public class EvitaRequest
{
    public Query Query { get; }
    public DateTimeOffset AlignedNow { get; }
    private bool _localeExamined;
    private string? _entityType;
    private CultureInfo? _implicitLocale;
    private int[]? _primaryKeys;
    private CultureInfo? _locale;
    private bool? _requiredLocales;
    private ISet<CultureInfo>? _requiredLocaleSet;
    private QueryPriceMode? _queryPriceMode;
    private bool? _priceValidInTimeSet;
    private DateTimeOffset? _priceValidInTime;
    private bool? _requiresEntity;
    private bool? _requiresParent;
    private HierarchyContent? _parentContent;
    private EntityFetch? _entityRequirement;
    private bool? _entityAttributes;
    private ISet<string>? _entityAttributeSet;
    private bool? _entityAssociatedData;
    private ISet<string>? _entityAssociatedDataSet;
    private bool? _entityReference;
    private PriceContentMode? _entityPrices;
    private bool? _currencySet;
    private Currency? _currency;
    private bool? _requiresPriceLists;
    private string[]? _priceLists;
    private string[]? _additionalPriceLists;
    private int? _firstRecordOffset;
    private IDictionary<string, IHierarchyFilterConstraint>? _hierarchyWithin;
    private bool? _requiredWithinHierarchy;
    private int? _limit;
    private ResultForm? _resultForm;
    private bool? _queryTelemetryRequested;
    private IDictionary<string, RequirementContext>? _entityFetchRequirements;
    private RequirementContext? _defaultReferenceRequirement;

    private static readonly int[] EmptyInts = Array.Empty<int>();

    private static RequirementContext GetRequirementContext(
        ReferenceContent referenceContent,
        AttributeContent? attributeContent
    )
    {
        return new RequirementContext(
            new AttributeRequest(
                attributeContent == null ? new HashSet<string>() : attributeContent.GetAttributeNames().ToHashSet(),
                attributeContent != null
            ),
            referenceContent.EntityRequirements,
            referenceContent.EntityGroupRequirements
        );
    }

    public EvitaRequest(
        Query query,
        DateTimeOffset alignedNow)
    {
        Collection? header = query.Entities;
        _entityType = header?.EntityType;
        Query = query;
        AlignedNow = alignedNow;
        _implicitLocale = null;
    }

    /**
     * Returns true if query targets specific entity type.
     */
    public bool EntityTypeRequested()
    {
        return _entityType != null;
    }

    /**
     * Returns type of the entity this query targets. Allows to choose proper {@link EntityCollectionContract}.
     */
    public string? GetEntityType()
    {
        return _entityType;
    }

    /**
     * Returns type of the entity this query targets. Allows to choose proper {@link EntityCollectionContract}.
     */
    public string GetEntityTypeOrThrowException(string purpose)
    {
        Collection? header = Query.Entities;
        return header is not null ? header.EntityType : throw new EntityCollectionRequiredException(purpose);
    }

    /**
     * Returns locale of the entity that is being requested.
     */
    public CultureInfo? GetLocale()
    {
        if (!_localeExamined)
        {
            _localeExamined = true;
            _locale = QueryUtils.FindFilter<EntityLocaleEquals>(Query)?.Locale;
        }

        return _locale;
    }

    /**
     * Returns implicit locale that might be derived from the globally unique attribute if the entity is matched
     * particularly by it.
     */
    public CultureInfo? GetImplicitLocale()
    {
        return _implicitLocale;
    }

    /**
     * Returns locale of the entity that is being requested. If locale is not explicitly set in the query it falls back
     * to {@link #getImplicitLocale()}.
     */
    public CultureInfo? GetRequiredOrImplicitLocale()
    {
        return GetLocale() ?? GetImplicitLocale();
    }

    /**
     * Returns set of locales if requirement {@link DataInLocales} is present in the query. If not it falls back to
     * {@link EntityLocaleEquals} (check {@link DataInLocales} docs).
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public ISet<CultureInfo> GetRequiredLocales()
    {
        if (_requiredLocales == null)
        {
            EntityFetch? entityFetch =
                QueryUtils.FindRequire<EntityFetch, ISeparateEntityContentRequireContainer>(Query);
            if (entityFetch == null)
            {
                _requiredLocales = true;
                CultureInfo? theLocale = GetLocale();
                if (theLocale != null)
                {
                    _requiredLocaleSet = new HashSet<CultureInfo>(new[] { theLocale });
                }
            }
            else
            {
                DataInLocales? dataRequirement =
                    QueryUtils.FindConstraint<DataInLocales, ISeparateEntityContentRequireContainer>(entityFetch);
                if (dataRequirement != null)
                {
                    _requiredLocaleSet = dataRequirement.Locales.Where(x => x is not null).ToHashSet()!;
                }
                else
                {
                    CultureInfo? theLocale = GetLocale();
                    if (theLocale != null)
                    {
                        _requiredLocaleSet = new HashSet<CultureInfo>(new[] { theLocale });
                    }
                }

                _requiredLocales = true;
            }
        }

        return _requiredLocaleSet;
    }

    /**
     * Returns query price mode of the current query.
     */
    public QueryPriceMode? GetQueryPriceMode()
    {
        if (_queryPriceMode == null)
        {
            _queryPriceMode = QueryUtils.FindRequire<PriceType>(Query)?.QueryPriceMode ?? QueryPriceMode.WithTax;
        }

        return _queryPriceMode;
    }

    /**
     * Returns set of primary keys that are required by the query in {@link EntityPrimaryKeyInSet} query.
     * If there is no such query empty array is returned in the result.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public int[] GetPrimaryKeys()
    {
        if (_primaryKeys == null)
        {
            _primaryKeys = QueryUtils.FindFilter<EntityPrimaryKeyInSet, ISeparateEntityContentRequireContainer>(Query)?.PrimaryKeys ?? EmptyInts;
        }

        return _primaryKeys;
    }

    /**
     * Method will determine if at least entity body is required for main entities.
     */
    public bool RequiresEntity()
    {
        if (_requiresEntity == null)
        {
            EntityFetch? entityFetch = QueryUtils.FindRequire<EntityFetch, ISeparateEntityContentRequireContainer>(Query);
            _requiresEntity = entityFetch != null;
            _entityRequirement = entityFetch;
        }

        return _requiresEntity.Value;
    }

    /**
     * Method will find all requirement specifying richness of main entities. The constraints inside
     * {@link SeparateEntityContentRequireContainer} implementing of same type are ignored because they relate to the different entity context.
     */
    public EntityFetch? GetEntityRequirement()
    {
        if (_requiresEntity == null)
        {
            RequiresEntity();
        }

        return _entityRequirement;
    }

    /**
     * Method will determine if parent body is required for main entities.
     */
    public bool RequiresParent()
    {
        if (_requiresParent == null)
        {
            EntityFetch? entityFetch = GetEntityRequirement();
            if (entityFetch == null)
            {
                _parentContent = null;
                _requiresParent = false;
            }
            else
            {
                _parentContent =
                    QueryUtils.FindConstraint<HierarchyContent, ISeparateEntityContentRequireContainer>(entityFetch);
                _requiresParent = _parentContent != null;
            }
        }

        return _requiresParent.Value;
    }

    /**
     * Method will find all requirement specifying richness of main entities. The constraints inside
     * {@link SeparateEntityContentRequireContainer} implementing of same type are ignored because they relate to the
     * different entity context.
     */
    public HierarchyContent? GetHierarchyContent()
    {
        if (_requiresParent == null)
        {
            RequiresParent();
        }

        return _parentContent;
    }

    /**
     * Returns TRUE if requirement {@link AttributeContent} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public bool RequiresEntityAttributes()
    {
        if (_entityAttributes == null)
        {
            EntityFetch? entityFetch = GetEntityRequirement();
            if (entityFetch == null)
            {
                _entityAttributes = false;
                _entityAttributeSet = new HashSet<string>();
            }
            else
            {
                AttributeContent? requiresAttributeContent =
                    QueryUtils.FindConstraint<AttributeContent, ISeparateEntityContentRequireContainer>(entityFetch);
                _entityAttributes = requiresAttributeContent != null;
                _entityAttributeSet = requiresAttributeContent != null
                    ? requiresAttributeContent.GetAttributeNames().ToHashSet()
                    : new HashSet<string>();
            }
        }

        return _entityAttributes.Value;
    }

    /**
     * Returns set of attribute names that were requested in the query. The set is empty if none is requested
     * which means - all attributes is ought to be returned.
     */
    public ISet<string> GetEntityAttributeSet()
    {
        if (_entityAttributeSet == null)
        {
            RequiresEntityAttributes();
        }

        return _entityAttributeSet!;
    }

    /**
     * Returns TRUE if requirement {@link AssociatedDataContent} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public bool RequiresEntityAssociatedData()
    {
        if (_entityAssociatedData == null)
        {
            EntityFetch? entityFetch = GetEntityRequirement();
            if (entityFetch == null)
            {
                _entityAssociatedData = false;
                _entityAssociatedDataSet = new HashSet<string>();
            }
            else
            {
                AssociatedDataContent? requiresAssociatedDataContent =
                    QueryUtils.FindConstraint<AssociatedDataContent, ISeparateEntityContentRequireContainer>(
                        entityFetch);
                _entityAssociatedData = requiresAssociatedDataContent != null;
                _entityAssociatedDataSet = requiresAssociatedDataContent != null
                    ? requiresAssociatedDataContent.AssociatedDataNames.ToHashSet()
                    : new HashSet<string>();
            }
        }

        return _entityAssociatedData.Value;
    }

    /**
     * Returns set of associated data names that were requested in the query. The set is empty if none is requested
     * which means - all associated data is ought to be returned.
     */
    public ISet<string> GetEntityAssociatedDataSet()
    {
        if (_entityAssociatedDataSet == null)
        {
            RequiresEntityAssociatedData();
        }

        return _entityAssociatedDataSet!;
    }

    /**
     * Returns TRUE if requirement {@link ReferenceContent} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public bool RequiresEntityReferences()
    {
        if (_entityReference == null)
        {
            GetReferenceEntityFetch();
        }

        return _entityReference!.Value;
    }

    /**
     * Returns {@link PriceContentMode} if requirement {@link PriceContent} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public PriceContentMode GetRequiresEntityPrices()
    {
        if (_entityPrices == null)
        {
            EntityFetch? entityFetch =
                QueryUtils.FindRequire<EntityFetch, ISeparateEntityContentRequireContainer>(Query);
            if (entityFetch == null)
            {
                _entityPrices = PriceContentMode.None;
                _additionalPriceLists = PriceContent.EmptyPriceLists;
            }
            else
            {
                PriceContent? priceContentRequirement =
                    QueryUtils.FindConstraint<PriceContent, ISeparateEntityContentRequireContainer>(entityFetch);
                _entityPrices = priceContentRequirement?.FetchMode ?? PriceContentMode.None;
                _additionalPriceLists =
                    priceContentRequirement?.AdditionalPriceListsToFetch ?? PriceContent.EmptyPriceLists;
            }
        }

        return _entityPrices.Value;
    }

    /**
     * Returns array of price list ids if requirement {@link PriceContent} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public string[] GetFetchesAdditionalPriceLists()
    {
        if (_additionalPriceLists == null)
        {
            GetRequiresEntityPrices();
        }

        return _additionalPriceLists!;
    }

    /**
     * Returns TRUE if any {@link PriceInPriceLists} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public bool RequiresPriceLists()
    {
        if (_requiresPriceLists == null)
        {
            PriceInPriceLists? pricesInPriceList = QueryUtils.FindFilter<PriceInPriceLists>(Query);
            _priceLists = pricesInPriceList?.PriceLists ?? Array.Empty<string>();
            _requiresPriceLists = pricesInPriceList != null;
        }

        return _requiresPriceLists.Value;
    }

    /**
     * Returns array of price list ids if filter {@link PriceInPriceLists} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public string[] GetRequiresPriceLists()
    {
        if (_priceLists == null)
        {
            RequiresPriceLists();
        }

        return _priceLists!;
    }

    /**
     * Returns set of price list ids if requirement {@link PriceInCurrency} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public Currency GetRequiresCurrency()
    {
        if (_currencySet == null)
        {
            _currency = QueryUtils.FindFilter<PriceInCurrency>(Query)?.Currency;
            _currencySet = true;
        }

        return _currency!;
    }

    /**
     * Returns price valid in datetime if requirement {@link io.evitadb.api.query.filter.PriceValidIn} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public DateTimeOffset? GetRequiresPriceValidIn()
    {
        if (_priceValidInTimeSet == null)
        {
            PriceValidIn? priceValidIn = QueryUtils.FindFilter<PriceValidIn>(Query);
            _priceValidInTime = priceValidIn?.TheMoment ?? AlignedNow;
            _priceValidInTimeSet = true;
        }

        return _priceValidInTime;
    }

    /**
     * Returns TRUE if requirement {@link QueryTelemetry} is present in the query.
     * Accessor method cache the found result so that consecutive calls of this method are pretty fast.
     */
    public bool QueryTelemetryRequested()
    {
        if (_queryTelemetryRequested == null)
        {
            _queryTelemetryRequested = QueryUtils.FindRequire<QueryTelemetry>(Query) != null;
        }

        return _queryTelemetryRequested.Value;
    }

    /**
     * Returns count of records required in the result (i.e. number of records on a single page).
     */
    public int GetLimit()
    {
        if (_limit == null)
        {
            InitPagination();
        }

        return _limit!.Value;
    }

    /**
     * Returns requested record offset of the records required in the result.
     * Offset is automatically reset to zero if requested offset exceeds the total available record count.
     */
    public int GetFirstRecordOffset(int totalRecordCount)
    {
        if (_firstRecordOffset == null)
        {
            InitPagination();
        }

        return _firstRecordOffset >= totalRecordCount ? 0 : _firstRecordOffset!.Value;
    }

    /**
     * Returns default requirements for reference content.
     */
    public RequirementContext? GetDefaultReferenceRequirement()
    {
        GetReferenceEntityFetch();
        return _defaultReferenceRequirement;
    }

    /**
     * Returns requested referenced entity requirements from the input query.
     * Allows traversing through the object relational graph in unlimited depth.
     */
    public IDictionary<string, RequirementContext> GetReferenceEntityFetch()
    {
        if (_entityFetchRequirements == null)
        {
            EntityFetch? entityRequirement = GetEntityRequirement();
            if (entityRequirement is not null)
            {
                List<ReferenceContent>
                    referenceContent = QueryUtils.FindConstraints<ReferenceContent, ISeparateEntityContentRequireContainer>(entityRequirement);
                _entityReference = referenceContent.Any();
                _defaultReferenceRequirement = referenceContent
                    .Where(it => ArrayUtils.IsEmpty(it.ReferencedNames))
                    .Select(it => GetRequirementContext(it, it.AttributeContent))
                    .FirstOrDefault();
                
                _entityFetchRequirements = referenceContent
                    .SelectMany(it=>
                        it.ReferencedNames
                            .Select(entityType => new KeyValuePair<string,RequirementContext>(entityType, GetRequirementContext(it, it.AttributeContent)))
                    ).ToDictionary(x=>x.Key, x=>x.Value);
            }
            else
            {
                _entityReference = false;
                _entityFetchRequirements = new Dictionary<string, RequirementContext>();
            }
        }

        return _entityFetchRequirements;
    }

    /**
     * Returns {@link HierarchyWithin} query
     */
    public IHierarchyFilterConstraint? GetHierarchyWithin(string referenceName)
    {
        if (_requiredWithinHierarchy == null)
        {
            if (Query.FilterBy == null)
            {
                _hierarchyWithin = new Dictionary<string, IHierarchyFilterConstraint>();
            }
            else
            {
                _hierarchyWithin = new Dictionary<string, IHierarchyFilterConstraint>();
                QueryUtils.FindConstraints<IHierarchyFilterConstraint>(
                    Query.FilterBy).ForEach(it => _hierarchyWithin.Add(it.ReferenceName!, it));
            }

            _requiredWithinHierarchy = true;
        }

        return _hierarchyWithin.TryGetValue(referenceName, out IHierarchyFilterConstraint? constraint)
            ? constraint
            : null;
    }
    
    private void InitPagination() {
        Page? page = QueryUtils.FindRequire<Page>(Query);
        Strip? strip = QueryUtils.FindRequire<Strip>(Query);
        if (page is not null) {
            _limit = page.PageSize;
            _firstRecordOffset = PaginatedList<object>.GetFirstItemNumberForPage(page.Number, _limit.Value);
            _resultForm = ResultForm.PaginatedList;
        } else if (strip is not null) {
            _limit = strip.Limit;
            _firstRecordOffset = strip.Offset;
            _resultForm = ResultForm.StripList;
        } else {
            _limit = 20;
            _firstRecordOffset = 0;
            _resultForm = ResultForm.PaginatedList;
        }
    }

    private enum ResultForm
    {
        PaginatedList,
        StripList
    }
}