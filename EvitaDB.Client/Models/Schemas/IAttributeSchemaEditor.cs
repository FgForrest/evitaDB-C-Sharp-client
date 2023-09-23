using System.Globalization;

namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IAttributeSchema"/> interface.
/// </summary>
/// <typeparam name="T">attribute schema altering editor type</typeparam>
public interface IAttributeSchemaEditor<out T> : IAttributeSchema, INamedSchemaWithDeprecationEditor<T> 
    where T : INamedSchemaWithDeprecationEditor<T>
{
    /// <summary>
    /// Default value is used when the entity is created without this attribute specified. Default values allow to pass
    /// non-null checks even if no attributes of such name are specified.
    /// </summary>
    /// <param name="defaultValue">value to set</param>
    /// <returns>builder to continue with configuration</returns>
    T WithDefaultValue(object? defaultValue);
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// When attribute is filterable, it is possible to filter entities by this attribute. Do not mark attribute
    /// as filterable unless you know that you'll search entities by this attribute. Each filterable attribute occupies
    /// (memory/disk) space in the form of index.
    /// </para>
    /// <para>
    /// The attribute will be filtered / looked up for by its <see cref="IAttributeSchema.Type"/> type and
    /// <see cref="IComparable"/> contract. If the type is not <see cref="IComparable"/> the <see cref="string.CompareTo(string)"/>
    /// comparison on its <see cref="object.ToString"/> will be used
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new T Filterable();
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// When attribute is filterable, it is possible to filter entities by this attribute. Do not mark attribute
    /// as filterable unless you know that you'll search entities by this attribute. Each filterable attribute occupies
    /// (memory/disk) space in the form of index.
    /// </para>
    /// <para>
    /// The attribute will be filtered / looked up for by its <see cref="IAttributeSchema.Type"/> type and
    /// <see cref="IComparable"/> contract. If the type is not <see cref="IComparable"/> the <see cref="string.CompareTo(string)"/>
    /// comparison on its <see cref="object.ToString"/> will be used
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="decider">returns true when attribute should be filtered</param>
    /// <returns>builder to continue with configuration</returns>
    T Filterable(Func<bool> decider);
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// When attribute is unique it is automatically filterable, and it is ensured there is exactly one single entity
    /// having certain value of this attribute.
    /// </para>
    /// <para>
    /// The attribute will be filtered / looked up for by its <see cref="IAttributeSchema.Type"/> type and
    /// <see cref="IComparable"/> contract. If the type is not <see cref="IComparable"/> the <see cref="string.CompareTo(string)"/>
    /// </para>
    /// <para>
    /// As an example of unique attribute can be EAN - there is no sense in having two entities with same EAN, and it's
    /// better to have this ensured by the database engine.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new T Unique();
    
    /// <summary>
    /// <remarks>
    /// <para>
    /// When attribute is unique it is automatically filterable, and it is ensured there is exactly one single entity
    /// having certain value of this attribute.
    /// </para>
    /// <para>
    /// The attribute will be filtered / looked up for by its <see cref="IAttributeSchema.Type"/> type and
    /// <see cref="IComparable"/> contract. If the type is not <see cref="IComparable"/> the <see cref="string.CompareTo(string)"/>
    /// </para>
    /// <para>
    /// As an example of unique attribute can be EAN - there is no sense in having two entities with same EAN, and it's
    /// better to have this ensured by the database engine.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="decider">returns true when attribute should be filtered</param>
    /// <returns>builder to continue with configuration</returns>
    T Unique(Func<bool> decider);
    
    /// <summary>
    /// When attribute is sortable, it is possible to sort entities by this attribute. Do not mark attribute
    /// as sortable unless you know that you'll sort entities along this attribute. Each sortable attribute occupies
    /// (memory/disk) space in the form of index. <see cref="IAttributeSchema.Type"/> Type of the filterable attribute must
    /// implement {@link Comparable} interface.
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new T Sortable();
    
    /// <summary>
    /// When attribute is sortable, it is possible to sort entities by this attribute. Do not mark attribute
    /// as sortable unless you know that you'll sort entities along this attribute. Each sortable attribute occupies
    /// (memory/disk) space in the form of index. <see cref="IAttributeSchema.Type"/> Type of the filterable attribute must
    /// implement {@link Comparable} interface.
    /// </summary>
    /// <param name="decider">returns true when attribute should be sortable</param>
    /// <returns>builder to continue with configuration</returns>
    T Sortable(Func<bool> decider);
    
    /// <summary>
    /// Localized attribute has to be ALWAYS used in connection with specific <see cref="CultureInfo"/>. In other
    /// words - it cannot be stored unless associated locale is also provided.
    /// </summary>
    /// <returns>builder to continue with configuration</returns>
    new T Localized();
    
    /// <summary>
    /// Localized attribute has to be ALWAYS used in connection with specific <see cref="CultureInfo"/>. In other
    /// words - it cannot be stored unless associated locale is also provided.
    /// </summary>
    /// <param name="decider">returns true when attribute should be localized</param>
    /// <returns>builder to continue with configuration</returns>
    T Localized(Func<bool> decider);
    
    /// <summary>
    /// When attribute is nullable, its values may be missing in the entities. Otherwise, the system will enforce
    /// non-null checks upon upserting of the entity.
    /// </summary>
    /// <returns></returns>
    new T Nullable();
    
    /// <summary>
    /// When attribute is nullable, its values may be missing in the entities. Otherwise, the system will enforce
    /// non-null checks upon upserting of the entity.
    /// </summary>
    /// <param name="decider">returns true when attribute should be nullable</param>
    /// <returns>builder to continue with configuration</returns>
    T Nullable(Func<bool> decider);
    
    /// <summary>
    /// Determines how many fractional places are important when entities are compared during filtering or sorting. It is
    /// essential to know that all values of this attribute will be converted to <see cref="int"/>, so the attribute
    /// number must not ever exceed maximum limits of this type when scaling the number by the power
    /// of ten using `indexDecimalPlaces` as exponent.
    /// </summary>
    /// <param name="indexedDecimalPlaces">how many decimal places should be indexed in database</param>
    /// <returns>builder to continue with configuration</returns>
    T IndexDecimalPlaces(int indexedDecimalPlaces);
}