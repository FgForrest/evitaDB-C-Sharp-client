namespace Client.Queries.Requires;

public interface IEntityRequire : IRequireConstraint
{ 
    IEntityContentRequire?[] Requirements { get; }
}