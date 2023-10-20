namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface extends the base <see cref="IAttributeSchema"/> with the ability to mark the attribute as representative.
/// This attribute contract can be used only at <see cref="IEntitySchema"/> level.
/// </summary>
public interface IEntityAttributeSchema : IAttributeSchema
{
    /// <summary>
    /// If an attribute is flagged as representative, it should be used in developer tools along with the entity's
    /// primary key to describe the entity or reference to that entity. The flag is completely optional and doesn't
    /// affect the core functionality of the database in any way. However, if it's used correctly, it can be very
    /// helpful to developers in quickly finding their way around the data. There should be very few representative
    /// attributes in the entity type, and the unique ones are usually the best to choose.
    /// </summary>
    bool Representative { get; }
}