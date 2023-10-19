namespace EvitaDB.Test.Utils;

public record TestAsDataObj(string Locale, string Url)
{
    public TestAsDataObj() : this("", "")
    {
    }
}