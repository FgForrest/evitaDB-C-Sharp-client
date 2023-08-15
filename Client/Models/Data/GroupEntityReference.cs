namespace Client.Models.Data;

public record GroupEntityReference(string ReferencedEntity, int ReferencedEntityPrimaryKey, int Version, bool Dropped = false) : IEntityReference, IDroppable
{
    public string Type => ReferencedEntity;
    public int? PrimaryKey => ReferencedEntityPrimaryKey;
}