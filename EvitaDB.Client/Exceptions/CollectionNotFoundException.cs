namespace EvitaDB.Client.Exceptions;

public class CollectionNotFoundException : EvitaInvalidUsageException
{
    public CollectionNotFoundException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public CollectionNotFoundException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public CollectionNotFoundException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public CollectionNotFoundException(string entityType) : base($"No collection found for entity type {entityType}!")
    {
    }
    
    public CollectionNotFoundException(int entityTypePrimaryKey) : base($"No collection found for entity type with primary key {entityTypePrimaryKey}!")
    {
    }
}