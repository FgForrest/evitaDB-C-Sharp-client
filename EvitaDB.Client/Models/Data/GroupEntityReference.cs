namespace EvitaDB.Client.Models.Data;

public record GroupEntityReference(string ReferencedEntity, int ReferencedEntityPrimaryKey, int Version, bool Dropped = false) : IEntityReference, IDroppable, IContentComparator<GroupEntityReference>
{
    public string Type => ReferencedEntity;
    public int? PrimaryKey => ReferencedEntityPrimaryKey;
    
    public bool DiffersFrom(GroupEntityReference? otherReferenceGroup) {
        if (otherReferenceGroup == null) {
            return true;
        }
        if (!Equals(PrimaryKey, otherReferenceGroup.PrimaryKey)) {
            return true;
        }
        return Dropped != otherReferenceGroup.Dropped;
    }
}