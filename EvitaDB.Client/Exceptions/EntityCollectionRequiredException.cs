namespace EvitaDB.Client.Exceptions;

public class EntityCollectionRequiredException : EvitaInvalidUsageException
{
    public EntityCollectionRequiredException(string publicMessage) 
    : base("Collection type is required in query in order to compute " + publicMessage + "!")
    {
    }
}