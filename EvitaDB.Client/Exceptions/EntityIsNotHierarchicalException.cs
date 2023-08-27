namespace Client.Exceptions;

public class EntityIsNotHierarchicalException : EvitaInvalidUsageException
{
    public string? ReferenceName { get; }
    public string EntityType { get; }
    
    public EntityIsNotHierarchicalException(string entityType) : base("Entity `" + entityType + "` is not hierarchical!") {
        ReferenceName = null;
        EntityType = entityType;
    }

    public EntityIsNotHierarchicalException(string? referenceName, string entityType) : base(referenceName == null ?
        "Entity `" + entityType + "` targeted by query within hierarchy is not hierarchical!" :
        "Entity `" + entityType + "` targeted by query within hierarchy through reference `" + referenceName + "` is not hierarchical!") {
        ReferenceName = referenceName;
        EntityType = entityType;
    }
}