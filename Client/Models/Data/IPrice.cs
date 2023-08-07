using Client.DataTypes;

namespace Client.Models.Data;

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
}