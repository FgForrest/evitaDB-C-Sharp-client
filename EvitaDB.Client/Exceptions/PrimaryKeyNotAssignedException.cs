namespace EvitaDB.Client.Exceptions;

/// <summary>
/// Raised when an entity's primary key is required but has not been assigned yet - i.e. the entity was
/// created on the client and has not been stored, so evitaDB has not generated a key for it.
/// Mirrors Java's `PrimaryKeyNotAssignedException`.
/// </summary>
public class PrimaryKeyNotAssignedException : EvitaInvalidUsageException
{
    public PrimaryKeyNotAssignedException(string entityType) : base(
        $"Primary key for entity `{entityType}` has not been assigned yet. Please store the entity first.")
    {
    }
}
