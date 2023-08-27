using Client.Models.Mutations;

namespace Client.Models.Cdc;

public record ChangeSystemCapture(long Index, string Catalog, Operation Operation, IMutation? Body) : IChangeCapture;