using Client.Converters.DataTypes;
using Client.DataTypes;
using Client.Exceptions;
using Client.Models.Data;
using Client.Models.Data.Mutations.Attributes;
using EvitaDB;

namespace Client.Converters.Models.Data.Mutations.Attributes;

public class
    ApplyDeltaMutationConverter : AttributeMutationConverter<ApplyDeltaAttributeMutation,
        GrpcApplyDeltaAttributeMutation>
{
    public override GrpcApplyDeltaAttributeMutation Convert(ApplyDeltaAttributeMutation mutation)
    {
        GrpcApplyDeltaAttributeMutation grpcApplyDeltaAttributeMutation = new()
        {
            AttributeName = mutation.AttributeKey.AttributeName,
        };

        if (mutation.AttributeKey.Localized)
        {
            grpcApplyDeltaAttributeMutation.AttributeLocale =
                EvitaDataTypesConverter.ToGrpcLocale(mutation.AttributeKey.Locale!);
        }

        switch (mutation)
        {
            case ApplyDeltaAttributeMutation<int> intMutation:
                grpcApplyDeltaAttributeMutation.IntegerDelta = intMutation.Delta;
                if (intMutation.RequiredRangeAfterApplication != null)
                {
                    grpcApplyDeltaAttributeMutation.IntegerRequiredRangeAfterApplication =
                        EvitaDataTypesConverter.ToGrpcIntegerNumberRange(intMutation.RequiredRangeAfterApplication);
                }
                break;
            case ApplyDeltaAttributeMutation<long> longMutation:
                grpcApplyDeltaAttributeMutation.LongDelta = longMutation.Delta;
                if (longMutation.RequiredRangeAfterApplication != null)
                {
                    grpcApplyDeltaAttributeMutation.LongRequiredRangeAfterApplication =
                        EvitaDataTypesConverter.ToGrpcLongNumberRange((LongNumberRange) longMutation.RequiredRangeAfterApplication);
                }
                break;
            case ApplyDeltaAttributeMutation<decimal> decimalMutation:
                grpcApplyDeltaAttributeMutation.BigDecimalDelta = EvitaDataTypesConverter.ToGrpcBigDecimal(decimalMutation.Delta);
                if (decimalMutation.RequiredRangeAfterApplication != null)
                {
                    grpcApplyDeltaAttributeMutation.BigDecimalRequiredRangeAfterApplication =
                        EvitaDataTypesConverter.ToGrpcBigDecimalNumberRange((DecimalNumberRange) decimalMutation.RequiredRangeAfterApplication);
                }
                break;
            default:
                throw new EvitaInternalError("This should never happen!");
        }

        return grpcApplyDeltaAttributeMutation;
    }

    public override ApplyDeltaAttributeMutation Convert(GrpcApplyDeltaAttributeMutation mutation)
    {
        AttributeKey key = BuildAttributeKey(mutation.AttributeName, mutation.AttributeLocale);
        switch (mutation.DeltaCase)
        {
            case GrpcApplyDeltaAttributeMutation.DeltaOneofCase.IntegerDelta:
                switch (mutation.RequiredRangeAfterApplicationCase)
                {
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase
                        .IntegerRequiredRangeAfterApplication:
                        return new ApplyDeltaAttributeMutation<int>(key, mutation.IntegerDelta,
                            EvitaDataTypesConverter.ToIntegerNumberRange(mutation
                                .IntegerRequiredRangeAfterApplication));
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase.None:
                        return new ApplyDeltaAttributeMutation<int>(key, mutation.IntegerDelta);
                    default:
                        throw new EvitaInvalidUsageException(
                            "In `GrpcApplyDeltaAttributeMutation`, RequiredRangeAfterApplication has to be the same type as the delta value, or none!");
                }
            case GrpcApplyDeltaAttributeMutation.DeltaOneofCase.LongDelta:
                switch (mutation.RequiredRangeAfterApplicationCase)
                {
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase
                        .LongRequiredRangeAfterApplication:
                        return new ApplyDeltaAttributeMutation<long>(key, mutation.LongDelta,
                            EvitaDataTypesConverter.ToLongNumberRange(mutation.LongRequiredRangeAfterApplication));
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase.None:
                        return new ApplyDeltaAttributeMutation<long>(key, mutation.LongDelta);
                    default:
                        throw new EvitaInvalidUsageException(
                            "In `GrpcApplyDeltaAttributeMutation`, RequiredRangeAfterApplication has to be the same type as the delta value, or none!");
                }
            case GrpcApplyDeltaAttributeMutation.DeltaOneofCase.BigDecimalDelta:
                switch (mutation.RequiredRangeAfterApplicationCase)
                {
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase
                        .BigDecimalRequiredRangeAfterApplication:
                        return new ApplyDeltaAttributeMutation<decimal>(key,
                            EvitaDataTypesConverter.ToDecimal(mutation.BigDecimalDelta),
                            EvitaDataTypesConverter.ToDecimalNumberRange(mutation
                                .BigDecimalRequiredRangeAfterApplication));
                    case GrpcApplyDeltaAttributeMutation.RequiredRangeAfterApplicationOneofCase.None:
                        return new ApplyDeltaAttributeMutation<decimal>(key,
                            EvitaDataTypesConverter.ToDecimal(mutation.BigDecimalDelta));
                    default:
                        throw new EvitaInvalidUsageException(
                            "In `GrpcApplyDeltaAttributeMutation`, RequiredRangeAfterApplication has to be the same type as the delta value, or none!");
                }
            default:
                throw new EvitaInvalidUsageException(
                    "Delta value has to be provided when applying `GrpcApplyDeltaAttributeMutation`!");
        }
    }
}