using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.References;

public class ReferenceAttributeMutationConverter : ILocalMutationConverter<ReferenceAttributeMutation, GrpcReferenceAttributeMutation>
{
    private static readonly DelegatingAttributeMutationConverter AttributeMutationConverter = new();

    public GrpcReferenceAttributeMutation Convert(ReferenceAttributeMutation mutation)
    {
        return new GrpcReferenceAttributeMutation
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey,
            AttributeMutation = AttributeMutationConverter.Convert(mutation.AttributeMutation)
        };
    }

    public ReferenceAttributeMutation Convert(GrpcReferenceAttributeMutation mutation)
    {
        return new ReferenceAttributeMutation(
            new ReferenceKey(mutation.ReferenceName,
                mutation.ReferencePrimaryKey),
            AttributeMutationConverter.Convert(mutation.AttributeMutation)
        );
    }
}