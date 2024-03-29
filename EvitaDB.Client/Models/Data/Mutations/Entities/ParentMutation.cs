﻿using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Entities;

public abstract class ParentMutation : ILocalMutation<int?>
{
    public abstract int? MutateLocal(IEntitySchema entitySchema, int? existingValue);
}