using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// <remarks>
/// <para>
/// Interface that simply combines <see cref="IReferenceSchemaEditor"/> and <see cref="IReferenceSchema"/> entity contracts
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
public interface IReferenceSchemaBuilder : IReferenceSchemaEditor<IReferenceSchemaBuilder>
{
    /// <summary>
    /// Returns collection of <see cref="IEntitySchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing parent schema in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// </summary>
    ICollection<IEntitySchemaMutation> ToMutation();
}