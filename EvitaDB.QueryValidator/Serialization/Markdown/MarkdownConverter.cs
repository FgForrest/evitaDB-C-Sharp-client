using System.Globalization;
using System.Text.RegularExpressions;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Requires;
using EvitaDB.Client.Utils;
using EvitaDB.QueryValidator.Serialization.Markdown.Structures;

namespace EvitaDB.QueryValidator.Serialization.Markdown;

public static partial class MarkdownConverter
{
    private static readonly IDictionary<CultureInfo, string> Locales = new Dictionary<CultureInfo, string>
    {
        { new CultureInfo("cs"), "\uD83C\uDDE8\uD83C\uDDFF" },
        { new CultureInfo("en"), "\uD83C\uDDEC\uD83C\uDDE7" },
        { new CultureInfo("de"), "\uD83C\uDDE9\uD83C\uDDEA" }
    };
    
    private static readonly IDictionary<string, string> CurrencySymbols = new Dictionary<string, string>
    {
        { "CZK", "Kč" },
        { "USD", "$" },
        { "GBP", "£" },
        { "EUR", "€" }
    };
    
    private const string PredecessorHeadSymbol = "⎆";
    private const string PredecessorSymbol = "↻ ";

    private const string RefEntityLink = "\uD83D\uDCC4 ";
    private const string RefLink = "\uD83D\uDD17 ";
    private const string AttrLink = ": ";
    private const string PriceLink = "\uD83E\uDE99 ";
    private const string PriceForSale = PriceLink + "Price for sale";
    private const string Prices = PriceLink + "Prices found";
    private const string EntityPrimaryKey = "entityPrimaryKey";

    private static readonly Regex AttrLinkParser = MyRegex();

