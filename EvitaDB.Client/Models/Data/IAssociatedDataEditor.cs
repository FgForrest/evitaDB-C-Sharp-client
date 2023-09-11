using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Contract for classes that allow creating / updating or removing information in <see cref="AssociatedData"/> instance.
/// Interface follows the <a href="https://en.wikipedia.org/wiki/Builder_pattern">builder pattern</a> allowing to alter
/// the data that are available on the read-only <see cref="IAssociatedData"/> interface.
/// </summary>
/// <typeparam name="TW">associated data altering editor type</typeparam>
public interface IAssociatedDataEditor<out TW> : IAssociatedData where TW : IAssociatedDataEditor<TW>
{
	/// <summary>
	/// Removes value associated with the key or null when the associatedData is missing.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <returns>self (builder pattern)</returns>
	TW RemoveAssociatedData(string associatedDataName);
	
	/// <summary>
	/// Stores value associated with the key.
	/// Setting null value effectively removes the associated data as if the <see cref="RemoveAssociatedData(string)"/> was called.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <param name="associatedDataValue">value of the associated data</param>
	/// <returns>self (builder pattern)</returns>
	TW SetAssociatedData(string associatedDataName, object? associatedDataValue);
	
	/// <summary>
	/// Stores array of values associated with the key.
	/// Setting null value effectively removes the associated data as if the <see cref="RemoveAssociatedData(string)"/> was called.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <param name="associatedDataValue">value of the associated data</param>
	/// <typeparam name="T">type of the value to be stored</typeparam>
	/// <returns>self (builder pattern)</returns>
	TW SetAssociatedData(string associatedDataName, object[]? associatedDataValue);
	
	/// <summary>
	/// Removes locale specific value associated with the key or null when the associatedData is missing.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <param name="locale">locale of the associated data</param>
	/// <returns>self (builder pattern)</returns>
	TW RemoveAssociatedData(string associatedDataName, CultureInfo locale);
	
	/// <summary>
	/// Stores locale specific value associated with the key.
	/// Setting null value effectively removes the associated data as if the <see cref="RemoveAssociatedData(string,CultureInfo)"/> was called.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <param name="locale">locale of the associated data</param>
	/// <param name="associatedDataValue">value of the associated data</param>
	/// <returns>self (builder pattern)</returns>
	TW SetAssociatedData(string associatedDataName, CultureInfo locale, object? associatedDataValue);
	
	/// <summary>
	/// Stores array of locale specific values associated with the key.
	/// Setting null value effectively removes the associated data as if the <see cref="RemoveAssociatedData(string,CultureInfo)"/> was called.
	/// </summary>
	/// <param name="associatedDataName">name of the associated data</param>
	/// <param name="locale">locale of the associated data</param>
	/// <param name="associatedDataValue">value of the associated data</param>
	/// <returns>self (builder pattern)</returns>
	TW SetAssociatedData(string associatedDataName, CultureInfo locale, object[]? associatedDataValue);
	
	/// <summary>
	/// <remarks>
	/// <para>
	/// Alters associatedData value in a way defined by the passed mutation implementation.
	/// There may never me multiple mutations for the same associatedData - if you need to compose mutations you must wrap
	/// them into single one, that is then handed to the builder.
	/// </para>
	/// <para>
	/// Remember each setAssociatedData produces a mutation itself - so you cannot set associatedData and mutate it in the same
	/// round. The latter operation would overwrite the previously registered mutation.
	/// </para>
	/// </remarks>
	/// </summary>
	/// <param name="mutation">mutation to be applied</param>
	/// <returns>self (builder pattern)</returns>
	TW MutateAssociatedData(AssociatedDataMutation mutation);
}