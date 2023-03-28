namespace Client.Models.Data.Structure;

public record EntityReference(string EntityType, int? PrimaryKey) : IEntityReference
{
}