using System.Globalization;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// If any filter constraint of the query targets a localized attribute, the `entityLocaleEquals` must also be provided,
/// otherwise the query interpreter will return an error. Localized attributes must be identified by both their name and
/// <see cref="CultureInfo"/> in order to be used.
/// Only a single occurrence of entityLocaleEquals is allowed in the filter part of the query. Currently, there is no way
/// to switch context between different parts of the filter and build queries such as find a product whose name in en-US
/// is "screwdriver" or in cs is "šroubovák".
/// Also, it's not possible to omit the language specification for a localized attribute and ask questions like: find
/// a product whose name in any language is "screwdriver".
/// Example:
/// <code>
/// query(
///     collection("Product"),
///     filterBy(
///         hierarchyWithin(
///             "categories",
///             attributeEquals("code", "vouchers-for-shareholders")
///         ),
///         entityLocaleEquals("en")
///     ),
///     require(
///        entityFetch(
///            attributeContent("code", "name")
///        )
///     )
/// )
/// </code>
/// </summary>
public class EntityLocaleEquals : AbstractFilterConstraintLeaf
{
    private EntityLocaleEquals(params object?[] arguments) : base(arguments)
    {
    }
    
    public EntityLocaleEquals(CultureInfo locale) : base(locale)
    {
    }
    
    public CultureInfo Locale => (Arguments[0] as CultureInfo)!;
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
}
