using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaUniqueMutationConverter : ISchemaMutationConverter<SetAttributeSchemaUniqueMutation, GrpcSetAttributeSchemaUniqueMutation>
{
    public GrpcSetAttributeSchemaUniqueMutation Convert(SetAttributeSchemaUniqueMutation mutation)
    {
        GrpcSetAttributeSchemaUniqueMutation grpcMutation = new GrpcSetAttributeSchemaUniqueMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            Unique = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique)
#pragma warning restore CS0612
        };

        if (mutation.Unique != AttributeUniquenessType.NotUnique)
        {
            grpcMutation.UniqueInScopes.Add(new GrpcScopedAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcAttributeUniquenessType(mutation.Unique)
            });
        }

        return grpcMutation;
    }

    public SetAttributeSchemaUniqueMutation Convert(GrpcSetAttributeSchemaUniqueMutation mutation)
    {
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
        return new SetAttributeSchemaUniqueMutation(mutation.Name,
            EvitaEnumConverter.ToAttributeUniquenessType(mutation.UniqueInScopes, mutation.Unique));
#pragma warning restore CS0612
    }
}
