using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data.Mutations;

public interface IEntityMutation : IMutation
{
	string EntityType { get; }
	
	int? EntityPrimaryKey { get; set; }
	
	EntityExistence Expects();

	SealedEntity Mutate(EntitySchema entitySchema, SealedEntity? entity);
}