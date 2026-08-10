using System.Text.RegularExpressions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface ISchemaMutation : IMutation
{
    IEnumerable<ChangeCatalogCapture> IMutation.ToChangeCatalogCapture(
        MutationPredicate predicate,
        CaptureContent content)
    {
        MutationPredicateContext context = predicate.Context;
        if (predicate.Test(this))
        {
            return
            [
                ChangeCatalogCapture.SchemaCapture(
                context,
                Operation,
                content == CaptureContent.Body ? this : null)
            ];
        }

        return [];
    }
}
