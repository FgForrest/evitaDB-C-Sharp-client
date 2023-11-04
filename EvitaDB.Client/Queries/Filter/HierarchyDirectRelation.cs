namespace EvitaDB.Client.Queries.Filter;

public class HierarchyDirectRelation : AbstractFilterConstraintLeaf, IHierarchySpecificationFilterConstraint
{
    private const string ConstraintName = "directRelation";
    
    private HierarchyDirectRelation(params object?[] arguments) : base(ConstraintName, arguments)
    {
    }
    
    public HierarchyDirectRelation() : base(ConstraintName)
    {
    }

    public new bool Applicable => true;
}