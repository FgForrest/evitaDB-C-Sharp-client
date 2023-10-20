using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class CreateAttributeSchemaMutationConverter : ISchemaMutationConverter<CreateAttributeSchemaMutation, GrpcCreateAttributeSchemaMutation>
{
    public GrpcCreateAttributeSchemaMutation Convert(CreateAttributeSchemaMutation mutation)
    {
        return new GrpcCreateAttributeSchemaMutation
        {
            Name = mutation.Name,
            Unique = mutation.Unique,
            Filterable = mutation.Filterable,
            Sortable = mutation.Sortable,
            Localized = mutation.Localized,
            Nullable = mutation.Nullable,
            Representative = mutation.Representative,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(mutation.Type),
            IndexedDecimalPlaces = mutation.IndexedDecimalPlaces,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            DefaultValue = mutation.DefaultValue is not null ? EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.DefaultValue) : null
        };
    }

    public CreateAttributeSchemaMutation Convert(GrpcCreateAttributeSchemaMutation mutation)
    {
        return new CreateAttributeSchemaMutation(
            mutation.Name,
            mutation.Description,
            mutation.DeprecationNotice,
            mutation.Unique,
            mutation.Filterable,
            mutation.Sortable,
            mutation.Localized,
            mutation.Nullable,
            mutation.Representative,
            EvitaDataTypesConverter.ToEvitaDataType(mutation.Type),
            EvitaDataTypesConverter.ToEvitaValue(mutation.DefaultValue),
            mutation.IndexedDecimalPlaces
        );
    }
}