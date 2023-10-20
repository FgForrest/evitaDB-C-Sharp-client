using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data;

public interface IReference : IAttributes<IAttributeSchema>, IDroppable
{
    public ReferenceKey ReferenceKey { get; }
    public GroupEntityReference? Group { get; }
    public ISealedEntity? GroupEntity { get; }
    public ISealedEntity? ReferencedEntity { get; }
    public IReferenceSchema? ReferenceSchema { get; }
    public Cardinality? ReferenceCardinality { get; }
    public string? ReferencedEntityType { get; }
    public string ReferenceName { get; }
    public int ReferencedPrimaryKey { get; }
    
    bool DiffersFrom(IReference? otherReference) {
        if (otherReference == null) return true;
        if (!Equals(ReferenceKey, otherReference.ReferenceKey)) return true;
        if (Group?.DiffersFrom(otherReference.Group) ?? otherReference.Group is not null)
            return true;
        return Dropped != otherReference.Dropped || AnyAttributeDifferBetween(this, otherReference);
    }
}