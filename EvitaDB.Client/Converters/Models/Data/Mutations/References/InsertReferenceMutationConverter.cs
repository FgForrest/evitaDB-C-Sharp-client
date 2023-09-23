using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.References;

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