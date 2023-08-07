﻿using Client.Models.Data.Structure;
using Client.Models.Mutations;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations;

namespace Client.Models.Data.Mutations;

public interface IEntityMutation : IMutation
{
	string EntityType { get; }
	
	int? EntityPrimaryKey { get; set; }
	
	EntityExistence Expects();

	SealedEntity Mutate(EntitySchema entitySchema, SealedEntity? entity);
}