using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Builders;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public class CatalogSchemaDecorator : ISealedCatalogSchema
{
    public CatalogSchema Delegate { get; }
    private readonly Func<string, IEntitySchema?> _entitySchemaAccessor;
    
    public CatalogSchemaDecorator(CatalogSchema catalogSchema, Func<string, IEntitySchema?> entitySchemaAccessor)
    {
        Delegate = catalogSchema;
        _entitySchemaAccessor = entitySchemaAccessor;
    }

    public ICatalogSchemaBuilder OpenForWrite()
    {
        return new InternalCatalogSchemaBuilder(this);
    }

    public ICatalogSchemaBuilder OpenForWriteWithMutations(params ILocalCatalogSchemaMutation[] schemaMutations)
    {
        return new InternalCatalogSchemaBuilder(this, schemaMutations.ToList());
    }

    public ICatalogSchemaBuilder OpenForWriteWithMutations(ICollection<ILocalCatalogSchemaMutation> schemaMutations)
    {
        return new InternalCatalogSchemaBuilder(this, schemaMutations);
    }

    public string Name => Delegate.Name;

    public string? Description => Delegate.Description;

    public IDictionary<NamingConvention, string?> NameVariants => Delegate.NameVariants;

    public string? GetNameVariant(NamingConvention namingConvention)
    {
        return Delegate.GetNameVariant(namingConvention);
    }

    public int Version => Delegate.Version;

    public bool DiffersFrom(ICatalogSchema? otherObject)
    {
        return Delegate.DiffersFrom(otherObject);
    }

    public IDictionary<string, IGlobalAttributeSchema> GetAttributes()
    {
        return Delegate.GetAttributes();
    }

    public IGlobalAttributeSchema? GetAttribute(string name)
    {
        return Delegate.GetAttribute(name);
    }

    public IGlobalAttributeSchema? GetAttributeByName(string name, NamingConvention namingConvention)
    {
        return Delegate.GetAttributeByName(name, namingConvention);
    }

    public ISet<CatalogEvolutionMode> CatalogEvolutionModes => Delegate.CatalogEvolutionModes;

    public IEntitySchema? GetEntitySchema(string entityType)
    {
        return _entitySchemaAccessor.Invoke(entityType);
    }
    
    public IEntitySchema GetEntitySchemaOrThrowException(string entityType) {
        return GetEntitySchema(entityType) ?? throw new EvitaInvalidUsageException("Schema for entity with name `" + entityType + "` was not found!");
    }
}