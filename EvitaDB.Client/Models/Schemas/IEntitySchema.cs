using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public interface IEntitySchema : IVersioned, INamedSchemaWithDeprecation, ISortableAttributeCompoundSchemaProvider<IEntityAttributeSchema>
{
    bool WithGeneratedPrimaryKey { get; }
    bool WithHierarchy { get; }
    bool WithPrice { get; }
    int IndexedPricePlaces { get; }
    ISet<CultureInfo> Locales { get; }
    ISet<Currency> Currencies { get; }
    ISet<EvolutionMode> EvolutionModes { get; }
    IEnumerable<IEntityAttributeSchema> NonNullableAttributes { get; }
    IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData { get; }
    IDictionary<string, IEntityAttributeSchema> Attributes { get; }
    IDictionary<string, IAssociatedDataSchema> AssociatedData { get; }
    IDictionary<string, IReferenceSchema> References { get; }
    bool IsBlank();
    IAssociatedDataSchema? GetAssociatedData(string name);
    IAssociatedDataSchema GetAssociatedDataOrThrow(string name);
    IAssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention);
    IReferenceSchema? GetReference(string name);
    IReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention);
    IReferenceSchema GetReferenceOrThrowException(string referenceName);
    IAttributeSchema GetAttributeOrThrow(string name);
    bool DiffersFrom(IEntitySchema? otherSchema);
    ISet<EvolutionMode> GetEvolutionMode();
    bool Allows(EvolutionMode evolutionMode);
    bool SupportsLocale(CultureInfo locale);
    bool SupportsCurrency(Currency currency)
    {
        return Currencies.Contains(currency);
    }
}