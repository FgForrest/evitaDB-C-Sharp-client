namespace EvitaDB.Client.Queries.Requires;

public interface IEntityFetchRequire : IRequireConstraint, IEntityRequire, ISeparateEntityContentRequireContainer
{
    IEntityContentRequire[] Requirements { get; }
}