namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface provides methods to (re)define schema description.
/// </summary>
/// <typeparam name="TS">named schema altering editor type</typeparam>
public interface INamedSchemaEditor<out TS> : INamedSchema where TS : INamedSchemaEditor<TS>
{
    /// <summary>
    /// Schema is best described by the passed string. Description is expected to be written using
    /// <a href="https://www.markdownguide.org/basic-syntax/">MarkDown syntax</a>. The description should be targeted
    /// on client API developers or users of your data store to facilitate their orientation.
    /// </summary>
    /// <param name="description">new description</param>
    /// <returns>builder to continue with configuration</returns>
    TS WithDescription(string? description);
}