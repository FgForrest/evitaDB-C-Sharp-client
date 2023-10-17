namespace EvitaDB.TestX.Utils;

public record TestAsDataObj(string Locale, string Url)
{
    public TestAsDataObj() : this("", "")
    {
    }
}