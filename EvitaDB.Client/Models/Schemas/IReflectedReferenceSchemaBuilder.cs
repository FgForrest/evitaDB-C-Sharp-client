namespace EvitaDB.Client.Models.Schemas;

public interface IReflectedReferenceSchemaBuilder : IReflectedReferenceSchemaEditor<IReflectedReferenceSchemaBuilder>
{
    /// <summary>
    /// Returns collection of <see cref="EntitySchemaMutation"/> instances describing what changes occurred in the builder
    /// and which should be applied on the existing parent schema in particular version.
    /// Each mutation increases <see cref="IVersioned.Version"/> of the modified object and allows to detect race
    /// conditions based on "optimistic locking" mechanism in very granular way.
    /// </summary>
    /// <returns></returns>
    ICollection<ILocalEntitySchemaMutation> ToMutation();
}
