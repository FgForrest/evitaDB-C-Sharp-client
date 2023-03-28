using Client.Models.Schemas.Mutations;

namespace Client.Models.Schemas.Builders;

public record MutationReplacement<T>(int Index, T ReplaceMutation) where T : ISchemaMutation;
