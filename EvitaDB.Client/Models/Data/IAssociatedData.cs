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
    
    static bool AnyAssociatedDataDifferBetween(IAssociatedData first, IAssociatedData second) {
        IEnumerable<AssociatedDataValue> thisValues = first.AssociatedDataAvailable() ? first.GetAssociatedDataValues() : new List<AssociatedDataValue>();
        IEnumerable<AssociatedDataValue> otherValues = second.AssociatedDataAvailable() ? second.GetAssociatedDataValues() : new List<AssociatedDataValue>();

        if (thisValues.Count() != otherValues.Count()) {
            return true;
        } else {
            return thisValues
                .Any(it => {
                AssociatedDataKey key = it.Key;
                object? thisValue = it.Value;
                object? otherValue = second.GetAssociatedData(
                    key.AssociatedDataName, key.Locale
                );
                return QueryUtils.ValueDiffers(thisValue, otherValue);
            });
        }
    }
}