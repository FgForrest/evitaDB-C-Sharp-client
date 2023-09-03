using System.Globalization;
using EvitaDB.Client.Models;
using EvitaDB;
using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Queries.Requires;
using Enum = System.Enum;
using QueryTelemetry = EvitaDB.Client.Models.ExtraResults.QueryTelemetry;

namespace EvitaDB.Client.Utils;

public class QueryConverter
{
    public static List<object> ConvertQueryParamsList(List<QueryParam> queryParams)
    {
        List<object> queryParamObject = new();
        foreach (var queryParam in queryParams)
        {
            queryParamObject.Add(ConvertQueryParam(queryParam));
        }

        return queryParamObject;
    }

    public static object ConvertQueryParam(QueryParam queryParam)
    {
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StringValue)
        {
            return queryParam.StringValue;
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.IntegerValue)
        {
            return queryParam.IntegerValue;
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LongValue)
        {
            return queryParam.LongValue;
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BooleanValue)
        {
            return queryParam.BooleanValue;
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BigDecimalValue)
        {
            return EvitaDataTypesConverter.ToDecimal(queryParam.BigDecimalValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.DateTimeRangeValue)
        {
            return EvitaDataTypesConverter.ToDateTimeRange(queryParam.DateTimeRangeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.IntegerNumberRangeValue)
        {
            return EvitaDataTypesConverter.ToIntegerNumberRange(queryParam.IntegerNumberRangeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LongNumberRangeValue)
        {
            return EvitaDataTypesConverter.ToLongNumberRange(queryParam.LongNumberRangeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BigDecimalNumberRangeValue)
        {
            return EvitaDataTypesConverter.ToDecimalNumberRange(queryParam.BigDecimalNumberRangeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.OffsetDateTimeValue)
        {
            return EvitaDataTypesConverter.ToDateTimeOffset(queryParam.OffsetDateTimeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LocaleValue)
        {
            return EvitaDataTypesConverter.ToLocale(queryParam.LocaleValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.CurrencyValue)
        {
            return EvitaDataTypesConverter.ToCurrency(queryParam.CurrencyValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.FacetStatisticsDepthValue)
        {
            return EvitaEnumConverter.ToFacetStatisticsDepth(queryParam.FacetStatisticsDepthValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.QueryPriceModelValue)
        {
            return EvitaEnumConverter.ToQueryPriceMode(queryParam.QueryPriceModelValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.PriceContentModeValue)
        {
            return EvitaEnumConverter.ToPriceContentMode(queryParam.PriceContentModeValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.AttributeSpecialValue)
        {
            return EvitaEnumConverter.ToAttributeSpecialValue(queryParam.AttributeSpecialValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.OrderDirectionValue)
        {
            return EvitaEnumConverter.ToOrderDirection(queryParam.OrderDirectionValue);
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.EmptyHierarchicalEntityBehaviour)
        {
            return EvitaEnumConverter.ToEmptyHierarchicalEntityBehaviour(queryParam.EmptyHierarchicalEntityBehaviour);
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StatisticsBase)
        {
            return EvitaEnumConverter.ToStatisticsBase(queryParam.StatisticsBase);
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StatisticsType)
        {
            return EvitaEnumConverter.ToStatisticsType(queryParam.StatisticsType);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StringArrayValue)
        {
            return EvitaDataTypesConverter.ToStringArray(queryParam.StringArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.IntegerArrayValue)
        {
            return EvitaDataTypesConverter.ToIntegerArray(queryParam.IntegerArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LongArrayValue)
        {
            return EvitaDataTypesConverter.ToLongArray(queryParam.LongArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BooleanArrayValue)
        {
            return EvitaDataTypesConverter.ToBooleanArray(queryParam.BooleanArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BigDecimalArrayValue)
        {
            return EvitaDataTypesConverter.ToDecimalArray(queryParam.BigDecimalArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.DateTimeRangeArrayValue)
        {
            return EvitaDataTypesConverter.ToDateTimeRangeArray(queryParam.DateTimeRangeArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.IntegerNumberRangeArrayValue)
        {
            return EvitaDataTypesConverter.ToIntegerNumberRangeArray(queryParam.IntegerNumberRangeArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LongNumberRangeArrayValue)
        {
            return EvitaDataTypesConverter.ToLongNumberRangeArray(queryParam.LongNumberRangeArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.BigDecimalNumberRangeArrayValue)
        {
            return EvitaDataTypesConverter.ToDecimalNumberRangeArray(queryParam.BigDecimalNumberRangeArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.OffsetDateTimeArrayValue)
        {
            return EvitaDataTypesConverter.ToDateTimeOffsetArray(queryParam.OffsetDateTimeArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.LocaleArrayValue)
        {
            return EvitaDataTypesConverter.ToLocaleArray(queryParam.LocaleArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.CurrencyArrayValue)
        {
            return EvitaDataTypesConverter.ToCurrencyArray(queryParam.CurrencyArrayValue);
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.FacetStatisticsDepthArrayValue)
        {
            return queryParam.FacetStatisticsDepthArrayValue.Value
                .Select(EvitaEnumConverter.ToFacetStatisticsDepth)
                .ToArray();
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.QueryPriceModelArrayValue)
        {
            return queryParam.QueryPriceModelArrayValue.Value
                .Select(EvitaEnumConverter.ToQueryPriceMode)
                .ToArray();
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.PriceContentModeArrayValue)
        {
            return queryParam.PriceContentModeArrayValue.Value
                .Select(EvitaEnumConverter.ToPriceContentMode)
                .ToArray();
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.AttributeSpecialArrayValue)
        {
            return queryParam.AttributeSpecialArrayValue.Value
                .Select(EvitaEnumConverter.ToAttributeSpecialValue)
                .ToArray();
        }

        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.OrderDirectionArrayValue)
        {
            return queryParam.OrderDirectionArrayValue.Value
                .Select(EvitaEnumConverter.ToOrderDirection)
                .ToArray();
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.EmptyHierarchicalEntityBehaviourArrayValue)
        {
            return queryParam.EmptyHierarchicalEntityBehaviourArrayValue.Value
                .Select(EvitaEnumConverter.ToEmptyHierarchicalEntityBehaviour)
                .ToArray();
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StatisticsBaseArrayValue)
        {
            return queryParam.StatisticsBaseArrayValue.Value
                .Select(EvitaEnumConverter.ToStatisticsBase)
                .ToArray();
        }
        
        if (queryParam.QueryParamCase == QueryParam.QueryParamOneofCase.StatisticsTypeArrayValue)
        {
            return queryParam.StatisticsTypeArrayValue.Value
                .Select(EvitaEnumConverter.ToStatisticsType)
                .ToArray();
        }

        throw new EvitaInvalidUsageException("Unsupported Evita data type `" + queryParam + "` in gRPC API.");
    }

    public static QueryParam ConvertQueryParam(object parameter)
    {
        QueryParam queryParam = new QueryParam();
        if (parameter is string stringValue)
        {
            queryParam.StringValue = stringValue;
        }
        else if (parameter is int integerValue)
        {
            queryParam.IntegerValue = integerValue;
        }
        else if (parameter is short shortValue)
        {
            queryParam.IntegerValue = shortValue;
        }
        else if (parameter is byte byteValue)
        {
            queryParam.IntegerValue = byteValue;
        }
        else if (parameter is long longValue)
        {
            queryParam.LongValue = longValue;
        }
        else if (parameter is bool booleanValue)
        {
            queryParam.BooleanValue = booleanValue;
        }
        else if (parameter is decimal decimalValue)
        {
            queryParam.BigDecimalValue = EvitaDataTypesConverter.ToGrpcBigDecimal(decimalValue);
        }
        else if (parameter is DateTimeRange dateTimeRangeValue)
        {
            queryParam.DateTimeRangeValue = EvitaDataTypesConverter.ToGrpcDateTimeRange(dateTimeRangeValue);
        }
        else if (parameter is ByteNumberRange byteNumberRangeValue)
        {
            queryParam.IntegerNumberRangeValue = EvitaDataTypesConverter.ToGrpcIntegerNumberRange(byteNumberRangeValue);
        }
        else if (parameter is ShortNumberRange shortNumberRangeValue)
        {
            queryParam.IntegerNumberRangeValue =
                EvitaDataTypesConverter.ToGrpcIntegerNumberRange(shortNumberRangeValue);
        }
        else if (parameter is IntegerNumberRange integerNumberRangeValue)
        {
            queryParam.IntegerNumberRangeValue =
                EvitaDataTypesConverter.ToGrpcIntegerNumberRange(integerNumberRangeValue);
        }
        else if (parameter is LongNumberRange longNumberRangeValue)
        {
            queryParam.LongNumberRangeValue = EvitaDataTypesConverter.ToGrpcLongNumberRange(longNumberRangeValue);
        }
        else if (parameter is DecimalNumberRange decimalNumberRangeValue)
        {
            queryParam.BigDecimalNumberRangeValue =
                EvitaDataTypesConverter.ToGrpcBigDecimalNumberRange(decimalNumberRangeValue);
        }
        else if (parameter is DateTimeOffset dateTimeOffsetValue)
        {
            queryParam.OffsetDateTimeValue = EvitaDataTypesConverter.ToGrpcDateTime(dateTimeOffsetValue);
        }
        else if (parameter is DateTime dateTimeValue)
        {
            queryParam.OffsetDateTimeValue = EvitaDataTypesConverter.ToGrpcDateTime(dateTimeValue);
        }
        else if (parameter is DateOnly dateOnlyValue)
        {
            queryParam.OffsetDateTimeValue = EvitaDataTypesConverter.ToGrpcDate(dateOnlyValue);
        }
        else if (parameter is TimeOnly timeOnlyValue)
        {
            queryParam.OffsetDateTimeValue = EvitaDataTypesConverter.ToGrpcTime(timeOnlyValue);
        }
        else if (parameter is CultureInfo localeValue)
        {
            queryParam.LocaleValue = EvitaDataTypesConverter.ToGrpcLocale(localeValue);
        }
        else if (parameter is Currency currencyValue)
        {
            queryParam.CurrencyValue = EvitaDataTypesConverter.ToGrpcCurrency(currencyValue);
        }
        else if (parameter is FacetStatisticsDepth facetStatisticsDepthValue)
        {
            queryParam.FacetStatisticsDepthValue = EvitaEnumConverter.ToGrpcFacetStatisticsDepth(facetStatisticsDepthValue);
        }
        else if (parameter is QueryPriceMode queryPriceModeValue)
        {
            queryParam.QueryPriceModelValue = EvitaEnumConverter.ToGrpcQueryPriceMode(queryPriceModeValue);
        }
        else if (parameter is PriceContentMode priceContentModeValue)
        {
            queryParam.PriceContentModeValue = EvitaEnumConverter.ToGrpcPriceContentMode(priceContentModeValue);
        }
        else if (parameter is AttributeSpecialValue attributeSpecialValue)
        {
            queryParam.AttributeSpecialValue = EvitaEnumConverter.ToGrpcAttributeSpecialValue(attributeSpecialValue);
        }
        else if (parameter is OrderDirection orderDirectionValue)
        {
            queryParam.OrderDirectionValue = EvitaEnumConverter.ToGrpcOrderDirection(orderDirectionValue);
        }
        else if (parameter is EmptyHierarchicalEntityBehaviour emptyHierarchicalEntityBehaviour)
        {
            queryParam.EmptyHierarchicalEntityBehaviour = EvitaEnumConverter.ToGrpcEmptyHierarchicalEntityBehaviour(emptyHierarchicalEntityBehaviour);
        }
        else if (parameter is StatisticsBase statisticsBase)
        {
            queryParam.StatisticsBase = EvitaEnumConverter.ToGrpcStatisticsBase(statisticsBase);
        }
        else if (parameter is StatisticsType statisticsType)
        {
            queryParam.StatisticsType = EvitaEnumConverter.ToGrpcStatisticsType(statisticsType);
        }

        else if (parameter is string[] stringArrayValue)
        {
            queryParam.StringArrayValue = EvitaDataTypesConverter.ToGrpcStringArray(stringArrayValue);
        }
        else if (parameter is int[] integerArrayValue)
        {
            queryParam.IntegerArrayValue = EvitaDataTypesConverter.ToGrpcIntegerArray(integerArrayValue);
        }
        else if (parameter is short[] shortArrayValue)
        {
            queryParam.IntegerArrayValue = EvitaDataTypesConverter.ToGrpcShortArray(shortArrayValue);
        }
        else if (parameter is byte[] byteArrayValue)
        {
            queryParam.IntegerArrayValue = EvitaDataTypesConverter.ToGrpcByteArray(byteArrayValue);
        }
        else if (parameter is long[] longArrayValue)
        {
            queryParam.LongArrayValue = EvitaDataTypesConverter.ToGrpcLongArray(longArrayValue);
        }
        else if (parameter is bool[] booleanArrayValue)
        {
            queryParam.BooleanArrayValue = EvitaDataTypesConverter.ToGrpcBooleanArray(booleanArrayValue);
        }
        else if (parameter is decimal[] decimalArrayValue)
        {
            queryParam.BigDecimalArrayValue = EvitaDataTypesConverter.ToGrpcBigDecimalArray(decimalArrayValue);
        }
        else if (parameter is DateTimeRange[] dateTimeRangeArrayValue)
        {
            queryParam.DateTimeRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcDateTimeRangeArray(dateTimeRangeArrayValue);
        }
        else if (parameter is ByteNumberRange[] byteNumberRangeArrayValue)
        {
            queryParam.IntegerNumberRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcIntegerNumberRangeArray(byteNumberRangeArrayValue);
        }
        else if (parameter is ShortNumberRange[] shortNumberRangeArrayValue)
        {
            queryParam.IntegerNumberRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcIntegerNumberRangeArray(shortNumberRangeArrayValue);
        }
        else if (parameter is IntegerNumberRange[] integerNumberRangeArrayValue)
        {
            queryParam.IntegerNumberRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcIntegerNumberRangeArray(integerNumberRangeArrayValue);
        }
        else if (parameter is LongNumberRange[] longNumberRangeArrayValue)
        {
            queryParam.LongNumberRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcLongNumberRangeArray(longNumberRangeArrayValue);
        }
        else if (parameter is DecimalNumberRange[] decimalNumberRangeArrayValue)
        {
            queryParam.BigDecimalNumberRangeArrayValue =
                EvitaDataTypesConverter.ToGrpcBigDecimalNumberRangeArray(decimalNumberRangeArrayValue);
        }
        else if (parameter is DateTimeOffset[] dateTimeOffsetArrayValue)
        {
            queryParam.OffsetDateTimeArrayValue =
                EvitaDataTypesConverter.ToGrpcDateTimeArray(dateTimeOffsetArrayValue);
        }
        else if (parameter is DateTime[] dateTimeArrayValue)
        {
            queryParam.OffsetDateTimeArrayValue =
                EvitaDataTypesConverter.ToGrpcDateTimeArray(dateTimeArrayValue);
        }
        else if (parameter is DateOnly[] dateOnlyArrayValue)
        {
            queryParam.OffsetDateTimeArrayValue =
                EvitaDataTypesConverter.ToGrpcDateArray(dateOnlyArrayValue);
        }
        else if (parameter is TimeOnly[] timeOnlyArrayValue)
        {
            queryParam.OffsetDateTimeArrayValue =
                EvitaDataTypesConverter.ToGrpcTimeArray(timeOnlyArrayValue);
        }
        else if (parameter is CultureInfo[] localeArrayValue)
        {
            queryParam.LocaleArrayValue = EvitaDataTypesConverter.ToGrpcLocaleArray(localeArrayValue);
        }
        else if (parameter is Currency[] currencyArrayValue)
        {
            queryParam.CurrencyArrayValue = EvitaDataTypesConverter.ToGrpcCurrencyArray(currencyArrayValue);
        }
        else if (parameter is FacetStatisticsDepth[] facetStatisticsDepthArrayValue)
        {
            queryParam.FacetStatisticsDepthArrayValue = new GrpcFacetStatisticsDepthArray
                {Value = {facetStatisticsDepthArrayValue.Select(EvitaEnumConverter.ToGrpcFacetStatisticsDepth)}};
        }
        else if (parameter is QueryPriceMode[] queryPriceModeArrayValue)
        {
            queryParam.QueryPriceModelArrayValue = new GrpcQueryPriceModeArray
                {Value = {queryPriceModeArrayValue.Select(EvitaEnumConverter.ToGrpcQueryPriceMode)}};
        }
        else if (parameter is PriceContentMode[] priceContentModeArrayValue)
        {
            queryParam.PriceContentModeArrayValue = new GrpcPriceContentModeArray
                {Value = {priceContentModeArrayValue.Select(EvitaEnumConverter.ToGrpcPriceContentMode)}};
        }
        else if (parameter is AttributeSpecialValue[] attributeSpecialValueArrayValue)
        {
            queryParam.AttributeSpecialArrayValue = new GrpcAttributeSpecialValueArray
                {Value = {attributeSpecialValueArrayValue.Select(EvitaEnumConverter.ToGrpcAttributeSpecialValue)}};
        }
        else if (parameter is OrderDirection[] orderDirectionArrayValue)
        {
            queryParam.OrderDirectionArrayValue = new GrpcOrderDirectionArray
                {Value = {orderDirectionArrayValue.Select(EvitaEnumConverter.ToGrpcOrderDirection)}};
        }
        else if (parameter is EmptyHierarchicalEntityBehaviour[] emptyHierarchicalEntityBehaviourArray)
        {
            queryParam.EmptyHierarchicalEntityBehaviourArrayValue = new GrpcEmptyHierarchicalEntityBehaviourArray
                {Value = {emptyHierarchicalEntityBehaviourArray.Select(EvitaEnumConverter.ToGrpcEmptyHierarchicalEntityBehaviour)}};
        }
        else if (parameter is StatisticsBase[] statisticsBaseArray)
        {
            queryParam.StatisticsBaseArrayValue = new GrpcStatisticsBaseArray
                {Value = {statisticsBaseArray.Select(EvitaEnumConverter.ToGrpcStatisticsBase)}};
        }
        else if (parameter is StatisticsType[] statisticsTypeArray)
        {
            queryParam.StatisticsTypeArrayValue = new GrpcStatisticsTypeArray
                {Value = {statisticsTypeArray.Select(EvitaEnumConverter.ToGrpcStatisticsType)}};
        }
        else
        {
            throw new EvitaInvalidUsageException("Unsupported Evita data type `" + parameter.GetType().Name + "` in gRPC API.");
        }

        return queryParam;
    }
}