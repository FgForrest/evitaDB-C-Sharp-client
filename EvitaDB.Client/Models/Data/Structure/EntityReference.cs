namespace EvitaDB.Client.Models.Data.Structure;

public record EntityReference(string Type, int? PrimaryKey) : IEntityReference
{
    public override string ToString()
    {
        return Type + ": " + PrimaryKey;
    }
}
