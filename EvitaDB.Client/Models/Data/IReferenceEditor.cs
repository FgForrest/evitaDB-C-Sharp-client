using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow creating / updating or removing information in <see cref="Reference"/> instance.
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IReference"/> interface.
/// </summary>
public interface IReferenceEditor<out TW> : IReference, IAttributesEditor<TW, IAttributeSchema> where TW : IReferenceEditor<TW>
{
    /// <summary>
    /// Sets group id to the reference. The group type must be already known by the entity schema.
    /// </summary>
    /// <param name="primaryKey">primary key of the group to set</param>
    /// <returns>self (builder pattern)</returns>
    TW SetGroup(int primaryKey);

    /// <summary>
    /// Sets group to the reference. Group is composed of entity type and primary key of the referenced group entity.
    /// Group may or may not be Evita entity. If the group is not known by the entity schema it's automatically set up.
    /// </summary>
    /// <param name="referencedEntity">entity type of the group to be set</param>
    /// <param name="primaryKey">primary key of the group to be set</param>
    /// <returns>self (builder pattern)</returns>
    TW SetGroup(string? referencedEntity, int primaryKey);

    /// <summary>
    /// Removes existing reference group.
    /// </summary>
    /// <returns>self (builder pattern)</returns>
    TW RemoveGroup();
}