using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations.Attributes;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.Attributes;

public class SetAttributeSchemaGloballyUniqueMutationConverter : ISchemaMutationConverter<SetAttributeSchemaGloballyUniqueMutation, GrpcSetAttributeSchemaGloballyUniqueMutation>
{
    public GrpcSetAttributeSchemaGloballyUniqueMutation Convert(SetAttributeSchemaGloballyUniqueMutation mutation)
    {
        GrpcSetAttributeSchemaGloballyUniqueMutation grpcMutation = new GrpcSetAttributeSchemaGloballyUniqueMutation
        {
            Name = mutation.Name,
#pragma warning disable CS0612 // deprecated wire fields are dual-written for servers older than 2024.12
            UniqueGlobally = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally)
#pragma warning restore CS0612
        };

        if (mutation.UniqueGlobally != GlobalAttributeUniquenessType.NotUnique)
        {
            grpcMutation.UniqueGloballyInScopes.Add(new GrpcScopedGlobalAttributeUniquenessType
            {
                Scope = GrpcEntityScope.ScopeLive,
                UniquenessType = EvitaEnumConverter.ToGrpcGlobalAttributeUniquenessType(mutation.UniqueGlobally)
            });
        }

        return grpcMutation;
    }

    public SetAttributeSchemaGloballyUniqueMutation Convert(GrpcSetAttributeSchemaGloballyUniqueMutation mutation)
    {
#pragma warning disable CS0612 // deprecated wire fields are read as fallback for servers older than 2024.12
        return new SetAttributeSchemaGloballyUniqueMutation(mutation.Name,
            EvitaEnumConverter.ToGlobalAttributeUniquenessType(mutation.UniqueGloballyInScopes,
                mutation.UniqueGlobally));
#pragma warning restore CS0612
    }
}
