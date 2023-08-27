namespace Client.Queries.Filter;

public interface IHierarchyFilterConstraint : IFilterConstraint
{
    string? ReferenceName { get; }
    bool DirectRelation { get; }
    IFilterConstraint[] HavingChildrenFilter { get; }
    IFilterConstraint[] ExcludeChildrenFilter { get; }
    
}