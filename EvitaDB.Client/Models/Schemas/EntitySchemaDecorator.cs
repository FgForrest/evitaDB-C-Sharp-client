using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Catalog schema decorator is a mere implementation of the <see cref="ISealedEntitySchema"/> that creates an instance
/// of the <see cref="IEntitySchemaBuilder"/> on seal breaking operations.
/// </summary>
public class EntitySchemaDecorator : ISealedEntitySchema
{
    private Func<ICatalogSchema> CatalogSchemaSupplier { get; }
    public EntitySchema Delegate { get; }

    public EntitySchemaDecorator(Func<ICatalogSchema> catalogSchemaSupplier, EntitySchema entitySchema)
    {
        CatalogSchemaSupplier = catalogSchemaSupplier;
        Delegate = entitySchema;
    }
    
    public IEntitySchemaBuilder OpenForWrite()
    {
        ICatalogSchema catalogSchema = CatalogSchemaSupplier.Invoke();
        return new InternalEntitySchemaBuilder(catalogSchema, Delegate);
    }

    public IEntitySchemaBuilder WithMutations(params IEntitySchemaMutation[] schemaMutations)
    {
        ICatalogSchema catalogSchema = CatalogSchemaSupplier.Invoke();
        return new InternalEntitySchemaBuilder(
            catalogSchema,
            Delegate,
            schemaMutations
        );
    }
    
    public IEntitySchemaBuilder WithMutations(ICollection<IEntitySchemaMutation> schemaMutations)
    {
        ICatalogSchema catalogSchema = CatalogSchemaSupplier.Invoke();
        return new InternalEntitySchemaBuilder(
            catalogSchema,
            Delegate,
            schemaMutations
        );
    }

    public int Version => Delegate.Version;

    public string Name => Delegate.Name;

    public string? Description => Delegate.Description;

    public IDictionary<NamingConvention, string?> NameVariants => Delegate.NameVariants;

    public string? GetNameVariant(NamingConvention namingConvention)
    {
        return Delegate.GetNameVariant(namingConvention);
    }

    public string? DeprecationNotice => Delegate.DeprecationNotice;

    public IDictionary<string, IEntityAttributeSchema> GetAttributes()
    {
        return Delegate.GetAttributes();
    }
    
    public bool DiffersFrom(IEntitySchema? otherSchema)
    {
        return Delegate.DiffersFrom(otherSchema);
    }

    public ISet<EvolutionMode> GetEvolutionMode()
    {
        return Delegate.GetEvolutionMode();
    }

    public bool Allows(EvolutionMode evolutionMode)
    {
        return Delegate.Allows(evolutionMode);
    }

    public bool SupportsLocale(CultureInfo locale)
    {
        return Delegate.SupportsLocale(locale);
    }

    public IAttributeSchema GetAttributeOrThrow(string name)
    {
        return Delegate.GetAttributeOrThrow(name);
    }

    public bool WithGeneratedPrimaryKey => Delegate.WithGeneratedPrimaryKey;

    public bool WithHierarchy => Delegate.WithHierarchy;

    public bool WithPrice => Delegate.WithPrice;

    public int IndexedPricePlaces => Delegate.IndexedPricePlaces;

    public ISet<CultureInfo> Locales => Delegate.Locales;

    public ISet<Currency> Currencies => Delegate.Currencies;

    public ISet<EvolutionMode> EvolutionModes => Delegate.EvolutionModes;
    public IEnumerable<IEntityAttributeSchema> NonNullableAttributes => Delegate.NonNullableAttributes;
    
    public IEnumerable<IAssociatedDataSchema> NonNullableAssociatedData => Delegate.NonNullableAssociatedData;
    public IDictionary<string, IEntityAttributeSchema> Attributes => Delegate.Attributes;
    
    public IDictionary<string, IAssociatedDataSchema> AssociatedData => Delegate.AssociatedData;

    public IDictionary<string, IReferenceSchema> References => Delegate.References;

    public bool IsBlank()
    {
        return Delegate.IsBlank();
    }

    public IAssociatedDataSchema? GetAssociatedData(string name)
    {
        return Delegate.GetAssociatedData(name);
    }

    public IAssociatedDataSchema GetAssociatedDataOrThrow(string name)
    {
        return Delegate.GetAssociatedDataOrThrow(name);
    }

    public IAssociatedDataSchema? GetAssociatedDataByName(string dataName, NamingConvention namingConvention)
    {
        return Delegate.GetAssociatedDataByName(dataName, namingConvention);
    }

    public IReferenceSchema? GetReference(string name)
    {
        return Delegate.GetReference(name);
    }

    public IReferenceSchema? GetReferenceByName(string dataName, NamingConvention namingConvention)
    {
        return Delegate.GetReferenceByName(dataName, namingConvention);
    }

    public IReferenceSchema GetReferenceOrThrowException(string referenceName)
    {
        return Delegate.GetReferenceOrThrowException(referenceName);
    }

    public IEntityAttributeSchema? GetAttribute(string name)
    {
        return Delegate.GetAttribute(name);
    }

    public IEntityAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
    {
        return Delegate.GetAttributeByName(name, namingConvention);
    }

    public IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds()
    {
        return Delegate.GetSortableAttributeCompounds();
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name)
    {
        return Delegate.GetSortableAttributeCompound(name);
    }

    public SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name, NamingConvention namingConvention)
    {
        return Delegate.GetSortableAttributeCompoundByName(name, namingConvention);
    }

    public IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName)
    {
        return Delegate.GetSortableAttributeCompoundsForAttribute(attributeName);
    }
}