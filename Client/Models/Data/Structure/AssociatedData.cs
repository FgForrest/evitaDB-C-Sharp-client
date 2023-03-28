using System.Globalization;
using Client.Models.Schemas.Dtos;
using Client.Queries;

namespace Client.Models.Data.Structure;

public class AssociatedData : IAssociatedData
{
    public EntitySchema EntitySchema { get; }
    private IDictionary<AssociatedDataKey, AssociatedDataValue?> AssociatedDataValues { get; }
    private IDictionary<string, AssociatedDataSchema> AssociatedDataTypes { get; }
    private ISet<string>? AssociatedDataNames { get; set; }
    private ISet<CultureInfo>? AssociatedDataLocales { get; set; }

    public AssociatedData(
        EntitySchema entitySchema,
        ISet<AssociatedDataKey> associatedDataKeys,
        ICollection<AssociatedDataValue>? associatedDataValues
    )
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue?>();
        foreach (AssociatedDataKey associatedDataKey in associatedDataKeys)
        {
            AssociatedDataValues.Add(associatedDataKey, null);
        }

        if (associatedDataValues != null)
        {
            foreach (AssociatedDataValue associatedDataValue in associatedDataValues)
            {
                AssociatedDataValues.Add(associatedDataValue.Key, associatedDataValue);
            }
        }

        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    /**
	 * Constructor should be used only when associated data are loaded from persistent storage.
	 * Constructor is meant to be internal to the Evita engine.
	 */
    public AssociatedData(
        EntitySchema entitySchema,
        ICollection<AssociatedDataValue?>? associatedDataValues
    )
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = associatedDataValues is null
            ? new Dictionary<AssociatedDataKey, AssociatedDataValue?>()
            : associatedDataValues
                .ToDictionary(x => x?.Key, x => x)!;
        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    public AssociatedData(EntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue?>();
        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    public object? GetAssociatedData(string associatedDataName)
    {
        return AssociatedDataValues[new AssociatedDataKey(associatedDataName)]?.Value;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        return AssociatedDataValues[new AssociatedDataKey(associatedDataName)]?.Value as object[] ?? null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        return AssociatedDataValues[new AssociatedDataKey(associatedDataName)];
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataValues[new AssociatedDataKey(associatedDataName, locale)]?.Value ??
               GetAssociatedData(associatedDataName);
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        return (object[]?) AssociatedDataValues[new AssociatedDataKey(associatedDataName, locale)]?.Value ??
               GetAssociatedData(associatedDataName) as object[];
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataValues[new AssociatedDataKey(associatedDataName, locale)] ??
               AssociatedDataValues[new AssociatedDataKey(associatedDataName)];
    }

    public AssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return AssociatedDataTypes[associatedDataName];
    }

    public ISet<string> GetAssociatedDataNames()
    {
        if (AssociatedDataNames is null)
        {
            AssociatedDataNames = AssociatedDataValues.Keys
                .Select(x => x.AssociatedDataName)
                .ToHashSet();
        }

        return AssociatedDataNames;
    }
    
    public ISet<AssociatedDataKey> GetAssociatedDataKeys() {
        return AssociatedDataValues.Keys.ToHashSet();
    }
    
    public ICollection<AssociatedDataValue> GetAssociatedDataValues() {
        return AssociatedDataValues.Values.Where(x=>x != null).ToList()!;
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName) {
        return AssociatedDataValues.Where(x=>associatedDataName == x.Key.AssociatedDataName)
            .Select(x=>x.Value)
            .ToList()!;
    }
    
    public ISet<CultureInfo> GetAssociatedDataLocales() {
        if (AssociatedDataLocales == null) {
            AssociatedDataLocales = AssociatedDataValues
                .Select(x=>x.Key.Locale)
                .Where(x=>x != null)
                .ToHashSet()!;
        }
        return AssociatedDataLocales;
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        return AssociatedDataValues[associatedDataKey];
    }

    public static bool AnyAssociatedDataDifferBetween(AssociatedData first, AssociatedData second)
    {
        ICollection<AssociatedDataValue> thisValues = first.GetAssociatedDataValues();
        ICollection<AssociatedDataValue> otherValues = second.GetAssociatedDataValues();

        if (thisValues.Count != otherValues.Count)
        {
            return true;
        }

        return first.AssociatedDataValues
            .Any(it =>
            {
                AssociatedDataKey key = it.Key;
                object? thisValue = it.Value;
                object? otherValue = second.GetAssociatedData(
                    key.AssociatedDataName, key.Locale
                );
                return QueryUtils.ValueDiffers(thisValue, otherValue);
            });
    }
}