using System.Globalization;
using System.Reflection;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Price and associated-data readers of <see cref="BoundTypedContract"/>.
/// </summary>
public sealed partial class BoundTypedContract
{
    /// <summary>
    /// Generic <see cref="IAssociatedData.GetAssociatedData{T}(string)"/>, used to deserialize a stored
    /// complex object into a member's POCO type.
    /// </summary>
    private static readonly MethodInfo GetComplexAssociatedData = typeof(IAssociatedData)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(it => it is { Name: nameof(IAssociatedData.GetAssociatedData), IsGenericMethod: true }
                      && it.GetParameters() is [{ ParameterType: var only }] && only == typeof(string));

    /// <summary>
    /// Generic <see cref="IAssociatedData.GetAssociatedData{T}(string, CultureInfo)"/>.
    /// </summary>
    private static readonly MethodInfo GetLocalizedComplexAssociatedData = typeof(IAssociatedData)
        .GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Single(it => it is { Name: nameof(IAssociatedData.GetAssociatedData), IsGenericMethod: true }
                      && it.GetParameters().Length == 2);

    /// <summary>
    /// Builds a reader for a member bound to associated data.
    /// <remarks>
    /// The per-name availability predicates are consulted, which is the most precise signal the API
    /// offers - though in the current driver every implementation of them returns a hard-coded
    /// <c>true</c>, so an unfetched payload is today indistinguishable from an absent one (the same
    /// limitation attributes have).
    /// </remarks>
    /// </summary>
    private static Func<object, object?[]?, object?>? BuildAssociatedDataReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema)
    {
        var dataName = member.DataName!;
        var dataSchema = schema is null
            ? null
            : schema.GetAssociatedData(dataName)
              ?? schema.GetAssociatedDataByName(dataName, Utils.NamingConvention.CamelCase);

        if (dataSchema is null && schema is not null)
        {
            if (member.NameDeclaredExplicitly)
            {
                throw new EvitaInvalidUsageException(
                    $"Associated data `{dataName}` referenced by " +
                    $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of " +
                    $"entity `{schema.Name}`."
                );
            }

            // an inferred member declines quietly, like the attribute inference branch
            return null;
        }

        var resolvedName = dataSchema?.Name ?? dataName;
        var isCollection = member.CollectionShape != TypedCollectionShape.Scalar;

        // a member typed as one of evitaDB's own data types is read directly; anything else is
        // deserialized from the stored complex object
        var complex = !EvitaDataTypes.IsSupportedType(member.ItemType);

        if (complex && member.ItemType.IsValueType)
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` is typed " +
                $"`{member.ItemType.Name}`, which is neither a supported evitaDB data type nor a " +
                $"reference type that a stored complex object can be deserialized into."
            );
        }

        object? Read(object source, CultureInfo? locale)
        {
            var holder = (IAssociatedData)source;
            var fetched = locale is null
                ? holder.AssociatedDataAvailable(resolvedName)
                : holder.AssociatedDataAvailable(resolvedName, locale);

            if (!fetched)
            {
                return Wrap(member, null, fetched: false, member.ItemType);
            }

            // An entity assembled on the client knows only the associated data actually set on it, and
            // raises AttributeNotFoundException for any other name - whereas a server-fetched entity
            // carries the whole schema and returns null. The member's name was already validated against
            // the schema at bind time, so here the exception can only mean "this entity has no value",
            // which is the absent case.
            try
            {
                if (complex)
                {
                    var method = locale is null
                        ? GetComplexAssociatedData.MakeGenericMethod(member.ItemType)
                        : GetLocalizedComplexAssociatedData.MakeGenericMethod(member.ItemType);
                    var arguments = locale is null
                        ? new object?[] { resolvedName }
                        : [resolvedName, locale];
                    var deserialized = method.Invoke(holder, arguments);
                    return Wrap(member, deserialized, fetched: true, member.ItemType);
                }

                var raw = locale is null
                    ? isCollection
                        ? holder.GetAssociatedDataArray(resolvedName)
                        : holder.GetAssociatedData(resolvedName)
                    : isCollection
                        ? holder.GetAssociatedDataArray(resolvedName, locale)
                        : holder.GetAssociatedData(resolvedName, locale);

                return Convert(member, raw, fetched: true, decimalPlaces: 0);
            }
            catch (AttributeNotFoundException)
            {
                return Wrap(member, null, fetched: true, member.ItemType);
            }
            catch (TargetInvocationException e) when (e.InnerException is AttributeNotFoundException)
            {
                return Wrap(member, null, fetched: true, member.ItemType);
            }
        }

        if (member.HasLocaleParameter)
        {
            return (source, args) =>
            {
                var locale = args?.Length > 0 ? args[0] as CultureInfo : null;
                if (locale is null)
                {
                    throw new EvitaInvalidUsageException(
                        $"Member `{descriptor.ContractType.Name}.{member.MemberName}` requires a " +
                        $"non-null CultureInfo argument."
                    );
                }

                return Read(source, locale);
            };
        }

        return (source, _) => Read(source, null);
    }

    /// <summary>
    /// Builds a reader for a member bound to the entity's stored prices, optionally narrowed to given
    /// price lists.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildPriceReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        AssertPriceMemberType(descriptor, member);

        var priceLists = member.PriceLists;
        var single = member.CollectionShape == TypedCollectionShape.Scalar;

        return (source, _) =>
        {
            var entity = AsEntity(descriptor, member, source);

            if (!entity.PricesAvailable())
            {
                return Wrap(member, null, fetched: false, member.ItemType);
            }

            var prices = entity.GetPrices().Where(it => !it.Dropped);

            if (priceLists.Count > 0)
            {
                prices = prices.Where(it => priceLists.Contains(it.PriceList, StringComparer.Ordinal));
            }

            var selected = prices.ToList();

            return single
                ? Wrap(member, selected.Count > 0 ? selected[0] : null, fetched: true, member.ItemType)
                : Wrap(member, MaterializeCollection(member, selected), fetched: true, member.ItemType);
        };
    }

    /// <summary>
    /// Builds a reader for a member bound to the computed selling price. A collection member yields every
    /// price eligible for sale instead of just the winning one.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildPriceForSaleReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        AssertPriceMemberType(descriptor, member);

        var single = member.CollectionShape == TypedCollectionShape.Scalar;

        return (source, _) =>
        {
            var entity = AsEntity(descriptor, member, source);

            if (!entity.PricesAvailable())
            {
                return Wrap(member, null, fetched: false, member.ItemType);
            }

            if (single)
            {
                // PriceForSale requires a price context (currency + price lists) established by the
                // query; the driver raises ContextMissingException when it is absent, which is more
                // informative than anything this layer could add
                return Wrap(member, entity.PriceForSale, fetched: true, member.ItemType);
            }

            var all = entity.GetAllPricesForSale().Where(it => !it.Dropped).ToList();
            return Wrap(member, MaterializeCollection(member, all), fetched: true, member.ItemType);
        };
    }

    /// <summary>
    /// Builds a reader for a member bound to the selling-price range.
    /// <remarks>
    /// Read straight off the entity: the bounds are computed by the server and carried on the response, unlike
    /// accompanying prices which this driver still drops. Null when the query established no price context, which
    /// is the same condition under which there is no selling price either.
    /// </remarks>
    /// </summary>
    private static Func<object, object?[]?, object?> BuildPriceRangeForSaleReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        if (!typeof(PriceRangeForSale).IsAssignableFrom(member.ItemType))
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` is annotated " +
                $"[PriceRangeForSale] but its type is `{member.ItemType.Name}`. The range has three prices in " +
                $"it - the cheapest, the most expensive and the selling price - so the member must expose " +
                $"`{nameof(PriceRangeForSale)}` itself."
            );
        }

        if (member.CollectionShape != TypedCollectionShape.Scalar)
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` is annotated " +
                $"[PriceRangeForSale] as a collection. An entity has at most one range; use [PriceForSale] on a " +
                $"collection member to read every price eligible for sale."
            );
        }

        return (source, _) =>
        {
            var entity = AsEntity(descriptor, member, source);

            return entity.PricesAvailable()
                ? Wrap(member, entity.PriceRangeForSale, fetched: true, member.ItemType)
                : Wrap(member, null, fetched: false, member.ItemType);
        };
    }

    /// <summary>
    /// Builds a reader for a member bound to an accompanying price.
    /// <remarks>
    /// Read through <see cref="IAccompanyingPriceSource"/>, because this driver does not yet carry
    /// accompanying prices on its entity model - see that interface for what closing the gap involves.
    /// </remarks>
    /// </summary>
    private static Func<object, object?[]?, object?> BuildAccompanyingPriceReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        AssertPriceMemberType(descriptor, member);

        var priceName = member.DataName!;

        return (source, _) =>
        {
            if (source is not IAccompanyingPriceSource accompanying)
            {
                throw new EvitaInvalidUsageException(
                    $"Member `{descriptor.ContractType.Name}.{member.MemberName}` reads accompanying price " +
                    $"`{priceName}`, but `{source.GetType().Name}` does not supply accompanying prices. " +
                    $"This driver drops them during conversion - see IAccompanyingPriceSource."
                );
            }

            return Wrap(member, accompanying.GetAccompanyingPrice(priceName), fetched: true,
                member.ItemType);
        };
    }

    /// <summary>
    /// Prices have no scalar projection - the member must expose <see cref="IPrice"/> itself, mirroring
    /// the Java driver's requirement that the item type be assignable to <c>PriceContract</c>.
    /// </summary>
    private static void AssertPriceMemberType(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        if (!typeof(IPrice).IsAssignableFrom(member.ItemType))
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` is typed " +
                $"`{member.ItemType.Name}`, but a price member must expose `IPrice` - read the amount from " +
                $"the returned price instead."
            );
        }
    }
}
