using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class CreateReferenceSchemaMutationConverter : ISchemaMutationConverter<CreateReferenceSchemaMutation,
    GrpcCreateReferenceSchemaMutation>
{
    public GrpcCreateReferenceSchemaMutation Convert(CreateReferenceSchemaMutation mutation)
    {
        GrpcCreateReferenceSchemaMutation grpcMutation = new GrpcCreateReferenceSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            Cardinality = EvitaEnumConverter.ToGrpcCardinality(mutation.Cardinality),
            ReferencedEntityType = mutation.ReferencedEntityType,
            ReferencedEntityTypeManaged = mutation.ReferencedEntityTypeManaged,
            ReferencedGroupType = mutation.ReferencedGroupType,
            ReferencedGroupTypeManaged = mutation.ReferencedGroupTypeManaged,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Filterable = mutation.Indexed,
            Faceted = mutation.Faceted
#pragma warning restore CS0612
        };

        if (mutation.Indexed)
        {
#pragma warning disable CS0612 // the deprecated scope list is dual-written for servers older than 2025.6
            grpcMutation.IndexedInScopes.Add(GrpcEntityScope.ScopeLive);
#pragma warning restore CS0612
            grpcMutation.ScopedIndexTypes.Add(new GrpcScopedReferenceIndexType
            {
                Scope = GrpcEntityScope.ScopeLive,
                IndexType = GrpcReferenceIndexType.ReferenceIndexTypeForFiltering
            });
        }

        if (mutation.Faceted)
        {
            grpcMutation.FacetedInScopes.Add(GrpcEntityScope.ScopeLive);
        }

        return grpcMutation;
    }

    public CreateReferenceSchemaMutation Convert(GrpcCreateReferenceSchemaMutation mutation)
    {
        return new CreateReferenceSchemaMutation(
            mutation.Name,
            mutation.Description,
            mutation.DeprecationNotice,
            EvitaEnumConverter.ToCardinality(mutation.Cardinality),
            mutation.ReferencedEntityType,
            mutation.ReferencedEntityTypeManaged,
            mutation.ReferencedGroupType,
            mutation.ReferencedGroupTypeManaged,
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2025.6 / 2024.12
            EvitaEnumConverter.ToReferenceIndexedFlag(mutation.ScopedIndexTypes, mutation.IndexedInScopes,
                mutation.Filterable),
            EvitaEnumConverter.ToScopedBooleanFlag(mutation.FacetedInScopes, mutation.Faceted)
#pragma warning restore CS0612
        );
    }
}