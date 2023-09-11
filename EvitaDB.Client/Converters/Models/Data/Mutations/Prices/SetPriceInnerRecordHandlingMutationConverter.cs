using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.Prices;

namespace EvitaDB.Client.Converters.Models.Data.Mutations.Prices;

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