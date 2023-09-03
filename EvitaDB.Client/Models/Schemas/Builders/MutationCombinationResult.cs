using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas.Builders;

public record MutationCombinationResult<T>(T? Origin, params T?[]? Current) where T : ISchemaMutation;
