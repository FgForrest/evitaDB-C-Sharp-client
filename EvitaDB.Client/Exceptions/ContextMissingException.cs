namespace EvitaDB.Client.Exceptions;

public class ContextMissingException : EvitaInvalidUsageException
{
    public ContextMissingException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public ContextMissingException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public ContextMissingException(string privateMessage, string publicMessage, Exception exception) : base(
        privateMessage, publicMessage, exception)
    {
    }

    public ContextMissingException(string publicMessage) : base(publicMessage)
    {
    }

    public ContextMissingException() : base(
        "Query context is missing. You need to use method getPriceForSale(Currency, OffsetDateTime, Serializable...) " +
        "and provide the context on your own.")
    {
    }

    public static ContextMissingException LocaleForAttributeContextMissing(string attributeName)
    {
        return new ContextMissingException(
            "Attribute `" + attributeName + "` is localized. You need to use `entityLocaleEquals` constraint in " +
            "your filter part of the query, or you need to call `getAttribute()` method " +
            "with explicit locale argument!"
        );
    }

    public static ContextMissingException LocaleForAssociatedDataContextMissing(string associatedDataName)
    {
        return new ContextMissingException(
            "Associated data `" + associatedDataName +
            "` is localized. You need to use `entityLocaleEquals` constraint in " +
            "your filter part of the query, or you need to call `getAssociatedData()` method " +
            "with explicit locale argument!"
        );
    }
}