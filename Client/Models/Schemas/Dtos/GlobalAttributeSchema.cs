﻿using Client.Utils;

namespace Client.Models.Schemas.Dtos;

public class GlobalAttributeSchema : AttributeSchema
{
    public bool UniqueGlobally { get; }
    public new bool Unique => base.Unique || UniqueGlobally;

    public GlobalAttributeSchema(
        string name,
        IDictionary<NamingConvention, string> nameVariants,
        string? description,
        string? deprecationNotice,
        bool unique,
        bool uniqueGlobally,
        bool filterable,
        bool sortable,
        bool localized,
        bool nullable,
        Type type,
        object? defaultValue,
        int indexedDecimalPlaces) : base(name, nameVariants, description, deprecationNotice, unique, filterable,
        sortable, localized, nullable, type, defaultValue, indexedDecimalPlaces)
    {
        UniqueGlobally = uniqueGlobally;
    }
}