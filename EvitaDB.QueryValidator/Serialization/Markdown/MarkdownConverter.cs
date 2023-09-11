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
        {new CultureInfo("cs"), "\uD83C\uDDE8\uD83C\uDDFF"},
        {new CultureInfo("en"), "\uD83C\uDDEC\uD83C\uDDE7"},
        {new CultureInfo("de"), "\uD83C\uDDE9\uD83C\uDDEA"}
    };

    private const string RefLink = "\uD83D\uDD17 ";
    private const string AttrLink = ": ";
    private const string PriceLink = "\uD83E\uDE99 ";
    private const string PriceForSale = PriceLink + "Price for sale";
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
        List<AttributeContent> attributeContents = QueryUtils.FindConstraints<AttributeContent, ISeparateEntityContentRequireContainer>(entityFetch!);
        var headers = new List<string> {EntityPrimaryKey};
        headers.AddRange(
        attributeContents
            .SelectMany(attributeContent =>
            {
                if (attributeContent.AllRequested)
                {
                    var attributes = entitySchema.Attributes.Values;
                    return (localizedQuery ? attributes.Where(attr => attr.Localized) : attributes)
                        .Select(attr => attr.Name)
                        .Where(attrName =>
                            response.RecordData.Any(entity => entity.GetAttributeValue(attrName) is not null));
                }

                return attributeContent.GetAttributeNames();
            })
            .SelectMany(
                attributeName => TransformLocalizedAttributes(
                    response, attributeName, entitySchema.Locales, entitySchema, entity => new[] {entity}
                )
            )
            .Distinct()
        );

        List<ReferenceContent> referenceContents = QueryUtils.FindConstraints<ReferenceContent, ISeparateEntityContentRequireContainer>(entityFetch!);
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
                                        response, attrName, entitySchema.Locales, referenceSchema,
                                        entity => entity.GetReferences(referenceSchema.Name)
                                    )
                                )
                                .Select(attr => RefLink + referenceSchema.Name + AttrLink + attr);
                        }

                        return Enumerable.Empty<string>();
                    })
                    .Distinct()
                )
        );
        
        List<PriceContent> priceContents = QueryUtils.FindConstraints<PriceContent, ISeparateEntityContentRequireContainer>(entityFetch!);
        headers.AddRange(
            priceContents
                .Select(priceCnt =>
                {
                    if (priceCnt.FetchMode == PriceContentMode.RespectingFilter)
                    {
                        return PriceForSale;
                    }

                    return null;
                })
                .Where(x => x != null)!
        );

        // define the table with header line
        var tableBuilder = new Table<object>.Builder()
            .WithAlignment(Table<object>.AlignLeft)
            // ReSharper disable once CoVariantArrayConversion
            .AddRow(headers.ToArray());

        // prepare price formatter
        var locale = query.FilterBy?
            .Select(QueryUtils.FindConstraint<EntityLocaleEquals>)
            .Select(f => f?.Locale)
            .FirstOrDefault() ?? new CultureInfo("en");

        var currency = query.FilterBy?
            .Select(QueryUtils.FindConstraint<PriceInCurrency>)
            .Select(f => f?.Currency.CurrencyCode)
            .FirstOrDefault() ?? new CultureInfo("de-DE").NumberFormat.CurrencySymbol;
        var priceFormatter = new CultureInfo(locale.Name) {NumberFormat = {CurrencySymbol = currency}};

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
                        var refAttr = AttrLinkParser.Split(header[RefLink.Length..]);
                        attributeKey = ToAttributeKey(refAttr[1]);
                        return string.Join(", ", sealedEntity.GetReferences(refAttr[0])
                            .Where(reference => reference.GetAttributeValue(attributeKey) is not null)
                            .Select(reference => RefLink + reference.ReferenceKey.PrimaryKey + AttrLink +
                                                 EvitaDataTypes.FormatValue(reference
                                                     .GetAttributeValue(attributeKey)?.Value)));
                    }

                    if (header == PriceForSale)
                    {
                        return sealedEntity.PriceForSale is not null
                            ? PriceLink +
                              string.Format(priceFormatter, "{0:C}", sealedEntity.PriceForSale.PriceWithTax) +
                              " (with " +
                              decimal.Parse(sealedEntity.PriceForSale.TaxRate.ToString("0.#########")) + "%" +
                              " tax) / " + string.Format(priceFormatter, "{0:C}",
                                  sealedEntity.PriceForSale.PriceWithoutTax)
                            : "N/A";
                    }

                    attributeKey = ToAttributeKey(header);
                    return sealedEntity.GetAttributeValue(attributeKey) is not null
                        ? EvitaDataTypes.FormatValue(
                            sealedEntity.GetAttributeValue(attributeKey)?.Value)
                        : "";
                }).ToArray()!);
        }

        // generate MarkDown
        PaginatedList<ISealedEntity> recordPage = (PaginatedList<ISealedEntity>) response.RecordPage;
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

    private static IEnumerable<string> TransformLocalizedAttributes(
        EvitaResponse<ISealedEntity> response,
        string attributeName,
        ISet<CultureInfo> entityLocales,
        IAttributeSchemaProvider<IAttributeSchema> schema,
        Func<ISealedEntity, IEnumerable<IAttributes>> attributesProvider
    )
    {
        bool localized = schema.GetAttribute(attributeName)?.Localized ??
                         throw new ArgumentException("Attribute not found: " + attributeName);
        if (localized)
        {
            return entityLocales
                .Where(locale => response.RecordData
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
                });
        }

        return new[] {attributeName};
    }

    [GeneratedRegex(": ", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}