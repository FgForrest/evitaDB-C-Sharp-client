using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Class supports intermediate mutable object that allows <see cref="AssociatedData"/> container rebuilding.
/// We need to closely monitor what associatedData is changed and how. These changes are wrapped in so called mutations
/// (see <see cref="AssociatedDataMutation"/> and its implementations) and mutations can be then processed transactionally by
/// the engine.
/// </summary>
public class ExistingAssociatedDataBuilder : IAssociatedDataBuilder
{
    private IEntitySchema EntitySchema { get; }
    private AssociatedData BaseAssociatedData { get; }
    private IDictionary<AssociatedDataKey, AssociatedDataMutation> AssociatedDataMutations { get; }

    public ExistingAssociatedDataBuilder(
        IEntitySchema entitySchema,
        AssociatedData baseAssociatedData
    )
    {
        EntitySchema = entitySchema;
        AssociatedDataMutations = new Dictionary<AssociatedDataKey, AssociatedDataMutation>();
        BaseAssociatedData = baseAssociatedData;
    }

    public bool AssociatedDataAvailable()
    {
        return BaseAssociatedData.AssociatedDataAvailable();
    }

    public bool AssociatedDataAvailable(CultureInfo locale)
    {
        return BaseAssociatedData.AssociatedDataAvailable(locale);
    }

    public bool AssociatedDataAvailable(string associatedDataName)
    {
        return BaseAssociatedData.AssociatedDataAvailable(associatedDataName);
    }

    public bool AssociatedDataAvailable(string associatedDataName, CultureInfo locale)
    {
        return BaseAssociatedData.AssociatedDataAvailable(associatedDataName, locale);
    }
    
    public void AddMutation(AssociatedDataMutation localMutation)
    {
        if (localMutation is UpsertAssociatedDataMutation upsertAssociatedDataMutation)
        {
            AssociatedDataKey associatedDataKey = upsertAssociatedDataMutation.AssociatedDataKey;
            object associatedDataValue = upsertAssociatedDataMutation.Value;
            InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(
                BaseAssociatedData.EntitySchema,
                associatedDataKey.AssociatedDataName, associatedDataValue.GetType(), associatedDataKey.Locale!
            );

            AssociatedDataMutations.Add(associatedDataKey, upsertAssociatedDataMutation);
        }
        else if (localMutation is RemoveAssociatedDataMutation removeAssociatedDataMutation)
        {
            AssociatedDataKey associatedDataKey = removeAssociatedDataMutation.AssociatedDataKey;
            VerifyAssociatedDataExists(associatedDataKey);
            if (BaseAssociatedData.GetAssociatedDataValueWithoutSchemaCheck(associatedDataKey) is null)
            {
                AssociatedDataMutations.Remove(associatedDataKey);
            }
            else
            {
                AssociatedDataMutations.Add(associatedDataKey, removeAssociatedDataMutation);
            }
        }
        else
        {
            throw new EvitaInternalError("Unknown Evita associated data mutation: `" + localMutation.GetType() + "`!");
        }
    }
    

