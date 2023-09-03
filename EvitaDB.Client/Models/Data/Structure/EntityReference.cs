namespace EvitaDB.Client.Models.Data.Structure;

public record EntityReference(string Type, int? PrimaryKey) : IEntityReference
{
}