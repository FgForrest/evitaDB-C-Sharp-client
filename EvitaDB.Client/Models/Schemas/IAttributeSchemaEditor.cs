namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IAttributeSchema"/> interface.
/// </summary>
/// <typeparam name="T">attribute schema altering editor type</typeparam>
public interface IAttributeSchemaEditor<out T> : IAttributeSchema, INamedSchemaWithDeprecationEditor<T> 
    where T : INamedSchemaWithDeprecationEditor<T>
{
    T WithDefaultValue(object? defaultValue);
    new T Filterable();
    T Filterable(Func<bool> decider);
    new T Unique();
    T Unique(Func<bool> decider);
    new T Sortable();
    T Sortable(Func<bool> decider);
    new T Localized();
    T Localized(Func<bool> decider);
    new T Nullable();
    T Nullable(Func<bool> decider);
    T IndexDecimalPlaces(int indexedDecimalPlaces);
}