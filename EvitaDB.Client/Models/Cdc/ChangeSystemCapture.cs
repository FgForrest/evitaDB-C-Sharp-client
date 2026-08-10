using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Cdc;

public record ChangeSystemCapture(long Version, int Index, string Catalog, Operation Operation, IMutation? Body) : IChangeCapture;
