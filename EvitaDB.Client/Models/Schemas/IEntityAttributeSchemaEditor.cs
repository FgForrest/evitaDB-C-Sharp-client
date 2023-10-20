namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IEntityAttributeSchema"/> interface.
/// </summary>
public interface IEntityAttributeSchemaEditor<out T> : IAttributeSchemaEditor<T>
where T : IEntityAttributeSchemaEditor<T>
{
    /// <summary>
    /// If an attribute is flagged as representative, it should be used in developer tools along with the entity's
    /// primary key to describe the entity or reference to that entity. The flag is completely optional and doesn't
    /// affect the core functionality of the database in any way. However, if it's used correctly, it can be very
    /// helpful to developers in quickly finding their way around the data. There should be very few representative
    /// attributes in the entity type, and the unique ones are usually the best to choose.
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    T Representative();

    /// <summary>
    /// If an attribute is flagged as representative, it should be used in developer tools along with the entity's
    /// primary key to describe the entity or reference to that entity. The flag is completely optional and doesn't
    /// affect the core functionality of the database in any way. However, if it's used correctly, it can be very
    /// helpful to developers in quickly finding their way around the data. There should be very few representative
    /// attributes in the entity type, and the unique ones are usually the best to choose.
    /// </summary>
    /// <param name="decider">returns true when attribute should be representative</param>
    /// <returns>builder to continue with configuration</returns>
    T Representative(Func<bool> decider);
}