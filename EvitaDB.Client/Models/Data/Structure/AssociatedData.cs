using System.Globalization;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;
using Newtonsoft.Json;

namespace EvitaDB.Client.Models.Data.Structure;

public class AssociatedData : IAssociatedData
{
    [JsonIgnore] public IEntitySchema EntitySchema { get; }
    internal IDictionary<AssociatedDataKey, AssociatedDataValue> AssociatedDataValues { get; }
    [JsonIgnore] internal IDictionary<string, IAssociatedDataSchema> AssociatedDataTypes { get; }
    private ISet<string>? AssociatedDataNames { get; set; }
    private ISet<CultureInfo>? AssociatedDataLocales { get; set; }
    public bool AssociatedDataAvailable() => true;
    public bool AssociatedDataAvailable(CultureInfo locale) => true;
    public bool AssociatedDataAvailable(string associatedDataName) => true;
    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale) => true;
    private IList<AssociatedDataValue> OrderedAssociatedDataValues { get; } = new List<AssociatedDataValue>();

    public AssociatedData(
        IEntitySchema entitySchema,
        IEnumerable<AssociatedDataValue> associatedDataValues,
        IDictionary<string, IAssociatedDataSchema> associatedDataTypes
    )
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue>();
        OrderedAssociatedDataValues = associatedDataValues.ToList();
        foreach (AssociatedDataValue associatedDataValue in OrderedAssociatedDataValues)
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
        IEntitySchema entitySchema,
        IDictionary<AssociatedDataKey, AssociatedDataValue>? associatedDataValues
    )
    {
        EntitySchema = entitySchema;
        if (associatedDataValues is null)
        {
            AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue>();
        }
        else
        {
            foreach (var aData in associatedDataValues)
            {
                OrderedAssociatedDataValues.Add(aData.Value);
            }
            AssociatedDataValues = associatedDataValues;
        }
        AssociatedDataTypes = entitySchema.AssociatedData;
    }

    public AssociatedData(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue>();
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
            ? associatedDataValue.Value
            : null;
    }

    public T? GetAssociatedData<T>(string associatedDataName) where T : class
    {
        IAssociatedDataSchema associatedDataSchema =
                AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? schema)
                    ? schema
                    : throw new AssociatedDataNotFoundException(associatedDataName, EntitySchema);
        Assert.IsTrue(
                !associatedDataSchema.Localized,
                () => ContextMissingException.LocaleForAssociatedDataContextMissing(associatedDataName)
            );
        AssociatedDataValue? associatedDataValue =
            AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName), out AssociatedDataValue? value)
                ? value
                : null;

        if (associatedDataValue is null)
        {
            return default;
        }

        if (associatedDataValue.Value is ComplexDataObject complexDataObject)
        {
            return ComplexDataObjectConverter.ConvertFromComplexDataObject<T>(complexDataObject);
        }
        
        return (T) associatedDataValue.Value!;
    }

    public T? GetAssociatedData<T>(string associatedDataName, CultureInfo locale) where T : class
    {
        IAssociatedDataSchema associatedDataSchema =
            AssociatedDataTypes.TryGetValue(associatedDataName, out IAssociatedDataSchema? schema)
                ? schema
                : throw new AssociatedDataNotFoundException(associatedDataName, EntitySchema);
        
        AssociatedDataKey associatedDataKey = associatedDataSchema.Localized ?
            new AssociatedDataKey(associatedDataName, locale) :
            new AssociatedDataKey(associatedDataName);
        
        AssociatedDataValue? associatedDataValue =
            AssociatedDataValues.TryGetValue(associatedDataKey, out AssociatedDataValue? value)
                ? value
                : null;

        if (associatedDataValue is null)
        {
            return default;
        }

        if (associatedDataValue.Value is ComplexDataObject complexDataObject)
        {
            return ComplexDataObjectConverter.ConvertFromComplexDataObject<T>(complexDataObject);
        }
        
        return (T) associatedDataValue.Value!;
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
            ? (object[]?) attributeValue.Value
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
            ? associatedDataValue.Value
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
            ? (object[]?) associatedDataValue.Value
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
        return AssociatedDataNames ??= AssociatedDataValues.Keys
            .Select(x => x.AssociatedDataName)
            .ToHashSet();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return AssociatedDataValues.Keys.ToHashSet();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return AssociatedDataValues.Values
            .OrderByDescending(x => x.Key.Locale?.TwoLetterISOLanguageName)
            .ThenBy(x => x.Key.AssociatedDataName)
            .ToList();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return GetAssociatedDataValues().Where(x => associatedDataName == x.Key.AssociatedDataName).ToList();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        return AssociatedDataLocales ??= AssociatedDataValues
            .Select(x => x.Key.Locale)
            .Where(x => x != null)
            .ToHashSet()!;
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

    internal AssociatedDataValue? GetAssociatedDataValueWithoutSchemaCheck(AssociatedDataKey associatedDataKey)
    {
        if (AssociatedDataValues.TryGetValue(associatedDataKey, out AssociatedDataValue? associatedDataValue))
        {
            return associatedDataValue;
        }

        if (associatedDataKey.Localized && AssociatedDataValues.TryGetValue(
                new AssociatedDataKey(associatedDataKey.AssociatedDataName),
                out AssociatedDataValue? globalAssociatedDataValue))
        {
            return globalAssociatedDataValue;
        }

        return null;
    }

    public override string ToString()
    {
        return string.Join("; ", GetAssociatedDataValues().Select(x => x.ToString()));
    }
}
