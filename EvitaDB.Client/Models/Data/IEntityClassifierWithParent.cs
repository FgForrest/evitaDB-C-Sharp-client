using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Common ancestor for contracts that either directly represent <see cref="IEntity"/> or reference to it and may
/// contain reference to parent entities. We don't use sealed interface here because there are multiple implementations
/// of those interfaces but only these two aforementioned extending interfaces could extend from this one.
/// </summary>
public interface IEntityClassifierWithParent : IEntityClassifier
{
    /// <summary>
    /// Optional reference to <see cref="Entity.Parent"/> of the referenced entity.
    /// </summary>
    IEntityClassifierWithParent? ParentEntity { get; }
}
