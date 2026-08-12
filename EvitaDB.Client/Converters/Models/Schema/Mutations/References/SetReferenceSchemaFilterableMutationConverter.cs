using EvitaDB.Client.Models.Schemas.Mutations.References;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.References;

/// <summary>
/// Converts <see cref="SetReferenceSchemaIndexedMutation"/> to the gRPC message that evitaDB renamed from
/// `GrpcSetReferenceSchemaFilterableMutation` to `GrpcSetReferenceSchemaIndexedMutation`. Until the C# model
/// supports entity scopes, the boolean indexed flag maps to a filtering index in the live scope.
/// </summary>
public class SetReferenceSchemaFilterableMutationConverter : ISchemaMutationConverter<SetReferenceSchemaIndexedMutation, GrpcSetReferenceSchemaIndexedMutation>
{
    public GrpcSetReferenceSchemaIndexedMutation Convert(SetReferenceSchemaIndexedMutation mutation)
    {
        GrpcSetReferenceSchemaIndexedMutation grpcMutation = new()
        {
            Name = mutation.Name
        };
        if (mutation.Indexed)
        {
            grpcMutation.ScopedIndexTypes.Add(new GrpcScopedReferenceIndexType
            {
                Scope = GrpcEntityScope.ScopeLive,
                IndexType = GrpcReferenceIndexType.ReferenceIndexTypeForFiltering
            });
        }
        return grpcMutation;
    }

    public SetReferenceSchemaIndexedMutation Convert(GrpcSetReferenceSchemaIndexedMutation mutation)
    {
        bool indexed = mutation.ScopedIndexTypes.Any(it => it.IndexType != GrpcReferenceIndexType.ReferenceIndexTypeNone)
#pragma warning disable CS0612 // fallback to the deprecated field for messages produced by servers older than 2025.6
                       || mutation.IndexedInScopes.Count > 0;
#pragma warning restore CS0612
        return new SetReferenceSchemaIndexedMutation(mutation.Name, indexed);
    }
}
