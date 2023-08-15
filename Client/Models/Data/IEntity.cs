using System.Globalization;
using Client.Models.Schemas;

namespace Client.Models.Data;

public interface IEntity : IEntityClassifierWithParent, IAttributes, IAssociatedData, IPrices, IDroppable
{
    IEntitySchema Schema { get; }
    int? Parent { get; }
    bool ParentAvailable { get; }
    bool ReferencesAvailable { get; }
    IEnumerable<IReference> GetReferences();
    IPrice? PriceForSale { get; }
    IReference? GetReference(string referenceName, int referencedEntityId);
    IReference? GetReference(ReferenceKey referenceKey);
    ISet<CultureInfo> GetAllLocales();
}