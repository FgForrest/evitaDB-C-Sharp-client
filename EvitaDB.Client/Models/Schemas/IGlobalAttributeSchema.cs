namespace EvitaDB.Client.Models.Schemas;

public interface IGlobalAttributeSchema : IAttributeSchema
{
    bool UniqueGlobally { get; }
}