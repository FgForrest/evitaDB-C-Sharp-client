using EvitaDB.Client.Queries.Requires;

namespace EvitaDB.Client.Models;

public record RequirementContext(AttributeRequest AttributeRequest, EntityFetch? EntityFetch, EntityGroupFetch? EntityGroupFetch);