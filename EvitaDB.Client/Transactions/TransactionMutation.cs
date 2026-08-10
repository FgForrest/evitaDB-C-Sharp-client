using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Transactions;

public class TransactionMutation : IMutation
{
    public Guid TransactionId { get; }
    public long CatalogVersion { get; }
    public int MutationCount { get; }
    public long WalSizeInBytes { get; }
    public DateTimeOffset CommitTimestamp { get; }
    
    public TransactionMutation(
        Guid transactionId, 
        long catalogVersion, 
        int mutationCount, 
        long walSizeInBytes, 
        DateTimeOffset commitTimestamp)
    {
        TransactionId = transactionId;
        CatalogVersion = catalogVersion;
        MutationCount = mutationCount;
        WalSizeInBytes = walSizeInBytes;
        CommitTimestamp = commitTimestamp;
    }
    
    public Operation Operation => Operation.Transaction;
    
    public IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content)
    {
        if (predicate.Test(this))
        {
            MutationPredicateContext context = predicate.Context;
            context.SetVersion(this.CatalogVersion, this.MutationCount);
            
            return [ChangeCatalogCapture.InfrastructureCapture(context, Operation, content == CaptureContent.Body ? this : null)];
        }

        return [];
    }
}
