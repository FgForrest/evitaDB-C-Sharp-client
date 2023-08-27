using Client.Models.Schemas.Mutations;

namespace Client.Models.Schemas.Builders;

public record MutationCombinationResult<T>(T? Origin, params T?[]? Current) where T : ISchemaMutation;
