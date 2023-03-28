using Client.Models.Schemas.Dtos;

namespace Client.Exceptions;

public class AttributeAlreadyPresentInCatalogSchemaException : EvitaInvalidUsageException
{
    public string CatalogName { get; }
    public AttributeSchema ExistingSchema { get; }
    public AttributeAlreadyPresentInCatalogSchemaException(string privateMessage, string publicMessage) : base(privateMessage, publicMessage)
    {
    }

    public AttributeAlreadyPresentInCatalogSchemaException(string publicMessage, Exception exception) : base(publicMessage, exception)
    {
    }

    public AttributeAlreadyPresentInCatalogSchemaException(string privateMessage, string publicMessage, Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public AttributeAlreadyPresentInCatalogSchemaException(string publicMessage) : base(publicMessage)
    {
    }
    
    public AttributeAlreadyPresentInCatalogSchemaException(string catalogName, AttributeSchema existingSchema) : base(
        $"Attribute with name {existingSchema.Name} already defined as global attribute of catalog {catalogName}, use `withCatalogAttribute` method to reuse it!.")
    {
        CatalogName = catalogName;
        ExistingSchema = existingSchema;
    }
}