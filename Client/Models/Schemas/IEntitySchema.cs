using System.Globalization;
using Client.DataTypes;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas;

public interface IEntitySchema
{
    int Version { get; }
    string Name { get; }
    IDictionary<NamingConvention, string> NameVariants { get; }
    string? Description { get; }
    string? DeprecationNotice { get; }
    bool WithGeneratedPrimaryKey { get; }
    bool WithHierarchy { get; }
    bool WithPrice { get; }
    int IndexedPricePlaces { get; }
    ISet<CultureInfo> Locales { get; }
    ISet<Currency> Currencies { get; }
    ISet<EvolutionMode> EvolutionModes { get; }
    IEnumerable<AttributeSchema> NonNullableAttributes { get; }
    IEnumerable<AssociatedDataSchema> NonNullableAssociatedData { get; }
    IDictionary<string, AttributeSchema> Attributes { get; }
    IDictionary<string, AssociatedDataSchema> AssociatedData { get; }
    IDictionary<string, ReferenceSchema> References { get; }
    bool IsBlank();
    AssociatedDataSchema? GetAssociatedData(string name);
    AssociatedDataSchema GetAssociatedDataOrThrow(string name);
    AssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention);
    ReferenceSchema? GetReference(string name);
    ReferenceSchema GetReferenceOrThrow(string name);
    ReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention);
    AttributeSchema? GetAttribute(string name);
    AttributeSchema GetAttributeOrThrow(string name);
    AttributeSchema? GetAttributeByName(string dataName, NamingConvention namingConvention);
    bool DiffersFrom(EntitySchema? otherSchema);
    string GetNameVariant(NamingConvention namingConvention);
	
    ISet<EvolutionMode> GetEvolutionMode();
    bool Allows(EvolutionMode evolutionMode);
    bool SupportsLocale(CultureInfo locale);
}