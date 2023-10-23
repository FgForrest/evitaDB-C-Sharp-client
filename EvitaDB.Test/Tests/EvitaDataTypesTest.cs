using EvitaDB.Client.DataTypes;

namespace EvitaDB.Test.Tests;

public class EvitaDataTypesTest
{
    [Fact]
    public void ShouldFormatDateTimeOffsetsCorrectly()
    {
        string dateTimeOffset1 = "2023-09-08T14:08:26+02:00";
        Assert.Equal(dateTimeOffset1, EvitaDataTypes.FormatValue(DateTimeOffset.Parse(dateTimeOffset1)));
        
        string dateTimeOffset2 = "2023-10-21T11:44:03.6+02:00";
        Assert.Equal(dateTimeOffset2, EvitaDataTypes.FormatValue(DateTimeOffset.Parse(dateTimeOffset2)));
        
        string dateTimeOffset3 = "2023-10-21T11:44:03.68+02:00";
        Assert.Equal(dateTimeOffset3, EvitaDataTypes.FormatValue(DateTimeOffset.Parse(dateTimeOffset3)));
        
        string dateTimeOffset4 = "2023-10-21T11:44:03.681+02:00";
        Assert.Equal(dateTimeOffset4, EvitaDataTypes.FormatValue(DateTimeOffset.Parse(dateTimeOffset4)));
        
        // should match dateTimeOffset4, because at most 3 decimal places are supported
        string dateTimeOffset5 = "2023-10-21T11:44:03.6812+02:00";
        Assert.Equal(dateTimeOffset4, EvitaDataTypes.FormatValue(DateTimeOffset.Parse(dateTimeOffset5)));
    }
}

public class DateTimeOffsetData
{
    public static IEnumerable<object[]> Data =>
        new List<object[]>
        {
            new object[] { DateTimeOffset.Parse("2023-09-08T14:08:26+02:00") },
            new object[] { DateTimeOffset.Parse("2023-10-21T11:44:03.6+02:00") },
            new object[] { DateTimeOffset.Parse("2023-10-21T11:44:03.68+02:00") },
            new object[] { DateTimeOffset.Parse("2023-10-21T11:44:03.681+02:00") },
            new object[] { DateTimeOffset.Parse("2023-10-21T11:44:03.6812+02:00") }
        };
}