using EvitaDB.Client.Models.Cdc;

namespace EvitaDB.Client.Models.Mutations;

public interface IMutation
{
    Operation Operation { get; }
    IEnumerable<ChangeCatalogCapture> ToChangeCatalogCapture(MutationPredicate predicate, CaptureContent content);

    enum StreamDirection
    {
        Forward,
        Reverse
    }
}
