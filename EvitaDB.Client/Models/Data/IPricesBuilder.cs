using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Structure;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Interface that simply combines writer and builder contracts together.
/// </summary>
public interface IPricesBuilder : IPricesEditor<IPricesBuilder>, IBuilder<Prices, ILocalMutation>
{
}