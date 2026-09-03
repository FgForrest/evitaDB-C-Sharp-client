using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

/// <summary>
/// Wire conversion for a reflected reference declaration.
/// </summary>
/// <remarks>
/// <para>
/// <b>Indexing and faceting are inherited by default.</b> A reflected reference exists to be the
/// server-maintained inverse of another reference, and the original already says whether it is indexed and
/// faceted; restating that here would let the two halves of one relation disagree. So
/// <c>indexedInherited</c> is always set, and <c>facetedInherited</c> unless the declaration asked for a
/// specific answer — which is the only thing the C# annotation can say.
/// </para>
/// <para>
/// The deprecated scalar <c>faceted</c> field is dual-written with the scoped list, the same fallback the
/// other reference converters use for servers older than 2024.12.
/// </para>
/// </remarks>
public class CreateReflectedReferenceSchemaMutationConverter : ISchemaMutationConverter<
    CreateReflectedReferenceSchemaMutation, GrpcCreateReflectedReferenceSchemaMutation>
{
    public GrpcCreateReflectedReferenceSchemaMutation Convert(CreateReflectedReferenceSchemaMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        GrpcCreateReflectedReferenceSchemaMutation grpcMutation = new()
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            ReferencedEntityType = mutation.ReferencedEntityType,
            ReflectedReferenceName = mutation.ReflectedReferenceName,
            AttributeInheritanceBehavior =
                EvitaEnumConverter.ToGrpcAttributeInheritanceBehavior(mutation.AttributeInheritanceBehavior),

            // Both sides of one relation, so the original decides. See the class remarks.
            IndexedInherited = true,
            FacetedInherited = mutation.Faceted is null
        };

        if (mutation.Cardinality is { } cardinality)
        {
            grpcMutation.Cardinality = EvitaEnumConverter.ToGrpcCardinality(cardinality);
        }

        if (mutation.AttributeInheritanceFilter is { Length: > 0 } filter)
        {
            grpcMutation.AttributeInheritanceFilter.AddRange(filter);
        }

        if (mutation.Faceted is { } faceted)
        {
#pragma warning disable CS0612 // the deprecated scalar is dual-written for servers older than 2024.12
            grpcMutation.Faceted = faceted;
#pragma warning restore CS0612

            if (faceted)
            {
                grpcMutation.FacetedInScopes.Add(GrpcEntityScope.ScopeLive);
            }
        }

        return grpcMutation;
    }

    public CreateReflectedReferenceSchemaMutation Convert(GrpcCreateReflectedReferenceSchemaMutation mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        return new CreateReflectedReferenceSchemaMutation(
            mutation.Name,
            mutation.Description,
            mutation.DeprecationNotice,
            mutation.Cardinality == GrpcCardinality.NotSpecified
                ? null
                : EvitaEnumConverter.ToCardinality(mutation.Cardinality),
            mutation.ReferencedEntityType,
            mutation.ReflectedReferenceName,
            mutation.FacetedInherited
                ? null
#pragma warning disable CS0612 // the deprecated scalar is read as a fallback for servers older than 2024.12
                : EvitaEnumConverter.ToScopedBooleanFlag(mutation.FacetedInScopes, mutation.Faceted ?? false),
#pragma warning restore CS0612
            EvitaEnumConverter.ToAttributeInheritanceBehavior(mutation.AttributeInheritanceBehavior),
            [.. mutation.AttributeInheritanceFilter]);
    }
}
