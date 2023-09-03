using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Models.Data;

public interface IPrice : IDroppable
{
    PriceKey Key { get; }
    int? InnerRecordId { get; }
    decimal PriceWithoutTax { get; }
    decimal TaxRate { get; }
    decimal PriceWithTax { get; }
    DateTimeRange? Validity { get; }
    bool Sellable { get; }
    Currency Currency { get; }
    string PriceList { get; }
    int PriceId { get; }
    
    bool DiffersFrom(IPrice? otherPrice) {
        if (otherPrice == null) return true;
        if (!Equals(InnerRecordId, otherPrice.InnerRecordId)) return true;
        if (!Equals(PriceWithoutTax, otherPrice.PriceWithoutTax)) return true;
        if (!Equals(PriceWithTax, otherPrice.PriceWithTax)) return true;
        if (!Equals(TaxRate, otherPrice.TaxRate)) return true;
        if (!Equals(Validity, otherPrice.Validity)) return true;
        if (Sellable != otherPrice.Sellable) return true;
        return Dropped != otherPrice.Dropped;
    }
}