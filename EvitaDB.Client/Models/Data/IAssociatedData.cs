using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// This interface prescribes a set of methods that must be implemented by the object, that maintains set of associated data.
/// </summary>
public interface IAssociatedData
{
    /// <summary>
    /// Returns true if entity associated data were fetched along with the entity. Calling this method before calling any
    /// other method that requires associated data to be fetched will allow you to avoid <see cref="ContextMissingException"/>.
    /// </summary>
    bool AssociatedDataAvailable();
    /// <summary>
    /// Returns true if entity associated data in specified locale were fetched along with the entity. Calling this
    /// method before calling any other method that requires associated data to be fetched will allow you to avoid
    /// <see cref="ContextMissingException"/>.
    /// </summary>
    bool AssociatedDataAvailable(CultureInfo locale);
    /// <summary>
    /// Returns true if entity associated data of particular name was fetched along with the entity. Calling this method
    /// before calling any other method that requires associated data to be fetched will allow you to avoid
    /// <see cref="ContextMissingException"/>.
    /// </summary>
    bool AssociatedDataAvailable(string associatedDataName);
    /// <summary>
    /// Returns true if entity associated data of particular name in particular locale was fetched along with the entity.
    /// Calling this method before calling any other method that requires associated data to be fetched will allow you to
    /// avoid <see cref="ContextMissingException"/>.
    /// </summary>
    bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale);
    /// <summary>
    /// Returns value associated with the key or null when the associatedData is missing.
    /// </summary>
    object? GetAssociatedData(string associatedDataName);
    /// <summary>
    /// Returns value associated with the key or null when the associatedData is missing.
    /// </summary>
    T? GetAssociatedData<T>(string associatedDataName) where T : class;
    /// <summary>
    /// Returns value associated with the key or null when the associatedData is missing.
    /// </summary>
    object? GetAssociatedData(string associatedDataName, CultureInfo locale);
    /// <summary>
    /// Returns value associated with the key or null when the associatedData is missing.
    /// </summary>
    T? GetAssociatedData<T>(string associatedDataName, CultureInfo locale) where T : class;
    /// <summary>
    /// Returns array of values associated with the key or null when the associatedData is missing.
    /// </summary>
    object[]? GetAssociatedDataArray(string associatedDataName);
    /// <summary>
    /// Returns array of values associated with the key or null when the associatedData is missing.
    /// When localized associatedData is not found it is looked up in generic (non-localized) associatedDatas. This makes this
    /// method safest way how to lookup for associatedData if caller doesn't know whether it is localized or not.
    /// </summary>
    object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale);
    /// <summary>
    /// Returns array of values associated with the key or null when the associated data is missing.
    /// 
    /// Method returns wrapper dto for the associated data that contains information about the associated data version
    /// and state.
    /// </summary>
    AssociatedDataValue? GetAssociatedDataValue(string associatedDataName);
    /// <summary>
    /// Returns array of values associated with the key or null when the associated data is missing.
    /// When localized associated data is not found it is looked up in generic (non-localized) associated data. This
    /// makes this method safest way how to lookup for associated data if caller doesn't know whether it is localized
    /// or not.
    /// 
    /// Method returns wrapper dto for the associated data that contains information about the associated data version
    /// and state.
    /// </summary>
    AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale);
    /// <summary>
    /// Returns array of values associated with the key or null when the associated data is missing.
    /// When localized associated data is not found it is looked up in generic (non-localized) associated data.
    /// This makes this method the safest way how to lookup for associated data if caller doesn't know whether it is
    /// localized or not.
    /// 
    /// Method returns wrapper dto for the associated data that contains information about the associated data version
    /// and state.
    /// </summary>
    AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey);
    /// <summary>
    /// Returns definition for the associatedData of specified name.
    /// </summary>
    IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName);
    /// <summary>
    /// Returns set of all keys registered in this associatedData set. The result set is not limited to the set
    /// of currently fetched associated data.
    /// </summary>
    ISet<string> GetAssociatedDataNames();
    /// <summary>
    /// Returns set of all keys (combination of associated data name and locale) registered in this associated data.
    /// </summary>
    ISet<AssociatedDataKey> GetAssociatedDataKeys();
    /// <summary>
    /// Returns collection of all values present in this object.
    /// </summary>
    ICollection<AssociatedDataValue> GetAssociatedDataValues();
    /// <summary>
    /// Returns collection of all values of `associatedDataName` present in this object. This method has usually sense
    /// only when the associated data is present in multiple localizations.
    /// </summary>
    ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName);
    /// <summary>
    /// Method returns set of locales used in the localized associated data. The result set is not limited to the set
    /// of currently fetched associated data.
    /// </summary>
    ISet<CultureInfo> GetAssociatedDataLocales();
    
    /// <summary>
    /// Returns true if single associated data differs between first and second instance.
    /// </summary>
    static bool AnyAssociatedDataDifferBetween(IAssociatedData first, IAssociatedData second)
    {
        ICollection<AssociatedDataValue> thisValues = first.GetAssociatedDataValues();
        ICollection<AssociatedDataValue> otherValues = second.GetAssociatedDataValues();

        if (thisValues.Count != otherValues.Count)
        {
            return true;
        }

        return thisValues
            .Any(it =>
            {
                AssociatedDataKey key = it.Key;
                object? thisValue = it.Value;
                object? otherValue = key.Localized
                    ? second.GetAssociatedData(
                        key.AssociatedDataName, key.Locale!
                    )
                    : second.GetAssociatedData(
                        key.AssociatedDataName
                    );
                return QueryUtils.ValueDiffers(thisValue, otherValue);
            });
    }
}
