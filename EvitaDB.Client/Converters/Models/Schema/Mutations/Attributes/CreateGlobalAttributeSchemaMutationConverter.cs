﻿using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class CreateGlobalAttributeSchemaMutationConverter : ISchemaMutationConverter<CreateGlobalAttributeSchemaMutation
    , GrpcCreateGlobalAttributeSchemaMutation>
{
    public GrpcCreateGlobalAttributeSchemaMutation Convert(CreateGlobalAttributeSchemaMutation mutation)
    {
        return new GrpcCreateGlobalAttributeSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            Unique = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique),
            UniqueGlobally = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally),
            Filterable = mutation.Filterable,
            Sortable = mutation.Sortable,
            Localized = mutation.Localized,
            Nullable = mutation.Nullable,
            Representative = mutation.Representative,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(mutation.Type),
            DefaultValue = mutation.DefaultValue is not null
                ? EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.DefaultValue)
                : null,
            IndexedDecimalPlaces = mutation.IndexedDecimalPlaces
        };
    }

    public CreateGlobalAttributeSchemaMutation Convert(GrpcCreateGlobalAttributeSchemaMutation mutation)
    {
        return new CreateGlobalAttributeSchemaMutation(mutation.Name, mutation.Description, mutation.DeprecationNotice,
            EvitaEnumConverter.ToAttributeUniquenessType(mutation.Unique),
            EvitaEnumConverter.ToGlobalAttributeUniquenessType(mutation.UniqueGlobally), mutation.Filterable,
            mutation.Sortable, mutation.Localized,
            mutation.Nullable, mutation.Representative, EvitaDataTypesConverter.ToEvitaDataType(mutation.Type),
            mutation.DefaultValue is not null ? EvitaDataTypesConverter.ToEvitaValue(mutation.DefaultValue) : null,
            mutation.IndexedDecimalPlaces);
    }
}
