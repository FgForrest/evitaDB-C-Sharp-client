using EvitaDB;
using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

public class CreateReferenceSchemaMutationConverter : ISchemaMutationConverter<CreateReferenceSchemaMutation,
    GrpcCreateReferenceSchemaMutation>
{
    public GrpcCreateReferenceSchemaMutation Convert(CreateReferenceSchemaMutation mutation)
    {
        return new GrpcCreateReferenceSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            Cardinality = EvitaEnumConverter.ToGrpcCardinality(mutation.Cardinality),
            ReferencedEntityType = mutation.ReferencedEntityType,
            ReferencedEntityTypeManaged = mutation.ReferencedEntityTypeManaged,
            ReferencedGroupType = mutation.ReferencedGroupType,
            ReferencedGroupTypeManaged = mutation.ReferencedGroupTypeManaged,
            Filterable = mutation.Indexed,
            Faceted = mutation.Faceted
        };
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
            mutation.Filterable,
            mutation.Faceted
        );
    }
}