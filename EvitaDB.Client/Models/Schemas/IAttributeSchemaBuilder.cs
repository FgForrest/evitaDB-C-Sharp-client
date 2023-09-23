using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// <remarks>
///	<para>
/// Interface that simply combines <see cref="IAttributeSchemaEditor"/> and <see cref="IAttributeSchema"/> entity contracts
/// together. Builder produces either <see cref="IEntitySchemaMutation"/> that describes all changes to be made on
/// <see cref="IEntitySchema"/> instance to get it to "up-to-date" state or can provide already built
/// <see cref="IEntitySchema"/> that may not represent globally "up-to-date" state because it is based on
/// the version of the entity known when builder was created.
/// </para>
/// <para>
/// Mutation allows Evita to perform surgical updates on the latest version of the <see cref="IEntitySchema"/>
/// object that is in the database at the time update request arrives.
/// </para>
/// </remarks>
/// </summary>
public interface IAttributeSchemaBuilder : IAttributeSchemaEditor<IAttributeSchemaBuilder>
{
    /// <summary>
    /// Returns collection of <see cref="IEntitySchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing parent schema in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// 
    /// All mutations need and will also to implement <see cref="IAttributeSchemaMutation"/> and can be retrieved by calling
    /// <see cref="ToAttributeMutation()"/> identically.
    /// </summary>
    ICollection<IEntitySchemaMutation> ToMutation();
    
    /// <summary>
    /// Returns collection of <see cref="IAttributeSchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing parent schema in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    ///
    /// All mutations need and will also to implement <see cref="IEntitySchemaMutation"/> and can be retrieved by calling
    /// <see cref="ToMutation()"/> identically.
    /// </summary>
    ICollection<IAttributeSchemaMutation> ToAttributeMutation();

    /// <summary>
    /// Returns collection of <see cref="IReferenceSchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing <see cref="IReferenceSchema"/> in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// All mutations need and will also to implement <see cref="IAttributeSchemaMutation"/> and can be retrieved by calling
    /// <see cref="ToAttributeMutation()"/> identically.
    /// </summary>
    ICollection<IReferenceSchemaMutation> ToReferenceMutation(string referenceName);

    /// <summary>
    /// Returns built "local up-to-date" {@link AttributeSchemaContract} instance that may not represent globally
    /// "up-to-date" state because it is based on the version of the entity known when builder was created.
    /// This method is particularly useful for tests.
    /// </summary>
    IAttributeSchema ToInstance();
}