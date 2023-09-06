using System.Globalization;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data;

public interface IAssociatedData
{
    bool AssociatedDataAvailable();
    bool AssociatedDataAvailable(CultureInfo locale);
    bool AssociatedDataAvailable(string associatedDataName);
    bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale);
    object? GetAssociatedData(string associatedDataName);
    object? GetAssociatedData(string associatedDataName, CultureInfo locale);
    object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale);
    AssociatedDataValue? GetAssociatedDataValue(string associatedDataName);
    AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale);
    IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName);
    ISet<string> GetAssociatedDataNames();
    ISet<AssociatedDataKey> GetAssociatedDataKeys();
    ICollection<AssociatedDataValue> GetAssociatedDataValues();
    ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName);
    ISet<CultureInfo> GetAssociatedDataLocales();
    
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