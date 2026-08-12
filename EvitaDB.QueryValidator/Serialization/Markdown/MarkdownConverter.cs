using System.Globalization;
using System.Text.RegularExpressions;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;
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
        { "CZK", "Kč" }, { "USD", "$" }, { "GBP", "£" }, { "EUR", "€" }
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
                                    (localizedQuery ? attributes.Where(attr => attr.Localized()) : attributes)
                                    .Select(attr => attr.Name)
                                    .Where(attrName => response.RecordData
                                        .SelectMany(entity => entity.GetReferences(referenceSchema.Name))
                                        .Any(reference => reference.GetAttributeValue(attrName) is not null)
                                    );
                            }
                            else
                            {
                                attributeNames = attributeContent.GetAttributeNames();
                            }

                            return attributeNames
                                .SelectMany(attrName => TransformLocalizedAttributes(
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
                        // Decided from the data, not from which price constraints the query happens to carry.
                        // Guessing from the constraints rendered a "Price for sale" column full of N/A whenever
                        // the query omitted `priceValidIn` even though a selling price was resolved anyway - and
                        // conversely never dropped that column when no row had one.
                        return AnyPriceForSale(response)
                            ? new List<string> { PriceForSale }
                            : new List<string> { Prices };
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
            .FirstOrDefault() ?? Locales.Keys.FirstOrDefault(x => x.Name == "en-US");

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
                        IPrice? priceForSale = TryGetPriceForSale(sealedEntity);
                        return priceForSale is not null
                            ? PriceLink +
                              currency + FormatNumber(priceForSale.PriceWithTax) +
                              " (with " +
                              FormatTaxRate(priceForSale.TaxRate) + "%" +
                              " tax) / " + currency + FormatNumber(priceForSale.PriceWithoutTax)
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
                                       PriceLink + currency + FormatNumber(price.PriceWithTax))) +
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
                    .SelectMany(refEntity => GetEntityHeaders(
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
                    return (localizedQuery ? attributes.Where(x => x.Localized()) : attributes)
                        .Select(x => x.Name)
                        .Where(attrName =>
                            entityCollectionAccessor.Invoke()
                                .Any(entity => entity.GetAttributeValue(attrName) is not null));
                }

                return attributeContent.GetAttributeNames();
            })
            .SelectMany(attributeName => TransformLocalizedAttributes(
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
        bool localized = schema.GetAttribute(attributeName)?.Localized() ??
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

    /// <summary>
    /// Formats a decimal for the rendered document: `.` as the decimal separator and `,` grouping thousands,
    /// e.g. `31,234.57`. Pinned to the invariant culture on purpose - without it the output followed whatever
    /// locale the machine running the validator happened to have, so the same query rendered `30.00` on one
    /// machine and `30,00` on another and the documentation fixtures could never match both.
    /// </summary>
    /// <summary>
    /// True when at least one returned entity actually resolved a selling price. `PriceForSale` throws
    /// <c>ContextMissingException</c> when the query carries no price context, so the probe is guarded -
    /// an entity that cannot answer simply counts as having none.
    /// </summary>
    private static bool AnyPriceForSale(EvitaResponse<ISealedEntity> response) =>
        response.RecordData.Any(entity => TryGetPriceForSale(entity) is not null);

    private static IPrice? TryGetPriceForSale(ISealedEntity entity)
    {
        try
        {
            return entity.PricesAvailable() ? entity.PriceForSale : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string FormatNumber(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);

    /// <summary>Tax rate without trailing zeros - `21` rather than `21.00`.</summary>
    private static string FormatTaxRate(decimal value) =>
        value.ToString("0.#########", CultureInfo.InvariantCulture);

    private static string FormatValue(object? value)
    {
        if (value is Predecessor predecessor)
        {
            return (predecessor as IChainableType).IsHead
                ? PredecessorHeadSymbol
                : PredecessorSymbol + predecessor.PredecessorPk;
        }

        // arrays are rendered element-wise; EvitaDataTypes.FormatValue only knows scalars and would throw
        if (value is Array array)
        {
            return "[" + string.Join(", ", array.Cast<object?>().Select(FormatValue)) + "]";
        }

        return EvitaDataTypes.FormatValue(value);
    }

    [GeneratedRegex(": ", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
