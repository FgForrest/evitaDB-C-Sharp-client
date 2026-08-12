using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class CreateAttributeSchemaMutationConverter : ISchemaMutationConverter<CreateAttributeSchemaMutation, GrpcCreateAttributeSchemaMutation>
{
    public GrpcCreateAttributeSchemaMutation Convert(CreateAttributeSchemaMutation mutation)
    {
        GrpcCreateAttributeSchemaMutation grpcMutation = new GrpcCreateAttributeSchemaMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Unique = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique),
            Filterable = mutation.Filterable,
            Sortable = mutation.Sortable,
#pragma warning restore CS0612
            Localized = mutation.Localized,
            Nullable = mutation.Nullable,
            Representative = mutation.Representative,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(mutation.Type),
            IndexedDecimalPlaces = mutation.IndexedDecimalPlaces,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            DefaultValue = mutation.DefaultValue is not null ? EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.DefaultValue) : null
        };

        if (mutation.Unique != AttributeUniquenessType.NotUnique)
        {
            grpcMutation.UniqueInScopes.Add(new GrpcScopedAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique)
            });
        }

        if (mutation.Filterable)
        {
            grpcMutation.FilterableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        if (mutation.Sortable)
        {
            grpcMutation.SortableInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcMutation;
    }

    public CreateAttributeSchemaMutation Convert(GrpcCreateAttributeSchemaMutation mutation)
    {
        return new CreateAttributeSchemaMutation(
            mutation.Name,
            mutation.Description,
            mutation.DeprecationNotice,
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
            EvitaEnumConverter.ToAttributeUniquenessType(mutation.UniqueInScopes, mutation.Unique),
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.FilterableInScopes, mutation.Filterable),
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.SortableInScopes, mutation.Sortable),
#pragma warning restore CS0612
            mutation.Localized,
            mutation.Nullable,
            mutation.Representative,
            EvitaDataTypesConverter.ToEvitaDataType(mutation.Type),
            EvitaDataTypesConverter.ToEvitaValue(mutation.DefaultValue),
            mutation.IndexedDecimalPlaces
        );
    }
}
