namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="ICatalogSchema"/> interface.
/// </summary>
public interface ICatalogSchemaEditor<out TS> : ICatalogSchema, INamedSchemaEditor<TS>,
    IAttributeProviderSchemaEditor<TS, IGlobalAttributeSchema, IGlobalAttributeSchemaBuilder> where TS : ICatalogSchemaEditor<TS>
{
}