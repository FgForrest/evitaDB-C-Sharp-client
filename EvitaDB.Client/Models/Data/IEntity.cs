using System.Globalization;
using Client.Models.Schemas;

namespace Client.Models.Data;

public interface IEntity : IEntityClassifierWithParent, IAttributes, IAssociatedData, IPrices, IDroppable,
    IContentComparator<IEntity>
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

    new bool DiffersFrom(IEntity? otherEntity)
    {
        if (this == otherEntity) return false;
        if (otherEntity == null) return true;

        if (!Equals(PrimaryKey, otherEntity.PrimaryKey)) return true;
        if (Version != otherEntity.Version) return true;
        if (Dropped != otherEntity.Dropped) return true;
        if (!Type.Equals(otherEntity.Type)) return true;
        if (ParentAvailable != otherEntity.ParentAvailable) return true;
        if (ParentAvailable)
        {
            if (ParentEntity is not null != otherEntity.ParentEntity is not null) return true;
            if (ParentEntity is not null &&
                !Equals(ParentEntity.PrimaryKey, otherEntity.ParentEntity?.PrimaryKey)) return true;
        }

        if (AnyAttributeDifferBetween(this, otherEntity)) return true;
        if (AnyAssociatedDataDifferBetween(this, otherEntity)) return true;
        if (InnerRecordHandling != otherEntity.InnerRecordHandling) return true;
        if (AnyPriceDifferBetween(this, otherEntity)) return true;
        if (!GetAllLocales().Equals(otherEntity.GetAllLocales())) return true;

        IEnumerable<IReference> thisReferences = GetReferences().ToList();
        IEnumerable<IReference> otherReferences = otherEntity.GetReferences().ToList();
        if (thisReferences.Count() != otherReferences.Count()) return true;
        foreach (IReference thisReference in thisReferences)
        {
            ReferenceKey thisKey = thisReference.ReferenceKey;
            if (otherEntity.GetReference(thisKey.ReferenceName, thisKey.PrimaryKey)?.DiffersFrom(thisReference) ?? true)
            {
                return true;
            }
        }

        return false;
    }
}