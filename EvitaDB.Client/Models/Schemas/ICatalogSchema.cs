using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;

namespace EvitaDB.Client.Models.Schemas;

public interface ICatalogSchema : INamedSchema, IVersioned, IContentComparator<ICatalogSchema>, IAttributeSchemaProvider<IGlobalAttributeSchema>
{
    ISet<CatalogEvolutionMode> CatalogEvolutionModes { get; }
    IEntitySchema? GetEntitySchema(string entityType);

    IEntitySchema GetEntitySchemaOrThrowException(string entityType)
    {
        return GetEntitySchema(entityType) ??
               throw new EvitaInvalidUsageException("Schema for entity with name `" + entityType + "` was not found!");
    }
}