    public static string GenerateMarkDownTable(
        IEntitySchema entitySchema,
        Query query,
        EvitaResponse<ISealedEntity> response
    )
    {
        EntityFetch? entityFetch = query.Require?
            .SelectMany(
                QueryUtils.FindConstraints<EntityFetch, ISeparateEntityContentRequireContainer>
            )
            .FirstOrDefault();
        bool localizedQuery = query.FilterBy is not null && query.FilterBy
                                  .Any(filterBy => QueryUtils.FindConstraint<EntityLocaleEquals>(filterBy) != null) ||
                              query.Require is not null && query.Require
                                  .Any(require => QueryUtils.FindConstraint<DataInLocales>(require) != null);
        bool allPriceForSaleConstraintsSet = query.FilterBy is not null &&
                                             QueryUtils.FindConstraint<PriceInPriceLists>(query.FilterBy) is not null &&
                                             QueryUtils.FindConstraint<PriceInCurrency>(query.FilterBy) is not null &&
                                             QueryUtils.FindConstraint<PriceValidIn>(query.FilterBy) is not null;

        // collect headers for the MarkDown table
        List<string> headers = new List<string> { EntityPrimaryKey };
        if (entityFetch is not null)
        {
            headers.AddRange(GetEntityHeaders(entityFetch, () => response.RecordData,
                entitySchema, localizedQuery, null));
        }

        List<ReferenceContent> referenceContents =
            QueryUtils.FindConstraints<ReferenceContent, ISeparateEntityContentRequireContainer>(entityFetch!);
        headers.AddRange(
            referenceContents
                .SelectMany(refCnt => refCnt.ReferencedNames
                    .Select(entitySchema.GetReferenceOrThrowException)
                    .SelectMany(referenceSchema =>
                    {
                        var attributeContent =
                            QueryUtils.FindConstraint<AttributeContent, ISeparateEntityContentRequireContainer>(refCnt);
                        if (attributeContent != null)
                        {
                            IEnumerable<string> attributeNames;
                            if (attributeContent.AllRequested)
                            {
                                var attributes = referenceSchema.GetAttributes().Values;
                                attributeNames =
                                    (localizedQuery ? attributes.Where(attr => attr.Localized) : attributes)
                                    .Select(attr => attr.Name)
                                    .Where(
                                        attrName => response.RecordData
                                            .SelectMany(entity => entity.GetReferences(referenceSchema.Name))
                                            .Any(reference => reference.GetAttributeValue(attrName) is not null)
                                    );
                            }
                            else
                            {
                                attributeNames = attributeContent.GetAttributeNames();
                            }

                            return attributeNames
                                .SelectMany(
                                    attrName => TransformLocalizedAttributes(
                                        () => response.RecordData, attrName, entitySchema.Locales, referenceSchema,
                                        entity => entity.GetReferences(referenceSchema.Name),
                                        RefLink + referenceSchema.Name + AttrLink
                                    )
                                )
                                .Concat(GetReferencedEntityHeaders(response, refCnt, referenceSchema, entitySchema,
                                    localizedQuery));
                        }

                        return GetReferencedEntityHeaders(response, refCnt, referenceSchema, entitySchema,
                            localizedQuery);
                    })
                    .Distinct()
                )
        );

        List<PriceContent> priceContents =
            QueryUtils.FindConstraints<PriceContent, ISeparateEntityContentRequireContainer>(entityFetch!);
        headers.AddRange(
            priceContents
                .SelectMany(priceCnt =>
                {
                    if (priceCnt.FetchMode == PriceContentMode.RespectingFilter)
                    {
                        return allPriceForSaleConstraintsSet
                            ? new List<string> { PriceForSale }
                            : new List<string> { PriceForSale, Prices };
                    }

                    return new List<string>();
                })
        );

        // define the table with header line
        Table<object>.Builder tableBuilder = new Table<object>.Builder()
            .WithAlignment(Table<object>.AlignLeft)
            // ReSharper disable once CoVariantArrayConversion
            .AddRow(headers.ToArray());

        // prepare price formatter
        CultureInfo? locale = query.FilterBy?
            .Select(QueryUtils.FindConstraint<EntityLocaleEquals>)
            .Select(f => f?.Locale)
            .FirstOrDefault() ?? Locales.Keys.FirstOrDefault(x=>x.Name == "en-US");
        
        string currency = query.FilterBy?
            .Select(QueryUtils.FindConstraint<PriceInCurrency>)
            .Select(f =>
                f is not null
                    ? CurrencySymbols[f.Currency.CurrencyCode]
                    : CurrencySymbols["EUR"])
            .FirstOrDefault()!;

        // add rows
        foreach (var sealedEntity in response.RecordData)
        {
            tableBuilder.AddRow(
                // ReSharper disable once CoVariantArrayConversion
                headers.Select(header =>
                {
                    if (header == EntityPrimaryKey)
                    {
                        return sealedEntity.PrimaryKey.ToString();
                    }

                    AttributeKey? attributeKey;
                    if (header.StartsWith(RefLink))
                    {
                        string[] refAttr = AttrLinkParser.Split(header[RefLink.Length..]);
                        if (refAttr.Length == 1)
                        {
                            string[] refEntitySplit = refAttr[0].Split(RefEntityLink);
                            string refName = refEntitySplit[0].Trim();
                            return string.Join(", ", sealedEntity.GetReferences(refName)
                                .Select(x => x.ReferencedPrimaryKey)
                                .Select(refEntity => RefEntityLink + refEntitySplit[1] + AttrLink + refEntity));
                        }

                        attributeKey = ToAttributeKey(refAttr[1]);
                        if (refAttr[0].Contains(RefEntityLink))
                        {
                            string[] refEntitySplit = refAttr[0].Split(RefEntityLink);
                            string refName = refEntitySplit[0].Trim();
                            return string.Join(", ", sealedEntity.GetReferences(refName)
                                .Select(x => x.ReferencedEntity)
                                .Where(x => x is not null)
                                .Select(x => x!)
                                .Where(refEntity => refEntity.GetAttributeValue(attributeKey) is not null)
                                .Select(refEntity =>
                                {
                                    string formattedValue =
                                        FormatValue(refEntity.GetAttributeValue(attributeKey)?.Value);
                                    return RefEntityLink + refEntitySplit[1] + " " + refEntity.PrimaryKey + AttrLink +
                                           formattedValue;
                                }));
                        }

                        return string.Join(", ", sealedEntity.GetReferences(refAttr[0])
                            .Where(reference => reference.GetAttributeValue(attributeKey) is not null)
                            .Select(r =>
                            {
                                string formattedValue = FormatValue(r.GetAttributeValue(attributeKey)?.Value);
                                return RefLink + r.ReferenceKey.PrimaryKey + AttrLink + formattedValue;
                            }));
                    }

                    if (header == PriceForSale)
                    {
                        return sealedEntity.PriceForSale is not null
                            ? PriceLink +
                              $"{currency}{sealedEntity.PriceForSale.PriceWithTax:N2}" +
                              " (with " +
                              decimal.Parse(sealedEntity.PriceForSale.TaxRate.ToString("0.#########")) + "%" +
                              " tax) / " + $"{currency}{sealedEntity.PriceForSale.PriceWithoutTax:N2}"
                            : "N/A";
                    }

                    if (header == Prices)
                    {
                        List<IPrice> prices = sealedEntity.GetPrices().ToList();
                        if (!prices.Any())
                        {
                            return "N/A";
                        }

                        return string.Join(", ",
                                   prices.Take(3).Select(price =>
                                       PriceLink + $"{currency}{price.PriceWithTax:N2}")) +
                               (prices.Count > 3 ? $" ... and {prices.Count - 3} other prices" : "");
                    }

                    attributeKey = ToAttributeKey(header);
                    return sealedEntity.GetAttributeValue(attributeKey) is not null
                        ? FormatValue(
                            sealedEntity.GetAttributeValue(attributeKey)?.Value)
                        : "";
                }).ToArray()!);
        }

        // generate MarkDown
        PaginatedList<ISealedEntity> recordPage = (PaginatedList<ISealedEntity>)response.RecordPage;
        return tableBuilder.Build().Serialize() + "\n\n###### **Page** " + recordPage.PageNumber + "/" +
               recordPage.LastPageNumber + " **(Total number of results: " + recordPage.TotalRecordCount +
               ")**";
    }

