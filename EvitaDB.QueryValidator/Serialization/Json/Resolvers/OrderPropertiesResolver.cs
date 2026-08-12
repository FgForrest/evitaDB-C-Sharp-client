using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EvitaDB.QueryValidator.Serialization.Json.Resolvers;

public class OrderPropertiesResolver : IgnoreNullablesWithDefaultValuesResolver
{
    /// <summary>
    /// Derived accessors emitted after the real data fields.
    ///
    /// Properties are otherwise ordered alphabetically, which matches the documentation fixtures for every
    /// plain data field. `primaryKeyOrThrowException` is not a field though - it is a convenience accessor
    /// over `primaryKey` - and Jackson, which generated those fixtures, emits it after `type` because that is
    /// where it sits on the Java interface. Sorting it alphabetically would place it before `type` and every
    /// `hierarchyContent` fixture would differ by that one line.
    /// </summary>
    private static readonly IReadOnlyList<string> TrailingProperties = ["primaryKeyOrThrowException"];

    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> baseProperty = base.CreateProperties(type, memberSerialization);
        return baseProperty
            .OrderBy(p => TrailingProperties.Contains(p.PropertyName) ? 1 : 0)
            .ThenBy(p => p.PropertyName, StringComparer.Ordinal)
            .ToList();
    }
}