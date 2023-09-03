namespace EvitaDB.Client.Models.Data.Structure;

public class HierarchicalPlacement
{
    public int Version { get; }
    public int? ParentPrimaryKey { get; }
    public int OrderAmongSiblings { get; }

    public HierarchicalPlacement(int orderAmongSiblings)
    {
        Version = 1;
        ParentPrimaryKey = null;
        OrderAmongSiblings = orderAmongSiblings;
    }
    
    public HierarchicalPlacement(int parentPrimaryKey, int orderAmongSiblings)
    {
        Version = 1;
        ParentPrimaryKey = parentPrimaryKey;
        OrderAmongSiblings = orderAmongSiblings;
    }
    
    public HierarchicalPlacement(int version, int? parentPrimaryKey, int orderAmongSiblings)
    {
        Version = version;
        ParentPrimaryKey = parentPrimaryKey;
        OrderAmongSiblings = orderAmongSiblings;
    }
}