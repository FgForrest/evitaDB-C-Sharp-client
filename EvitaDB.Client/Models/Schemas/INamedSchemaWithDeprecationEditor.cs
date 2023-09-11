namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface provides methods to make schema deprecated or revert such deprecation.
/// </summary>
/// <typeparam name="TS"></typeparam>
public interface INamedSchemaWithDeprecationEditor<out TS> : INamedSchemaEditor<TS> where TS : INamedSchemaWithDeprecationEditor<TS>
{
    TS Deprecated(string deprecationNotice);
    TS NotDeprecatedAnymore();
}