namespace EvitaDB.Client.Models.Schemas;

public interface IGlobalAttributeSchema : IEntityAttributeSchema
{
    bool UniqueGlobally { get; }
}