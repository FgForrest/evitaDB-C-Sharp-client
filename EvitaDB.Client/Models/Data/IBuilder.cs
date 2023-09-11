using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Base interface for the entity builders.
/// </summary>
/// <typeparam name="T">type produced by this builder</typeparam>
/// <typeparam name="TM">supported mutation base type</typeparam>
public interface IBuilder<out T, out TM> where TM : IMutation
{
    /// <summary>
    /// Produces enumerable that can be used to create or alter existing instance of the class.
    /// </summary>
    /// <returns>enumerable of mutations</returns>
    IEnumerable<TM> BuildChangeSet();
    /// <summary>
    /// Produces new immutable object with data changed by the builder.
    /// </summary>
    /// <returns>the built object</returns>
    T Build();
}