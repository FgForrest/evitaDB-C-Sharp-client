using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// <remarks>
/// <para>
/// Catalog schema defines the basic properties of the <see cref="ICatalog"/> that is a main container
/// of evitaDB data comparable to the single schema/database in a relational database system. Single catalog usually
/// contains data of single customer and represents a distinguishing unit in multi-tenant system.
/// </para>
/// <para>
/// Catalog is uniquely identified by its <see cref="INamedSchema.Name"/> among all other catalogs in the same evitaDB engine.
/// The instance of the catalog is represented by the <see cref="ICatalog"/>.
/// </para>
/// </remarks>
/// </summary>
public interface ICatalogSchema : INamedSchema, IVersioned, IContentComparator<ICatalogSchema>, IAttributeSchemaProvider<IGlobalAttributeSchema>
{
    /// <summary>
    /// Returns set of allowed evolution modes. These allow to specify how strict is evitaDB when unknown information is
    /// presented to her for the first time. When no evolution mode is set, each violation of the <see cref="IEntitySchema"/> is
    /// reported by an exception. This behaviour can be changed by this evolution mode, however.
    /// </summary>
    ISet<CatalogEvolutionMode> CatalogEvolutionModes { get; }
    
    /// <summary>
    /// Returns entity schema that is connected with passed `entityType` or NULL if such entity collection doesn't
    /// exist.
    /// </summary>
    IEntitySchema? GetEntitySchema(string entityType);

    /// <summary>
    /// Returns entity schema that is connected with passed `entityType` or throws an exception if such entity collection
    /// doesn't exist.
    /// </summary>
    IEntitySchema GetEntitySchemaOrThrowException(string entityType)
    {
        return GetEntitySchema(entityType) ??
               throw new EvitaInvalidUsageException("Schema for entity with name `" + entityType + "` was not found!");
    }
}