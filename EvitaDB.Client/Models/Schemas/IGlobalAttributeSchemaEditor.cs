namespace EvitaDB.Client.Models.Schemas;

public interface IGlobalAttributeSchemaEditor<out T> : IAttributeSchemaEditor<T>
    where T : IGlobalAttributeSchemaEditor<T>
{
    T UniqueGlobally();
    T UniqueGlobally(Func<bool> decider);
}