using EvitaDB.Client.Converters.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Converters.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Converters.Models.Data.Mutations.Entities;
using EvitaDB.Client.Converters.Models.Data.Mutations.Prices;
using EvitaDB.Client.Converters.Models.Data.Mutations.References;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Mutations.Entities;
using EvitaDB.Client.Models.Data.Mutations.Prices;
using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Converters.Models.Data.Mutations;

public class DelegatingLocalMutationConverter : ILocalMutationConverter<ILocalMutation, GrpcLocalMutation>
{
    public GrpcLocalMutation Convert(ILocalMutation mutation)
    {
        GrpcLocalMutation grpcLocalMutation = new();
        switch (mutation)
        {
            case ApplyDeltaAttributeMutation applyDeltaAttributeMutation:
                grpcLocalMutation.ApplyDeltaAttributeMutation =
                    new ApplyDeltaMutationConverter().Convert(applyDeltaAttributeMutation);
                break;
            case UpsertAttributeMutation upsertAttributeMutation:
                grpcLocalMutation.UpsertAttributeMutation =
                    new UpsertAttributeMutationConverter().Convert(upsertAttributeMutation);
                break;
            case RemoveAttributeMutation removeAttributeMutation:
                grpcLocalMutation.RemoveAttributeMutation =
                    new RemoveAttributeMutationConverter().Convert(removeAttributeMutation);
                break;
            case UpsertAssociatedDataMutation upsertAssociatedDataMutation:
                grpcLocalMutation.UpsertAssociatedDataMutation =
                    new UpsertAssociatedDataMutationConverter().Convert(upsertAssociatedDataMutation);
                break;
            case RemoveAssociatedDataMutation removeAssociatedDataMutation:
                grpcLocalMutation.RemoveAssociatedDataMutation =
                    new RemoveAssociatedDataMutationConverter().Convert(removeAssociatedDataMutation);
                break;
            case UpsertPriceMutation upsertPriceMutation:
                grpcLocalMutation.UpsertPriceMutation = new UpsertPriceMutationConverter().Convert(upsertPriceMutation);
                break;
            case RemovePriceMutation removePriceMutation:
                grpcLocalMutation.RemovePriceMutation = new RemovePriceMutationConverter().Convert(removePriceMutation);
                break;
            case SetPriceInnerRecordHandlingMutation setPriceInnerRecordHandlingMutation:
                grpcLocalMutation.SetPriceInnerRecordHandlingMutation =
                    new SetPriceInnerRecordHandlingMutationConverter().Convert(setPriceInnerRecordHandlingMutation);
                break;
            case SetParentMutation setParentMutation:
                grpcLocalMutation.SetParentMutation = new SetParentMutationConverter().Convert(setParentMutation);
                break;
            case RemoveParentMutation removeParentMutation:
                grpcLocalMutation.RemoveParentMutation =
                    new RemoveParentMutationConverter().Convert(removeParentMutation);
                break;
            case InsertReferenceMutation insertReferenceMutation:
                grpcLocalMutation.InsertReferenceMutation =
                    new InsertReferenceMutationConverter().Convert(insertReferenceMutation);
                break;
            case RemoveReferenceMutation removeReferenceMutation:
                grpcLocalMutation.RemoveReferenceMutation =
                    new RemoveReferenceMutationConverter().Convert(removeReferenceMutation);
                break;
            case SetReferenceGroupMutation setReferenceGroupMutation:
                grpcLocalMutation.SetReferenceGroupMutation =
                    new SetReferenceGroupMutationConverter().Convert(setReferenceGroupMutation);
                break;
            case RemoveReferenceGroupMutation removeReferenceGroupMutation:
                grpcLocalMutation.RemoveReferenceGroupMutation =
                    new RemoveReferenceGroupMutationConverter().Convert(removeReferenceGroupMutation);
                break;
            case ReferenceAttributeMutation referenceAttributeMutation:
                grpcLocalMutation.ReferenceAttributeMutation =
                    new ReferenceAttributeMutationConverter().Convert(referenceAttributeMutation);
                break;
            default:
                throw new EvitaInternalError("This should never happen!");
        }

        return grpcLocalMutation;
    }

    public ILocalMutation Convert(GrpcLocalMutation mutation)
    {
        return mutation.MutationCase switch
        {
            GrpcLocalMutation.MutationOneofCase.ApplyDeltaAttributeMutation =>
                new ApplyDeltaMutationConverter().Convert(mutation.ApplyDeltaAttributeMutation),
            GrpcLocalMutation.MutationOneofCase.UpsertAttributeMutation => new UpsertAttributeMutationConverter()
                .Convert(mutation.UpsertAttributeMutation),
            GrpcLocalMutation.MutationOneofCase.RemoveAttributeMutation => new RemoveAttributeMutationConverter()
                .Convert(mutation.RemoveAttributeMutation),
            GrpcLocalMutation.MutationOneofCase.UpsertAssociatedDataMutation =>
                new UpsertAssociatedDataMutationConverter().Convert(mutation.UpsertAssociatedDataMutation),
            GrpcLocalMutation.MutationOneofCase.RemoveAssociatedDataMutation =>
                new RemoveAssociatedDataMutationConverter().Convert(mutation.RemoveAssociatedDataMutation),
            GrpcLocalMutation.MutationOneofCase.UpsertPriceMutation => new UpsertPriceMutationConverter().Convert(
                mutation.UpsertPriceMutation),
            GrpcLocalMutation.MutationOneofCase.RemovePriceMutation => new RemovePriceMutationConverter().Convert(
                mutation.RemovePriceMutation),
            GrpcLocalMutation.MutationOneofCase.SetPriceInnerRecordHandlingMutation =>
                new SetPriceInnerRecordHandlingMutationConverter().Convert(mutation
                    .SetPriceInnerRecordHandlingMutation),
            GrpcLocalMutation.MutationOneofCase.SetParentMutation => new SetParentMutationConverter().Convert(
                mutation.SetParentMutation),
            GrpcLocalMutation.MutationOneofCase.RemoveParentMutation => new RemoveParentMutationConverter().Convert(
                mutation.RemoveParentMutation),
            GrpcLocalMutation.MutationOneofCase.InsertReferenceMutation => new InsertReferenceMutationConverter()
                .Convert(mutation.InsertReferenceMutation),
            GrpcLocalMutation.MutationOneofCase.RemoveReferenceMutation => new RemoveReferenceMutationConverter()
                .Convert(mutation.RemoveReferenceMutation),
            GrpcLocalMutation.MutationOneofCase.SetReferenceGroupMutation => new SetReferenceGroupMutationConverter()
                .Convert(mutation.SetReferenceGroupMutation),
            GrpcLocalMutation.MutationOneofCase.RemoveReferenceGroupMutation =>
                new RemoveReferenceGroupMutationConverter().Convert(mutation.RemoveReferenceGroupMutation),
            GrpcLocalMutation.MutationOneofCase.ReferenceAttributeMutation => new ReferenceAttributeMutationConverter()
                .Convert(mutation.ReferenceAttributeMutation),
            _ => throw new EvitaInternalError("This should never happen!")
        };
    }
}