    public object? GetAssociatedData(string associatedDataName)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName))?.Value;
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName, locale))?.Value;
    }

    public object[]? GetAssociatedDataArray(string associatedDataName)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName))?.Value as object[];
    }

    public object[]? GetAssociatedDataArray(string associatedDataName, CultureInfo locale)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName, locale))?.Value as object[];
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName));
    }

    public AssociatedDataValue? GetAssociatedDataValue(string associatedDataName, CultureInfo locale)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName, locale));
    }

    public AssociatedDataValue? GetAssociatedDataValue(AssociatedDataKey associatedDataKey)
    {
        return GetAssociatedDataValueInternal(associatedDataKey) ?? 
               (associatedDataKey.Localized ? GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataKey.AssociatedDataName)) : null);
    }

    public IAssociatedDataSchema? GetAssociatedDataSchema(string associatedDataName)
    {
        return BaseAssociatedData.GetAssociatedDataSchema(associatedDataName);
    }

    public ISet<string> GetAssociatedDataNames()
    {
        return GetAssociatedDataValues().Select(it => it.Key.AssociatedDataName).ToHashSet();
    }

    public ISet<AssociatedDataKey> GetAssociatedDataKeys()
    {
        return GetAssociatedDataValues()
            .Select(x=>x.Key)
            .ToHashSet();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues()
    {
        return GetAssociatedDataValuesWithoutPredicate().ToList();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return GetAssociatedDataValuesWithoutPredicate()
            .Where(x=>x.Key.AssociatedDataName == associatedDataName).ToList();
    }

    public ISet<CultureInfo> GetAssociatedDataLocales()
    {
        return GetAssociatedDataValues()
            .Select(it => it.Key.Locale)
            .Where(x => x is not null)
            .ToHashSet()!;
    }

    public IAssociatedDataBuilder RemoveAssociatedData(string associatedDataName)
    {
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        VerifyAssociatedDataExists(associatedDataKey);
        AssociatedDataMutations.Add(
            associatedDataKey,
            new RemoveAssociatedDataMutation(associatedDataKey)
        );
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, object? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType());
        AssociatedDataMutations.Add(associatedDataKey, new UpsertAssociatedDataMutation(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, object[]? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType());
        AssociatedDataMutations.Add(associatedDataKey, new UpsertAssociatedDataMutation(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder RemoveAssociatedData(string associatedDataName, CultureInfo locale)
    {
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName, locale);
        VerifyAssociatedDataExists(associatedDataKey);
        AssociatedDataMutations.Add(
            associatedDataKey,
            new RemoveAssociatedDataMutation(associatedDataKey)
        );
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
        InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType(), locale);
        AssociatedDataMutations.Add(associatedDataKey, new UpsertAssociatedDataMutation(associatedDataKey, valueToStore));
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
        InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType(), locale);
        AssociatedDataMutations.Add(associatedDataKey, new UpsertAssociatedDataMutation(associatedDataKey, valueToStore));
        return this;
    }

    public IAssociatedDataBuilder MutateAssociatedData(AssociatedDataMutation mutation)
    {
        AssociatedDataMutations.Add(mutation.AssociatedDataKey, mutation);
        return this;
    }

    public IEnumerable<AssociatedDataMutation> BuildChangeSet()
    {
        IDictionary<AssociatedDataKey, AssociatedDataValue> builtAssociatedData =
            new Dictionary<AssociatedDataKey, AssociatedDataValue>(BaseAssociatedData.AssociatedDataValues);
        return AssociatedDataMutations.Values
            .Where(it =>
            {
                AssociatedDataValue? existingValue = builtAssociatedData.TryGetValue(it.AssociatedDataKey, out AssociatedDataValue? value)
                    ? value : null;
                AssociatedDataValue newAssociatedData = it.MutateLocal(EntitySchema, existingValue);
                builtAssociatedData.Add(it.AssociatedDataKey, newAssociatedData);
                return existingValue == null || newAssociatedData.Version > existingValue.Version;
            });
    }

    public AssociatedData Build()
    {
        if (!AnyChangeInMutations())
        {
            return BaseAssociatedData;
        }

        ICollection<AssociatedDataValue> newAssociatedDataValues = GetAssociatedDataValuesWithoutPredicate();
        IDictionary<string, IAssociatedDataSchema> newAssociatedDataTypes = BaseAssociatedData.AssociatedDataTypes.Values
            .Concat(newAssociatedDataValues
                .Where(it => !BaseAssociatedData.AssociatedDataTypes.ContainsKey(it.Key.AssociatedDataName))
                .Select(IAssociatedDataBuilder.CreateImplicitSchema))
            .ToImmutableDictionary(x => x.Name, x => x);

        return new AssociatedData(
            BaseAssociatedData.EntitySchema,
            newAssociatedDataValues,
            newAssociatedDataTypes
        );
    }
    
    private void VerifyAssociatedDataExists(AssociatedDataKey associatedDataKey) {
        Assert.IsTrue(
            BaseAssociatedData.GetAssociatedDataValueWithoutSchemaCheck(associatedDataKey) is not null || AssociatedDataMutations.TryGetValue(associatedDataKey, out AssociatedDataMutation? value) && value is UpsertAssociatedDataMutation,
            "Associated data `" + associatedDataKey + "` doesn't exist!"
        );
    }
    
    private AssociatedDataValue? GetAssociatedDataValueInternal(AssociatedDataKey associatedDataKey) {
        AssociatedDataValue? associatedDataValue = BaseAssociatedData.AssociatedDataValues.TryGetValue(associatedDataKey, out AssociatedDataValue? value) ? value : null;
        return AssociatedDataMutations.TryGetValue(associatedDataKey, out AssociatedDataMutation? mutation) ? mutation.MutateLocal(EntitySchema, associatedDataValue) : null;
    }
    
    private List<AssociatedDataValue> GetAssociatedDataValuesWithoutPredicate()
    {
        List<AssociatedDataValue> result = new List<AssociatedDataValue>();
        foreach (var (key, value) in BaseAssociatedData.AssociatedDataValues)
        {
            if (AssociatedDataMutations.TryGetValue(key, out AssociatedDataMutation? associatedDataMutation))
            {
                AssociatedDataValue mutatedAssociatedData = associatedDataMutation.MutateLocal(EntitySchema, value);
                result.Add(mutatedAssociatedData.DiffersFrom(value) ? mutatedAssociatedData : value);
            }
            else
            {
                result.Add(value);
            }
        }

        result.AddRange(
            AssociatedDataMutations.Values
                .Where(it => !BaseAssociatedData.AssociatedDataValues.ContainsKey(it.AssociatedDataKey))
                .Select(it => it.MutateLocal(EntitySchema, null))
        );

        return result;
    }
    
    private bool AnyChangeInMutations()
    {
        return BaseAssociatedData.AssociatedDataValues
            .Select(it =>
                AssociatedDataMutations.TryGetValue(it.Key, out AssociatedDataMutation? associatedDataMutation) &&
                associatedDataMutation.MutateLocal(EntitySchema, it.Value).DiffersFrom(it.Value))
            .Concat(AssociatedDataMutations
                .Values
                .Where(it => !BaseAssociatedData.AssociatedDataValues.ContainsKey(it.AssociatedDataKey))
                .Select(_ => true))
            .Any(t => t);
    }
}