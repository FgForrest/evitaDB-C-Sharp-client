using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Cdc;

/// <summary>
/// Represents a single system-level (engine) change event - e.g. a catalog being created, renamed or removed.
/// The body carries the engine mutation when full bodies were requested and the mutation type is supported
/// by the client model.
/// </summary>
public record ChangeSystemCapture(long Version, int Index, Operation Operation, IMutation? Body) : IChangeCapture;
