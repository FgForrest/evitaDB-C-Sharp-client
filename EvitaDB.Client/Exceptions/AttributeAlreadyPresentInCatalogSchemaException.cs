using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class AttributeAlreadyPresentInCatalogSchemaException : EvitaInvalidUsageException
{
    public string CatalogName { get; }
    public IAttributeSchema ExistingSchema { get; }
    
    public AttributeAlreadyPresentInCatalogSchemaException(string catalogName, IAttributeSchema existingSchema) : base(
        $"Attribute with name {existingSchema.Name} already defined as global attribute of catalog {catalogName}, use `withCatalogAttribute` method to reuse it!.")
    {
        CatalogName = catalogName;
        ExistingSchema = existingSchema;
    }
}