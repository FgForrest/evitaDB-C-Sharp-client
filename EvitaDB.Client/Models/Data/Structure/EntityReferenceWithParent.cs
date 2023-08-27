namespace Client.Models.Data.Structure;

public record EntityReferenceWithParent(string Type, int? PrimaryKey, IEntityClassifierWithParent? ParentEntity) : IEntityReference, IEntityClassifierWithParent
{
}