using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public class AssociatedData : IAssociatedData
{
    [JsonIgnore] public IEntitySchema EntitySchema { get; }
    private IDictionary<AssociatedDataKey, AssociatedDataValue?> AssociatedDataValues { get; }
    [JsonIgnore] private IDictionary<string, IAssociatedDataSchema> AssociatedDataTypes { get; }
    private ISet<string>? AssociatedDataNames { get; set; }
    private ISet<CultureInfo>? AssociatedDataLocales { get; set; }
    public bool AssociatedDataAvailable() => true;
    public bool AssociatedDataAvailable(CultureInfo locale) => true;

    public bool AssociatedDataAvailable(string associatedDataName) => true;

    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale) => true;

    public AssociatedData(
        IEntitySchema entitySchema,
        IEnumerable<AssociatedDataKey> associatedDataKeys,
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

    public AssociatedData(
        IEntitySchema entitySchema,
        IEnumerable<AssociatedDataValue> associatedDataValues,
        IDictionary<string, IAssociatedDataSchema> associatedDataTypes
    )
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue?>();
        foreach (AssociatedDataValue associatedDataValue in associatedDataValues)
        {
            AssociatedDataValues.Add(associatedDataValue.Key, associatedDataValue);
        }

        AssociatedDataTypes = associatedDataTypes;
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
                .ToDictionary(x => x!.Key, x => x);
        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    public AssociatedData(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue?>();
        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    public object? GetAssociatedData(string associatedDataName)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        Assert.IsTrue(!associatedDataSchema.Localized,
            () => ContextMissingException.LocaleForAssociatedDataContextMissing(associatedDataName));
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName),
            out AssociatedDataValue? associatedDataValue)
            ? associatedDataValue?.Value
            : null;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        Assert.IsTrue(!associatedDataSchema.Localized,
            () => ContextMissingException.LocaleForAttributeContextMissing(associatedDataName));
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName), out var attributeValue)
            ? (object[]?) attributeValue?.Value
            : null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        return associatedDataSchema.Localized
            ? null
            : AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName), out var associatedDataValue)
                ? associatedDataValue
                : null;
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        AssociatedDataKey associatedDataKey = associatedDataSchema.Localized
            ? new AssociatedDataKey(associatedDataName, locale)
            : new AssociatedDataKey(associatedDataName);
        return AssociatedDataValues.TryGetValue(associatedDataKey, out var associatedDataValue)
            ? associatedDataValue?.Value
            : null;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        AssociatedDataKey associatedDataKey = associatedDataSchema.Localized
            ? new AssociatedDataKey(associatedDataName, locale)
            : new AssociatedDataKey(associatedDataName);
        return AssociatedDataValues.TryGetValue(associatedDataKey, out var associatedDataValue)
            ? (object[]?) associatedDataValue?.Value
            : null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        if (!AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(associatedDataName, EntitySchema);
        }

        AssociatedDataKey associatedDataKey = associatedDataSchema.Localized
            ? new AssociatedDataKey(associatedDataName, locale)
            : new AssociatedDataKey(associatedDataName);
        return AssociatedDataValues.TryGetValue(associatedDataKey, out var associatedDataValue)
            ? associatedDataValue
            : null;
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? associatedDataSchema)
            ? associatedDataSchema
            : null;
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

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return AssociatedDataValues.Keys.ToHashSet();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return AssociatedDataValues.Values.Where(x => x != null)
            .OrderByDescending(x => x?.Key.Locale?.TwoLetterISOLanguageName).ThenBy(x => x?.Key.AssociatedDataName)
            .ToList()!;
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return GetAssociatedDataValues().Where(x => associatedDataName == x.Key.AssociatedDataName).ToList();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        if (AssociatedDataLocales == null)
        {
            AssociatedDataLocales = AssociatedDataValues
                .Select(x => x.Key.Locale)
                .Where(x => x != null)
                .ToHashSet()!;
        }

        return AssociatedDataLocales;
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        string attributeName = associatedDataKey.AssociatedDataName;
        if (!AssociatedDataTypes.TryGetValue(attributeName, out IAssociatedDataSchema? associatedDataSchema))
        {
            throw new AttributeNotFoundException(attributeName, EntitySchema);
        }

        AssociatedDataKey associatedDataKeyToUse = associatedDataSchema.Localized
            ? associatedDataKey
            : associatedDataKey.Localized
                ? new AssociatedDataKey(attributeName)
                : associatedDataKey;
        return AssociatedDataValues.TryGetValue(associatedDataKeyToUse, out var associatedDataValue)
            ? associatedDataValue
            : null;
    }

    public override string ToString()
    {
        return string.Join("; ", GetAssociatedDataValues().Select(x => x.ToString()));
    }
}