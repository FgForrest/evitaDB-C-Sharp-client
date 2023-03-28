namespace Client.Models.Data;

public interface IEntityClassifier
{
    public string EntityType { get; }
    public int? PrimaryKey { get; }
}