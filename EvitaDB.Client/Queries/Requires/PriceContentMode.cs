namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// Determines which prices will be fetched along with entity.
/// </summary>
public enum PriceContentMode
{
    None,
    RespectingFilter,
    All
}
