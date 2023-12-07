using System.Globalization;

namespace EvitaDB.Client.Models.Schemas.Dtos;

/// <summary>
/// This enum represents the uniqueness type of an <see cref="AttributeSchema"/>. It is used to determine whether the attribute
/// value must be unique among all the entity attributes of this type or whether it must be unique only among attributes
/// of the same locale.
/// </summary>
public enum AttributeUniquenessType
{
    /// <summary>
    /// The attribute is not unique (default).
    /// </summary>
    NotUnique,
    /// <summary>
    /// The attribute value must be unique among all the entities of the same collection.
    /// </summary>
    UniqueWithinCollection,
    /// <summary>
    /// The localized attribute value must be unique among all values of the same <see cref="CultureInfo"/> among all the entities
    /// using of the same collection.
    /// </summary>
    UniqueWithinCollectionLocale
}
