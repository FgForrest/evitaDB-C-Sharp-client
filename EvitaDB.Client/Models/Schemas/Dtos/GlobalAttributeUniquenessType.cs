using System.Globalization;

namespace EvitaDB.Client.Models.Schemas.Dtos;

/// <summary>
/// This enum represents the uniqueness type of an <see cref="GlobalAttributeSchema"/>. It is used to determine whether
/// the attribute value must be unique among all the entities using this <see cref="GlobalAttributeSchema"/> or whether it
/// must be unique only among entities of the same locale.
/// </summary>
public enum GlobalAttributeUniquenessType
{
    /// <summary>
    /// The attribute is not unique (default).
    /// </summary>
    NotUnique,
    /// <summary>
    /// The attribute value (either localized or non-localized) must be unique among all values among all the entities
    /// using this <see cref="GlobalAttributeSchema"/> in the entire catalog.
    /// </summary>
    UniqueWithinCatalog,
    /// <summary>
    /// The localized attribute value must be unique among all values of the same <see cref="CultureInfo"/> among all the entities
    /// using this <see cref="GlobalAttributeSchema"/> in the entire catalog.
    /// </summary>
    UniqueWithinCatalogLocale
}
