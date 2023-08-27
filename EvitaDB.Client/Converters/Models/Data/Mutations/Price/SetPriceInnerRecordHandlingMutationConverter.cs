using Client.Exceptions;
using Client.Models.Data.Mutations.Price;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Price;

public class SetPriceInnerRecordHandlingMutationConverter : ILocalMutationConverter<SetPriceInnerRecordHandlingMutation,
    GrpcSetPriceInnerRecordHandlingMutation>
{
    public GrpcSetPriceInnerRecordHandlingMutation Convert(SetPriceInnerRecordHandlingMutation mutation)
    {
        return new GrpcSetPriceInnerRecordHandlingMutation
        {
            PriceInnerRecordHandling =
                EvitaEnumConverter.ToGrpcPriceInnerRecordHandling(mutation.PriceInnerRecordHandling)
        };
    }

    public SetPriceInnerRecordHandlingMutation Convert(GrpcSetPriceInnerRecordHandlingMutation mutation)
    {
        if (mutation.PriceInnerRecordHandling == GrpcPriceInnerRecordHandling.Unknown)
        {
            throw new EvitaInvalidUsageException("Unrecognized price inner record handling: " +
                                                 mutation.PriceInnerRecordHandling);
        }

        return new SetPriceInnerRecordHandlingMutation(
            EvitaEnumConverter.ToPriceInnerRecordHandling(mutation.PriceInnerRecordHandling));
    }
}