using System.Globalization;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IAssociatedData
{
    bool AssociatedDataAvailable { get; }
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
}