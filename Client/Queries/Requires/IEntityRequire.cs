namespace Client.Queries.Requires;

public interface IEntityRequire
{ 
    IEntityContentRequire?[] Requirements { get; }
}