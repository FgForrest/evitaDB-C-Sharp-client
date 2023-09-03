namespace EvitaDB.Client.Queries.Filter;

public class HierarchyExcludingRoot : AbstractFilterConstraintLeaf, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "excludingRoot";
    
    private HierarchyExcludingRoot(params object[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyExcludingRoot() : base(ConstraintName)
    {
    }
    
    public new bool Applicable => true;
}