    private static AttributeKey ToAttributeKey(string attributeHeader)
    {
        if (attributeHeader.StartsWith('\uD83C'))
        {
            foreach (KeyValuePair<CultureInfo, string> entry in Locales)
            {
                if (attributeHeader.StartsWith(entry.Value))
                {
                    return new AttributeKey(
                        attributeHeader[(entry.Value.Length + 1)..],
                        entry.Key
                    );
                }
            }

            throw new ArgumentException("Unknown locale for attribute header: " + attributeHeader);
        }

        return new AttributeKey(attributeHeader);
    }

    private static IEnumerable<string> GetReferencedEntityHeaders(
        EvitaResponse<ISealedEntity> response,
        ReferenceContent referenceContent,
        IReferenceSchema referenceSchema,
        IEntitySchema entitySchema,
        bool localizedQuery
    )
    {
        return new[]
            {
                RefLink + " " + referenceSchema.Name + " " + RefEntityLink + referenceSchema.ReferencedEntityType
            }
            .Concat(
                QueryUtils.FindConstraints<EntityFetch, ISeparateEntityContentRequireContainer>(referenceContent)
                    .SelectMany(
                        refEntity => GetEntityHeaders(
                            refEntity,
                            () => response.RecordData
                                .SelectMany(theEntity => theEntity.GetReferences(referenceSchema.Name))
                                .Select(theRef => theRef.ReferencedEntity)
                                .Where(x => x is not null)!,
                            entitySchema, localizedQuery,
                            RefLink + " " + referenceSchema.Name + " " + RefEntityLink +
                            referenceSchema.ReferencedEntityType + AttrLink
                        )
                    )
            );
    }

    private static IEnumerable<string> GetEntityHeaders(EntityFetch entityFetch,
        Func<IEnumerable<ISealedEntity>> entityCollectionAccessor, IEntitySchema entitySchema,
        bool localizedQuery, string? prefix)
    {
        return QueryUtils.FindConstraints<AttributeContent, ISeparateEntityContentRequireContainer>(entityFetch)
            .SelectMany(attributeContent =>
            {
                if (attributeContent.AllRequested)
                {
                    IEnumerable<IAttributeSchema> attributes = entitySchema is EntitySchemaDecorator schema
                        ? schema.OrderedAttributes
                        : entitySchema.GetAttributes().Values;
                    return (localizedQuery ? attributes.Where(x => x.Localized) : attributes)
                        .Select(x => x.Name)
                        .Where(attrName =>
                            entityCollectionAccessor.Invoke()
                                .Any(entity => entity.GetAttributeValue(attrName) is not null));
                }

                return attributeContent.GetAttributeNames();
            })
            .SelectMany(
                attributeName => TransformLocalizedAttributes(
                    entityCollectionAccessor, attributeName, entitySchema.Locales, entitySchema, x => new[] { x },
                    prefix
                )
            )
            .Distinct();
    }

    private static IEnumerable<string> TransformLocalizedAttributes<T>(
        Func<IEnumerable<ISealedEntity>> response,
        string attributeName,
        ISet<CultureInfo> entityLocales,
        IAttributeSchemaProvider<T> schema,
        Func<ISealedEntity, IEnumerable<IAttributes<T>>> attributesProvider,
        string? prefix
    ) where T : IAttributeSchema
    {
        bool localized = schema.GetAttribute(attributeName)?.Localized ??
                         throw new ArgumentException("Attribute not found: " + attributeName);
        if (localized)
        {
            return entityLocales
                .Where(locale => response.Invoke()
                    .SelectMany(attributesProvider)
                    .Any(attributeProvider => attributeProvider.AttributesAvailable(locale) &&
                                              attributeProvider.GetAttributeValue(attributeName,
                                                  locale) is not null)
                )
                .Select(locale =>
                {
                    if (Locales.TryGetValue(locale, out var flag))
                    {
                        return $"{flag} {attributeName}";
                    }

                    throw new ArgumentException("No flag for locale: " + locale);
                })
                .Select(it => prefix is null ? it : prefix + attributeName);
        }

        return prefix is null ? new[] { attributeName } : new[] { prefix + attributeName };
    }

    private static string FormatValue(object? value)
    {
        if (value is Predecessor predecessor)
        {
            return predecessor.IsHead ? PredecessorHeadSymbol : PredecessorSymbol + predecessor.PredecessorId;
        }

        return EvitaDataTypes.FormatValue(value);
    }

    [GeneratedRegex(": ", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
