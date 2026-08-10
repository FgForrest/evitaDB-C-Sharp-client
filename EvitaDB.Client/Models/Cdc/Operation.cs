namespace EvitaDB.Client.Models.Cdc;

public enum Operation
{
    /**
     * Create or update operation - i.e. there was data with such identity before, and it was updated.
     */
    Upsert,
    /**
     * Remove operation - i.e. there was data with such identity before, and it was removed.
     */
    Remove,
    /**
     * Delimiting operation signaling the beginning of a transaction.
     */
    Transaction
}
