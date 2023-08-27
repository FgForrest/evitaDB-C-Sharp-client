using Client.Models.Data.Structure;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IReference : IAttributes, IDroppable
{
    public ReferenceKey ReferenceKey { get; }
    public GroupEntityReference? Group { get; }
    public SealedEntity? GroupEntity { get; }
    public SealedEntity? ReferencedEntity { get; }
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