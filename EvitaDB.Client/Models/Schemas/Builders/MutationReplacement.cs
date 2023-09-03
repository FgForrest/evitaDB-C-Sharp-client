using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas.Builders;

public record MutationReplacement<T>(int Index, T ReplaceMutation) where T : ISchemaMutation;
