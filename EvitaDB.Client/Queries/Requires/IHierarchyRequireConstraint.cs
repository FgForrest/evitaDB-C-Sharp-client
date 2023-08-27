namespace Client.Queries.Requires;

public interface IHierarchyRequireConstraint : IRequireConstraint, IExtraResultRequireConstraint
{
    string? OutputName { get; }
}