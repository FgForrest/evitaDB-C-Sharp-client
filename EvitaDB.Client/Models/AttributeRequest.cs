namespace EvitaDB.Client.Models;

/// <summary>
/// Attribute request DTO contains information about all attribute names that has been requested for the particular reference.
/// </summary>
/// <param name="AttributeSet">Contains information about all attribute names that has been fetched / requested for the entity.</param>
/// <param name="RequiresEntityAttributes">Contains true if any of the attributes of the entity has been fetched / requested.</param>
public record AttributeRequest(ISet<string> AttributeSet, bool RequiresEntityAttributes)
{
    /// <summary>
    /// Represents a request for no attributes to be fetched.
    /// </summary>
    public static readonly AttributeRequest Empty = new(new HashSet<string>(), false);
    
    /// <summary>
    /// Represents a request for all attributes to be fetched.
    /// </summary>
    public static readonly AttributeRequest Full = new(new HashSet<string>(), true);
}