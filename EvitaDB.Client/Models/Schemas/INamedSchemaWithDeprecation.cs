﻿namespace EvitaDB.Client.Models.Schemas;

public interface INamedSchemaWithDeprecation : INamedSchema
{
    string? DeprecationNotice { get; }
}