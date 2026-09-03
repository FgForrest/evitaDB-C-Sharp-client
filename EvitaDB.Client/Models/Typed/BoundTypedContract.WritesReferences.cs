using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Writers for prices and references - the two kinds that carry several coordinated values rather than a
/// single named one.
/// </summary>
public sealed partial class BoundTypedContract
{
    /// <summary>
    /// Builds a writer for a price member. A price is written whole, as an <see cref="IPrice"/>, because
    /// its identity is the triplet (price id, price list, currency) and every field must travel together.
    /// </summary>
    private static Action<IEntityBuilder, object?[]?> BuildPriceWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        var priceLists = member.PriceLists;

        return (builder, args) =>
        {
            var (value, _) = SplitArguments(descriptor, member, args);

            if (member.Access == TypedMemberAccess.Remove)
            {
                RemovePrices(builder, value, priceLists);
                return;
            }

            foreach (var price in EnumerateValues(value))
            {
                if (price is not IPrice typedPrice)
                {
                    throw new EvitaInvalidUsageException(
                        $"Member `{descriptor.ContractType.Name}.{member.MemberName}` writes prices, so its " +
                        $"value must be `IPrice`, not `{price?.GetType().Name ?? "null"}`."
                    );
                }

                builder.SetPrice(
                    typedPrice.PriceId,
                    typedPrice.PriceList,
                    typedPrice.Currency,
                    typedPrice.InnerRecordId,
                    typedPrice.PriceWithoutTax,
                    typedPrice.TaxRate,
                    typedPrice.PriceWithTax,
                    typedPrice.Validity,
                    typedPrice.Indexed
                );
            }
        };
    }

    /// <summary>
    /// Removes either the prices given, or - when the remover takes no value - every price the member is
    /// scoped to.
    /// </summary>
    private static void RemovePrices(
        IEntityBuilder builder,
        object? value,
        IReadOnlyList<string> priceLists)
    {
        var explicitPrices = EnumerateValues(value).OfType<IPrice>().ToList();

        if (explicitPrices.Count > 0)
        {
            foreach (var price in explicitPrices)
            {
                builder.RemovePrice(price.PriceId, price.PriceList, price.Currency);
            }

            return;
        }

        var candidates = builder.PricesAvailable()
            ? builder.GetPrices().Where(it => !it.Dropped)
            : [];

        if (priceLists.Count > 0)
        {
            candidates = candidates.Where(it => priceLists.Contains(it.PriceList, StringComparer.Ordinal));
        }

        foreach (var price in candidates.ToList())
        {
            builder.RemovePrice(price.PriceId, price.PriceList, price.Currency);
        }
    }

    /// <summary>
    /// Builds a writer for a reference member. Accepted values are a primary key, an
    /// <see cref="IEntityReference"/>, or a typed view of the referenced entity.
    /// <remarks>
    /// A collection member replaces the whole set of that reference: keys absent from the new value are
    /// removed. A scalar member writes just the one reference.
    /// </remarks>
    /// </summary>
    private static Action<IEntityBuilder, object?[]?> BuildReferenceWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema)
    {
        var referenceName = member.DataName!;
        var referenceSchema = schema is null
            ? null
            : schema.GetReference(referenceName)
              ?? schema.GetReferenceByName(referenceName, Utils.NamingConvention.CamelCase);

        if (referenceSchema is null && schema is not null && member.NameDeclaredExplicitly
            && member.ReferenceDeclaration?.Entity is null)
        {
            throw new EvitaInvalidUsageException(
                $"Reference `{referenceName}` written by " +
                $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of entity " +
                $"`{schema.Name}`, and the member does not say which entity it points to."
            );
        }

        var resolvedName = referenceSchema?.Name ?? referenceName;
        var declaration = member.ReferenceDeclaration;
        var cardinality = referenceSchema?.Cardinality ?? ResolveWrittenCardinality(member, declaration);

        // "set" means "make it be this". A collection member therefore replaces the whole set, and so does
        // a scalar member on a single-cardinality reference - otherwise writing it twice would leave two
        // references behind. A scalar member on a multi-cardinality reference adds, since replacing every
        // category because one was set would be surprising.
        var replaceSet = member.CollectionShape != TypedCollectionShape.Scalar
                         || cardinality is Cardinality.ZeroOrOne or Cardinality.ExactlyOne;

        return (builder, args) =>
        {
            var (value, _) = SplitArguments(descriptor, member, args);

            var keys = EnumerateValues(value)
                .Select(it => ResolvePrimaryKey(descriptor, member, it))
                .Where(it => it is not null)
                .Select(it => it!.Value)
                .ToList();

            if (member.Access == TypedMemberAccess.Remove)
            {
                RemoveReferences(builder, resolvedName, keys);
                return;
            }

            var existing = ExistingReferenceKeys(builder, resolvedName);

            if (replaceSet)
            {
                RemoveReferences(builder, resolvedName, existing.Where(it => !keys.Contains(it)).ToList());
            }

            foreach (var key in keys)
            {
                // an initial builder indexes references by key and rejects a duplicate add, so a
                // reference that is already present is left as it is
                if (existing.Contains(key))
                {
                    continue;
                }

                // the schema knows the target entity type and cardinality; only when it does not is the
                // contract's own declaration used
                if (referenceSchema is not null)
                {
                    builder.SetReference(resolvedName, key);
                }
                else
                {
                    builder.SetReference(
                        resolvedName,
                        declaration?.Entity
                        ?? throw new EvitaInvalidUsageException(
                            $"Member `{descriptor.ContractType.Name}.{member.MemberName}` cannot create " +
                            $"reference `{resolvedName}` without knowing the referenced entity type - set " +
                            $"[EvitaReference(Entity = \"...\")]."),
                        ResolveWrittenCardinality(member, declaration),
                        key
                    );
                }
            }
        };
    }

    /// <summary>
    /// Points <paramref name="referenceName"/> at <paramref name="key"/>, replacing whatever a
    /// single-cardinality reference held before.
    /// <remarks>
    /// The deep upsert establishes references itself - it only learns a child's key once the server has
    /// assigned one - so it needs the same replace-versus-add rule the declarative setters apply. Calling
    /// <see cref="IEntityEditor{T}.SetReference(string,int)"/> directly is not enough: an initial builder
    /// indexes references by (name, primary key), so setting the same reference twice leaves two behind
    /// and reads return whichever came first.
    /// </remarks>
    /// </summary>
    /// <param name="builder">builder of the entity owning the reference</param>
    /// <param name="referenceName">name of the reference to establish</param>
    /// <param name="key">primary key of the referenced entity</param>
    internal static void EstablishReference(IEntityBuilder builder, string referenceName, int key)
    {
        var existing = ExistingReferenceKeys(builder, referenceName);

        if (existing.Contains(key))
        {
            return;
        }

        // without a schema entry there is nothing to say the reference may hold more than one target, and
        // the attachment is the only thing claiming it - so replace
        var cardinality = builder.Schema.GetReference(referenceName)?.Cardinality;

        if (existing.Count > 0 && cardinality is null or Cardinality.ZeroOrOne or Cardinality.ExactlyOne)
        {
            RemoveReferences(builder, referenceName, existing);
        }

        builder.SetReference(referenceName, key);
    }

    private static void RemoveReferences(
        IEntityBuilder builder,
        string referenceName,
        IReadOnlyList<int> keys)
    {
        // an empty key list on a remover means "all of them"
        var targets = keys.Count > 0 ? keys : ExistingReferenceKeys(builder, referenceName);

        foreach (var key in targets)
        {
            builder.RemoveReference(referenceName, key);
        }
    }

    private static List<int> ExistingReferenceKeys(IEntityBuilder builder, string referenceName) =>
        builder.ReferencesAvailable()
            ? builder.GetReferences(referenceName)
                .Where(it => !it.Dropped)
                .Select(it => it.ReferencedPrimaryKey)
                .ToList()
            : [];

    /// <summary>
    /// Extracts the referenced primary key from whatever the caller passed.
    /// </summary>
    private static int? ResolvePrimaryKey(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case int key:
                return key;
            case IEntityReference reference:
                return reference.PrimaryKey;
            case ITypedEntity typed:
                return typed.Entity.PrimaryKey;
            case IEntity entity:
                return entity.PrimaryKey;
        }

        // a typed view that does not extend the mixin still exposes its entity through the factory; this
        // is tried before any numeric conversion, which throws hard rather than declining
        try
        {
            return TypedEntityFactory.GetEntity(value).PrimaryKey;
        }
        catch (EvitaInvalidUsageException)
        {
            // not a typed view - fall through to a numeric conversion
        }

        try
        {
            if (EvitaDataTypes.ToTargetType(value, typeof(int)) is int converted)
            {
                return converted;
            }
        }
        catch (Exception)
        {
            // reported below with the member that caused it
        }

        throw new EvitaInvalidUsageException(
            $"Member `{descriptor.ContractType.Name}.{member.MemberName}` cannot resolve a referenced " +
            $"primary key from `{value.GetType().Name}` - pass an int, an IEntityReference, or a typed view " +
            $"of the referenced entity."
        );
    }

    /// <summary>
    /// Cardinality used when a reference has to be created without a schema to consult.
    /// </summary>
    private static Cardinality ResolveWrittenCardinality(
        TypedMemberDescriptor member,
        EvitaReferenceAttribute? declaration)
    {
        var allowEmpty = declaration?.AllowEmpty ?? true;

        return member.CollectionShape == TypedCollectionShape.Scalar
            ? allowEmpty ? Cardinality.ZeroOrOne : Cardinality.ExactlyOne
            : allowEmpty ? Cardinality.ZeroOrMore : Cardinality.OneOrMore;
    }

    /// <summary>
    /// Flattens a written value into the items it carries, so scalar and collection members share a path.
    /// </summary>
    private static IEnumerable<object?> EnumerateValues(object? value) => value switch
    {
        null => [],
        string => [value],
        System.Collections.IEnumerable enumerable => enumerable.Cast<object?>(),
        _ => [value]
    };
}
