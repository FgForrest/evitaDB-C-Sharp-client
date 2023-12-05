using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow reading information about <see cref="Entity"/> instance.
/// </summary>
public interface IEntity : IEntityClassifierWithParent, IAttributes<IEntityAttributeSchema>, IAssociatedData, IPrices, IDroppable,
    IContentComparator<IEntity>
{
    /// <summary>
    /// Returns schema of the entity, that fully describes its structure and capabilities. Schema is up-to-date to the
    /// moment entity was fetched from evitaDB.
    /// </summary>
    IEntitySchema Schema { get; }
    /// <summary>
    /// Returns primary key of the entity that is UNIQUE among all other entities of the same type.
    /// Primary key may be null only when entity is created in case evitaDB is responsible for automatically assigning
    /// new primary key. Once entity is stored into evitaDB it MUST have non-null primary key. So the NULL can be
    /// returned only in the rare case when new entity is created in the client code and hasn't yet been stored to
    /// evitaDB.
    /// </summary>
    int? Parent { get; }
    /// <summary>
    /// Returns collection of <see cref="Reference"/> of this entity. The references represent relations to other evitaDB
    /// entities or external entities in different systems.
    /// </summary>
    IEnumerable<IReference> GetReferences();
    /// <summary>
    /// Returns collection of <see cref="Reference"/> to certain type of other entities. References represent relations to
    /// other evitaDB entities or external entities in different systems.
    /// </summary>
    IEnumerable<IReference> GetReferences(string referenceName);
    /// <summary>
    /// Returns single <see cref="Reference"/> instance that is referencing passed entity type with certain primary key.
    /// The references represent relations to other evitaDB entities or external entities in different systems.
    /// </summary>
    IReference? GetReference(string referenceName, int referencedEntityId);
    /// <summary>
    /// Returns set of locales this entity has any of localized data in. Although <see cref="IEntitySchema.Locales"/> may
    /// support wider range of the locales, this method returns only those that are used by data of this very entity
    /// instance.
    /// </summary>
    ISet<CultureInfo> GetAllLocales();
    /// <summary>
    /// Returns true if entity hierarchy was fetched along with the entity. Calling this method before calling any
    /// other method that requires prices to be fetched will allow you to avoid <see cref="ContextMissingException"/>.
    /// 
    /// Method also returns false if the entity is not allowed to be hierarchical by the schema. Checking this method
    /// also allows you to avoid <see cref="EntityIsNotHierarchicalException"/> in such case.
    /// </summary>
    bool ParentAvailable();
    /// <summary>
    /// Returns true if entity references were fetched along with the entity. Calling this method before calling any
    /// other method that requires references to be fetched will allow you to avoid <see cref="ContextMissingException"/>.
    /// </summary>
    bool ReferencesAvailable();
    /// <summary>
    /// Method returns true if any entity inner data differs from other entity.
    /// </summary>
    new bool DiffersFrom(IEntity? otherEntity)
    {
        if (Equals(this, otherEntity)) return false;
        if (otherEntity == null) return true;

        if (!Equals(PrimaryKey, otherEntity.PrimaryKey)) return true;
        if (Version != otherEntity.Version) return true;
        if (Dropped != otherEntity.Dropped) return true;
        if (!Type.Equals(otherEntity.Type)) return true;
        if (ParentAvailable != otherEntity.ParentAvailable) return true;
        if (ParentAvailable())
        {
            if (ParentEntity is not null != otherEntity.ParentEntity is not null) return true;
            if (ParentEntity is not null &&
                !Equals(ParentEntity.PrimaryKey, otherEntity.ParentEntity?.PrimaryKey)) return true;
        }

        if (AnyAttributeDifferBetween(this, otherEntity)) return true;
        if (AnyAssociatedDataDifferBetween(this, otherEntity)) return true;
        if (InnerRecordHandling != otherEntity.InnerRecordHandling) return true;
        if (AnyPriceDifferBetween(this, otherEntity)) return true;
        if (!GetAllLocales().Equals(otherEntity.GetAllLocales())) return true;

        IEnumerable<IReference> thisReferences = GetReferences().ToList();
        IEnumerable<IReference> otherReferences = otherEntity.GetReferences().ToList();
        if (thisReferences.Count() != otherReferences.Count()) return true;
        foreach (IReference thisReference in thisReferences)
        {
            ReferenceKey thisKey = thisReference.ReferenceKey;
            if (otherEntity.GetReference(thisKey.ReferenceName, thisKey.PrimaryKey)?.DiffersFrom(thisReference) ?? true)
            {
                return true;
            }
        }

        return false;
    }
}
