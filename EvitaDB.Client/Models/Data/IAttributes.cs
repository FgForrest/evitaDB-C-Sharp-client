using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// This interface prescribes a set of methods that must be implemented by the object, that maintains set of attributes.
/// </summary>
public interface IAttributes<out TS> where TS : IAttributeSchema
{
    /// <summary>
    /// Returns true if entity attributes were fetched along with the entity. Calling this method before calling any
    /// other method that requires attributes to be fetched will allow you to avoid <see cref="ContextMissingException"/>.
    /// </summary>
    bool AttributesAvailable();
    /// <summary>
    /// Returns true if entity attributes were fetched in specified locale along with the entity. Calling this method
    /// before calling any other method that requires attributes to be fetched will allow you to avoid <see cref="ContextMissingException"/>
    /// </summary>
    bool AttributesAvailable(CultureInfo locale);
    /// <summary>
    /// Returns true if entity attribute of particular name was fetched along with the entity. Calling this method
    /// before calling any other method that requires attributes to be fetched will allow you to avoid <see cref="ContextMissingException"/>
    /// </summary>
    bool AttributeAvailable(string attributeName);
    /// <summary>
    /// Returns true if entity attribute of particular name in particular locale was fetched along with the entity.
    /// Calling this method before calling any other method that requires attributes to be fetched will allow you to avoid
    /// <see cref="ContextMissingException"/>
    /// </summary>
    /// <param name="attributeName"></param>
    /// <param name="locale"></param>
    /// <returns></returns>
    bool AttributeAvailable(string attributeName, CultureInfo locale);
    /// <summary>
    /// Returns value associated with the key or null when the attribute is missing.
    /// <code>
    /// // when the name attribute is of type string
    /// string name = entity.GetAttribute("name");
    /// </code>
    /// </summary>
    object? GetAttribute(string attributeName);
    /// <summary>
    /// Returns value associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// <code>
    /// // when the name attribute is of type string
    /// string name = entity.GetAttribute("name", new CultureInfo("en-US"));
    /// </code>
    /// </summary>
    object? GetAttribute(string attributeName, CultureInfo locale);
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// </summary>
    object[]? GetAttributeArray(string attributeName);
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// </summary>
    object[]? GetAttributeArray(string attributeName, CultureInfo locale);
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// 
    /// Method returns wrapper dto for the attribute that contains information about the attribute version and state.
    /// </summary>
    AttributeValue? GetAttributeValue(string attributeName);
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// 
    /// Method returns wrapper dto for the attribute that contains information about the attribute version and state.
    /// </summary>
    AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale);
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// 
    /// Method returns wrapper dto for the attribute that contains information about the attribute version and state.
    /// </summary>
    AttributeValue? GetAttributeValue(AttributeKey attributeKey);
    /// <summary>
    /// Returns definition for the attribute of specified name.
    /// </summary>
    TS? GetAttributeSchema(string attributeName);
    /// <summary>
    /// Returns set of all attribute names registered in this attribute set. The result set is not limited to the set
    /// of currently fetched attributes.
    /// </summary>
    ISet<string> GetAttributeNames();
    /// <summary>
    /// Returns set of all keys (combination of attribute name and locale) registered in this attribute set.
    /// </summary>
    ISet<AttributeKey> GetAttributeKeys();
    /// <summary>
    /// Returns collection of all values present in this object.
    /// </summary>
    ICollection<AttributeValue> GetAttributeValues();
    /// <summary>
    /// Returns array of values associated with the key or null when the attribute is missing.
    /// When localized attribute is not found it is looked up in generic (non-localized) attributes. This makes this
    /// method the safest way how to lookup for attribute if caller doesn't know whether it is localized or not.
    /// 
    /// Method returns wrapper dto for the attribute that contains information about the attribute version and state.
    /// </summary>
    ICollection<AttributeValue> GetAttributeValues(string attributeName);
    /// <summary>
    /// Method returns set of all locales used in the localized attributes. The result set is not limited to the set
    /// of currently fetched attributes.
    /// </summary>
    ISet<CultureInfo> GetAttributeLocales();

    /// <summary>
    /// Returns true if single attribute differs between first and second instance.
    /// </summary>
    static bool AnyAttributeDifferBetween(IAttributes<TS> first, IAttributes<TS> second)
    {
        IEnumerable<AttributeValue> thisValues =
            first.AttributesAvailable() ? first.GetAttributeValues() : new List<AttributeValue>();
        IEnumerable<AttributeValue> otherValues =
            second.AttributesAvailable() ? second.GetAttributeValues() : new List<AttributeValue>();

        if (thisValues.Count() != otherValues.Count())
        {
            return true;
        }

        return thisValues
            .Any(it =>
            {
                object? thisValue = it.Value;
                AttributeKey key = it.Key;
                AttributeValue? other = second.GetAttributeValue(key.AttributeName, key.Locale!);
                if (other == null)
                {
                    return true;
                }

                object? otherValue = other.Value;
                return it.Dropped != other.Dropped || QueryUtils.ValueDiffers(thisValue, otherValue);
            });
    }
}
