using Client.Models.Data;
using Client.Models.Data.Mutations.Reference;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Reference;

public class InsertReferenceMutationConverter : ILocalMutationConverter<InsertReferenceMutation, GrpcInsertReferenceMutation>
{
    public GrpcInsertReferenceMutation Convert(InsertReferenceMutation mutation)
    {
        GrpcInsertReferenceMutation grpcInsertReferenceMutation = new()
        {
            ReferenceName = mutation.ReferenceKey.ReferenceName,
            ReferencePrimaryKey = mutation.ReferenceKey.PrimaryKey,
            ReferenceCardinality = EvitaEnumConverter.ToGrpcCardinality(mutation.ReferenceCardinality),
            ReferencedEntityType = mutation.ReferencedEntityType
        };

        return grpcInsertReferenceMutation;
    }

    public InsertReferenceMutation Convert(GrpcInsertReferenceMutation mutation)
    {
        return new InsertReferenceMutation(
            new ReferenceKey(mutation.ReferenceName, mutation.ReferencePrimaryKey),
            EvitaEnumConverter.ToCardinality(mutation.ReferenceCardinality),
            mutation.ReferencedEntityType
        );
    }
}