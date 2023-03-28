namespace Client.Models.Data;

public record GroupEntityReference(string ReferencedEntity, int ReferencedEntityPrimaryKey, int Version) : IEntityReference
{
    public string EntityType => ReferencedEntity;
    public int? PrimaryKey => ReferencedEntityPrimaryKey;
}