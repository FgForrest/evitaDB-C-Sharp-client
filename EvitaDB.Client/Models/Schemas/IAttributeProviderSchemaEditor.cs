using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Generic interface that allows altering schema attributes. Allow to unify altering process of attributes across
/// multiple levels of the schema - catalog, entity and reference which all allow defining attributes.
/// </summary>
public interface IAttributeProviderSchemaEditor<out TS, T, out TU> : IAttributeSchemaProvider<T>
    where TS : IAttributeProviderSchemaEditor<TS, T, TU>
    where T : IAttributeSchema
    where TU : IAttributeSchemaEditor<TU>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributeName"></param>
    /// <typeparam name="TT">type of the entity. Must be one of <see cref="EvitaDataTypes.SupportedTypes"/> types</typeparam>
    /// <returns></returns>
    TS WithAttribute<TT>(string attributeName);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributeName">name of the attribute</param>
    /// <param name="whichIs">lambda that allows to specify attributes of the attribute itself</param>
    /// <typeparam name="TT">type of the entity. Must be one of &lt;see cref="EvitaDataTypes.SupportedTypes"/&gt; types</typeparam>
    /// <typeparam name="TA"></typeparam>
    /// <returns></returns>
    TS WithAttribute<TT, TA>(
        string attributeName,
        Action<TU>? whichIs
    );

    /// <summary>
    /// Removes specific <see cref="IAttributeSchema"/> from the set of allowed attributes of the entity or reference
    /// schema.
    /// </summary>
    /// <param name="attributeName">name of the attribute</param>
    /// <returns></returns>
    TS WithoutAttribute(string attributeName);
}