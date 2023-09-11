using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow creating / updating or removing information in <see cref="Attributes"/> instance.
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IAttributes"/> interface.
/// </summary>
/// <typeparam name="TW">attribute altering editor type</typeparam>
public interface IAttributesEditor<out TW> : IAttributes where TW : IAttributesEditor<TW>
{
	/// <summary>
	/// Removes value associated with the key or null when the attribute is missing.
	/// </summary>
	/// <param name="attributeName">name of attribute to remove</param>
	/// <returns>self (builder pattern)</returns>
    TW RemoveAttribute(string attributeName);

	/// <summary>
	/// Stores value associated with the key.
	/// Setting null value effectively removes the attribute as if the <see cref="RemoveAttribute(string)"/> was called.
	/// </summary>
	/// <param name="attributeName">name of the attribute</param>
	/// <param name="attributeValue">value of the attribute</param>
	/// <returns>self (builder pattern)</returns>
    TW SetAttribute(string attributeName, object? attributeValue);

	/// <summary>
	/// Stores array of values associated with the key.
	/// Setting null value effectively removes the attribute as if the <see cref="RemoveAttribute(string)"/> was called.
	/// </summary>
	/// <param name="attributeName">name of the attribute</param>
	/// <param name="attributeValue">value of the attribute</param>
	/// <returns>self (builder pattern)</returns>
    TW SetAttribute(string attributeName, object[]? attributeValue);

    /// <summary>
    /// Removes locale specific value associated with the key or null when the attribute is missing.
    /// </summary>
    /// <param name="attributeName">name of the attribute</param>
    /// <param name="locale">locale of the attribute</param>
    /// <returns>self (builder pattern)</returns>
    TW RemoveAttribute(string attributeName, CultureInfo locale);
    
    /// <summary>
    /// Stores locale specific value associated with the key.
    /// Setting null value effectively removes the attribute as if the <see cref="RemoveAttribute(string)"/> was called.
    /// </summary>
    /// <param name="attributeName">name of the attribute</param>
    /// <param name="locale">locale of the attribute</param>
    /// <param name="attributeValue">value of the attribute</param>
    /// <returns>self (builder pattern)</returns>
    TW SetAttribute(string attributeName, CultureInfo locale, object? attributeValue);
    
    /// <summary>
    /// Stores array of locale specific value associated with the key.
    /// Setting null value effectively removes the attribute as if the <see cref="RemoveAttribute(string)"/> was called.
    /// </summary>
    /// <param name="attributeName">name of the attribute</param>
    /// <param name="locale">locale of the attribute</param>
    /// <param name="attributeValue">value of the attribute</param>
    /// <returns>self (builder pattern)</returns>
    TW SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue);

    /// <summary>
    /// <remarks>
    ///	<para>
    /// Alters attribute value in a way defined by the passed mutation implementation.
    /// There may never me multiple mutations for the same attribute - if you need to compose mutations you must wrap
    /// them into single one, that is then handed to the builder.
    /// </para>
    /// <para>
    /// Remember each setAttribute produces a mutation itself - so you cannot set attribute and mutate it in the same
    /// round. The latter operation would overwrite the previously registered mutation.
    /// </para>
    /// </remarks>
    /// </summary>
    /// <param name="mutation">mutation to be applied</param>
    /// <returns>self (builder pattern)</returns>
    TW MutateAttribute(AttributeMutation mutation);
}