using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class CreateGlobalAttributeSchemaMutationConverter : ISchemaMutationConverter<CreateGlobalAttributeSchemaMutation
    , GrpcCreateGlobalAttributeSchemaMutation>
{
    public GrpcCreateGlobalAttributeSchemaMutation Convert(CreateGlobalAttributeSchemaMutation mutation)
    {
        GrpcCreateGlobalAttributeSchemaMutation grpcMutation = new GrpcCreateGlobalAttributeSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Unique = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique),
            UniqueGlobally = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally),
            Filterable = mutation.Filterable,
            Sortable = mutation.Sortable,
#pragma warning restore CS0612
            Localized = mutation.Localized,
            Nullable = mutation.Nullable,
            Representative = mutation.Representative,
            Type = EvitaDataTypesConverter.ToGrpcEvitaDataType(mutation.Type),
            DefaultValue = mutation.DefaultValue is not null
                ? EvitaDataTypesConverter.ToGrpcEvitaValue(mutation.DefaultValue)
                : null,
            IndexedDecimalPlaces = mutation.IndexedDecimalPlaces
        };

        if (mutation.Unique != AttributeUniquenessType.NotUnique)
        {
            grpcMutation.UniqueInScopes.Add(new GrpcScopedAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique)
            });
        }

        if (mutation.UniqueGlobally != GlobalAttributeUniquenessType.NotUnique)
        {
            grpcMutation.UniqueGloballyInScopes.Add(new GrpcScopedGlobalAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally)
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

    public CreateGlobalAttributeSchemaMutation Convert(GrpcCreateGlobalAttributeSchemaMutation mutation)
    {
        return new CreateGlobalAttributeSchemaMutation(mutation.Name, mutation.Description, mutation.DeprecationNotice,
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
            EvitaEnumConverter.ToAttributeUniquenessType(mutation.UniqueInScopes, mutation.Unique),
            EvitaEnumConverter.ToGlobalAttributeUniquenessType(mutation.UniqueGloballyInScopes,
                mutation.UniqueGlobally),
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.FilterableInScopes, mutation.Filterable),
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.SortableInScopes, mutation.Sortable),
#pragma warning restore CS0612
            mutation.Localized,
            mutation.Nullable, mutation.Representative, EvitaDataTypesConverter.ToEvitaDataType(mutation.Type),
            mutation.DefaultValue is not null ? EvitaDataTypesConverter.ToEvitaValue(mutation.DefaultValue) : null,
            mutation.IndexedDecimalPlaces);
    }
}
