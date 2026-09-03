using System.Collections.Immutable;
using System.Globalization;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.DataTypes;
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

            AssociatedDataMutations[associatedDataKey] = upsertAssociatedDataMutation;
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
                AssociatedDataMutations[associatedDataKey] = removeAssociatedDataMutation;
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

    public T? GetAssociatedData<T>(string associatedDataName) where T : class
    {
        return DeserializeAssociatedData<T>(new AssociatedDataKey(associatedDataName));
    }

    public object? GetAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return GetAssociatedDataValueInternal(new AssociatedDataKey(associatedDataName, locale))?.Value;
    }

    public T? GetAssociatedData<T>(string associatedDataName, CultureInfo locale) where T : class
    {
        return DeserializeAssociatedData<T>(new AssociatedDataKey(associatedDataName, locale));
    }

    /// <summary>
    /// Reads associated data through the pending mutations and deserializes it into <typeparamref name="T"/>.
    /// <remarks>
    /// The port read the base container here instead, so associated data written on this builder was invisible
    /// to the typed getter - and asking for data the fetched entity never carried threw rather than returning
    /// what was just written.
    /// </remarks>
    /// </summary>
    private T? DeserializeAssociatedData<T>(AssociatedDataKey associatedDataKey) where T : class
    {
        object? value = GetAssociatedDataValueInternal(associatedDataKey)?.Value;

        return value switch
        {
            null => null,
            ComplexDataObject complexDataObject =>
                ComplexDataObjectConverter.ConvertFromComplexDataObject<T>(complexDataObject),
            _ => (T)value
        };
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
        // see the same filter in ExistingAttributesBuilder.GetAttributeValues
        return GetAssociatedDataValuesWithoutPredicate().Where(it => !it.Dropped).ToList();
    }

    public ICollection<AssociatedDataValue> GetAssociatedDataValues(string associatedDataName)
    {
        return GetAssociatedDataValues()
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
        return RemoveAssociatedDataInternal(new AssociatedDataKey(associatedDataName));
    }

    public IAssociatedDataBuilder SetAssociatedData(string associatedDataName, object? associatedDataValue)
    {
        if (associatedDataValue == null) {
            return RemoveAssociatedData(associatedDataName);
        }

        object valueToStore = ComplexDataObjectConverter.GetSerializableForm(associatedDataValue);
        AssociatedDataKey associatedDataKey = new AssociatedDataKey(associatedDataName);
        InitialAssociatedDataBuilder.VerifyAssociatedDataIsInSchemaAndTypeMatch(EntitySchema, associatedDataName, valueToStore.GetType());
        AssociatedDataMutations[associatedDataKey] = new UpsertAssociatedDataMutation(associatedDataKey, valueToStore);
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
        AssociatedDataMutations[associatedDataKey] = new UpsertAssociatedDataMutation(associatedDataKey, valueToStore);
        return this;
    }

    public IAssociatedDataBuilder RemoveAssociatedData(string associatedDataName, CultureInfo locale)
    {
        return RemoveAssociatedDataInternal(new AssociatedDataKey(associatedDataName, locale));
    }

    /// <summary>
    /// Removes associated data the way Java's <c>removeAssociatedData</c> does.
    /// <remarks>
    /// When the fetched entity never carried this data there is nothing to tombstone, so the pending
    /// mutation is simply discarded. Recording a removal instead - which the port did - made the change
    /// set throw <c>InvalidMutationException: it doesn't exist</c> as soon as anything read it back.
    /// </remarks>
    /// </summary>
    private IAssociatedDataBuilder RemoveAssociatedDataInternal(AssociatedDataKey associatedDataKey)
    {
        VerifyAssociatedDataExists(associatedDataKey);

        AssociatedDataValue? baseValue =
            BaseAssociatedData.GetAssociatedDataValueWithoutSchemaCheck(associatedDataKey);

        if (baseValue is null || baseValue.Dropped)
        {
            AssociatedDataMutations.Remove(associatedDataKey);
        }
        else
        {
            AddMutation(new RemoveAssociatedDataMutation(associatedDataKey));
        }

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
        AssociatedDataMutations[associatedDataKey] = new UpsertAssociatedDataMutation(associatedDataKey, valueToStore);
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
        AssociatedDataMutations[associatedDataKey] = new UpsertAssociatedDataMutation(associatedDataKey, valueToStore);
        return this;
    }

    public IAssociatedDataBuilder MutateAssociatedData(AssociatedDataMutation mutation)
    {
        AssociatedDataMutations[mutation.AssociatedDataKey] = mutation;
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
                // overwrite, as Java's Map.put does - Dictionary.Add throws when an existing value is mutated
                builtAssociatedData[it.AssociatedDataKey] = newAssociatedData;
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
        AssociatedDataValue? baseValue =
            BaseAssociatedData.AssociatedDataValues.TryGetValue(associatedDataKey, out AssociatedDataValue? value)
                ? value
                : null;
        AssociatedDataMutations.TryGetValue(associatedDataKey, out AssociatedDataMutation? mutation);

        AssociatedDataValue? result;
        if (baseValue is not null)
        {
            // unmutated associated data reads as it stands - see the same fix in ExistingAttributesBuilder
            AssociatedDataValue? mutated = mutation?.MutateLocal(EntitySchema, baseValue);
            result = mutated is not null && mutated.DiffersFrom(baseValue) ? mutated : baseValue;
        }
        else
        {
            result = mutation?.MutateLocal(EntitySchema, null);
        }

        // Java's associatedDataPredicate defaults to Droppable::exists
        return result is null || result.Dropped ? null : result;
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