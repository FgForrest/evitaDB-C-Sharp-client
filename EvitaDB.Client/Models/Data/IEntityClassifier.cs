namespace Client.Models.Data;

public interface IEntityClassifier
{
    public string Type { get; }
    public int? PrimaryKey { get; }
}