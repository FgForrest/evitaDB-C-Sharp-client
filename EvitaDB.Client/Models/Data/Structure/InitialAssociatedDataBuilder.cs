using System.Globalization;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Class supports intermediate mutable object that allows <see cref="AssociatedData"/> container rebuilding.
/// Due to performance reasons, there is special implementation for the situation when entity is newly created.
/// In this case we know everything is new and we don't need to closely monitor the changes so this can speed things up.
/// </summary>
public class InitialAssociatedDataBuilder : IAssociatedDataBuilder
{
    private IEntitySchema EntitySchema { get; }
    private IDictionary<AssociatedDataKey, AssociatedDataValue> AssociatedDataValues { get; }

    internal InitialAssociatedDataBuilder(IEntitySchema entitySchema)
    {
        EntitySchema = entitySchema;
        AssociatedDataValues = new Dictionary<AssociatedDataKey, AssociatedDataValue>();
    }

    public bool AssociatedDataAvailable() => true;

    public bool AssociatedDataAvailable(CultureInfo locale) => true;

    public bool AssociatedDataAvailable(string associatedDataName) => true;

    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale) => true;

    public object? GetAssociatedData(string associatedDataName)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName),
            out AssociatedDataValue? value)
            ? value.Value
            : null;
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName, locale),
            out AssociatedDataValue? value)
            ? value.Value
            : null;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName),
            out AssociatedDataValue? value)
            ? value.Value as object[]
            : null;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName, locale),
            out AssociatedDataValue? value)
            ? value.Value as object[]
            : null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName),
            out AssociatedDataValue? value)
            ? value
            : null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        return AssociatedDataValues.TryGetValue(new AssociatedDataKey(associatedDataName, locale),
            out AssociatedDataValue? value)
            ? value
            : null;
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        if (AssociatedDataValues.TryGetValue(associatedDataKey, out AssociatedDataValue? value))
        {
            return value;
        }

        if (associatedDataKey.Localized && AssociatedDataValues.TryGetValue(
                new AssociatedDataKey(associatedDataKey.AssociatedDataName),
                out AssociatedDataValue? globalAssociatedDataValue))
        {
            return globalAssociatedDataValue;
        }

        return null;
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return EntitySchema.GetAssociatedData(associatedDataName);
    }

    public ISet<string> GetAssociatedDataNames()
    {
        return AssociatedDataValues
            .Keys
            .Select(x => x.AssociatedDataName)
            .ToHashSet();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return AssociatedDataValues.Keys.ToHashSet();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return AssociatedDataValues.Values;
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return AssociatedDataValues
            .Where(it => associatedDataName.Equals(it.Key.AssociatedDataName))
            .Select(x=>x.Value)
            .ToList();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        return AssociatedDataValues
            .Keys
            .Select(x => x.Locale)
            .Where(x => x is not null)
            .ToHashSet()!;
    }

    public IAssociatedDataBuilder RemoveAssociatedData(string associatedDataName)
    {
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        AssociatedDataValues.Remove(associatedDataKey);
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, object? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType());
        AssociatedDataValues.Add(associatedDataKey, new AssociatedDataValue(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, object[]? associatedDataValue)
    {
        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType());
        AssociatedDataValues.Add(associatedDataKey, new AssociatedDataValue(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder RemoveAssociatedData(string associatedDataName, CultureInfo locale)
    {
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName, locale);
        AssociatedDataValues.Remove(associatedDataKey);
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, CultureInfo locale,
        object? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName, locale);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName, locale);
        VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType(), locale);
        AssociatedDataValues.Add(associatedDataKey, new AssociatedDataValue(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, CultureInfo locale,
        object[]? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName, locale);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName, locale);
        VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType(), locale);
        AssociatedDataValues.Add(associatedDataKey, new AssociatedDataValue(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder MutateAssociatedData(AssociatedDataMutation mutation)
    {
        throw new NotSupportedException("You cannot apply mutation when entity is just being created!");
    }

    public IEnumerable<AssociatedDataMutation> BuildChangeSet()
    {
        throw new NotSupportedException("Initial entity creation doesn't support change monitoring - it has no sense.");
    }

    public AssociatedData Build()
    {
        IDictionary<string, IAssociatedDataSchema> associatedDataTypes = AssociatedDataValues
            .Values
            .Select(IAssociatedDataBuilder.CreateImplicitSchema)
            .ToDictionary(x => x.Name, x => x);

        return new AssociatedData(
            EntitySchema,
            AssociatedDataValues.Values,
            associatedDataTypes
        );
    }

    internal static void VerifyAssociatedDataIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string associatedDataName,
        Type? type
    )
    {
        VerifyAssociatedDataIsInSchemaAndTypeMatch(
            entitySchema, associatedDataName, type, null,
            entitySchema.GetAssociatedData(associatedDataName)
        );
    }

    internal static void VerifyAssociatedDataIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string associatedDataName,
        Type? type,
        CultureInfo locale
    )
    {
        VerifyAssociatedDataIsInSchemaAndTypeMatch(
            entitySchema, associatedDataName, type, locale,
            entitySchema.GetAssociatedData(associatedDataName)
        );
    }

    private static void VerifyAssociatedDataIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string associatedDataName,
        Type? type,
        CultureInfo? locale,
        IAssociatedDataSchema? associatedDataSchema
    )
    {
        Assert.IsTrue(
            associatedDataSchema != null || entitySchema.Allows(EvolutionMode.AddingAssociatedData),
            () => new InvalidMutationException(
                "AssociatedData " + associatedDataName + " is not configured in entity " + entitySchema.Name +
                " schema and automatic evolution is not enabled for associated data!"
            )
        );
        if (associatedDataSchema != null)
        {
            if (type != null)
            {
                Assert.IsTrue(
                    associatedDataSchema.Type.IsAssignableFrom(type),
                    () => new InvalidDataTypeMutationException(
                        "AssociatedData " + associatedDataName + " accepts only type " +
                        associatedDataSchema.GetType().Name +
                        " - value type is different: " + type.Name + "!",
                        associatedDataSchema.GetType(), type
                    )
                );
            }

            if (locale == null)
            {
                Assert.IsTrue(
                    !associatedDataSchema.Localized,
                    () => new InvalidMutationException(
                        "AssociatedData " + associatedDataName +
                        " is localized and doesn't accept non-localized associated data!"
                    )
                );
            }
            else
            {
                Assert.IsTrue(
                    associatedDataSchema.Localized,
                    () => new InvalidMutationException(
                        "AssociatedData " + associatedDataName +
                        " is not localized and doesn't accept localized associated data!"
                    )
                );
                Assert.IsTrue(
                    entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                    () => new InvalidMutationException(
                        "AssociatedData " + associatedDataName + " is localized, but schema doesn't support locale " +
                        locale + "! " +
                        "Supported locales are: " +
                        string.Join(", ", entitySchema.Locales.Select(x => x.TwoLetterISOLanguageName))
                    )
                );
            }
        }
        else if (locale != null)
        {
            // at least verify supported locale
            Assert.IsTrue(
                entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                () => new InvalidMutationException(
                    "AssociatedData " + associatedDataName + " is localized, but schema doesn't support locale " +
                    locale + "! " +
                    "Supported locales are: " +
                    string.Join(", ", entitySchema.Locales.Select(x => x.TwoLetterISOLanguageName))
                )
            );
        }
    }
}