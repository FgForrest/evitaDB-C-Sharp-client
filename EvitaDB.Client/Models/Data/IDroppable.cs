﻿namespace EvitaDB.Client.Models.Data;

public interface IDroppable : IVersioned
{
    bool Dropped { get; }
}