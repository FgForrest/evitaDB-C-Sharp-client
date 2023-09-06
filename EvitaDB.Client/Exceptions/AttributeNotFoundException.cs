using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

public class AttributeNotFoundException : EvitaInvalidUsageException
{
    public AttributeNotFoundException(string attributeName, ICatalogSchema catalogSchema) : 
        base("Global attribute with name `" + attributeName + "` is not present in schema of catalog `" + catalogSchema.Name + "`.")
    {
    }
    
    public AttributeNotFoundException(string attributeName, IEntitySchema entitySchema) : 
        base("Attribute with name `" + attributeName + "` is not present in schema of entity `" + entitySchema.Name + "`.")
    {
    }

    public AttributeNotFoundException(string attributeName, IReferenceSchema referenceSchema,
        IEntitySchema entitySchema)
        : base("Attribute with name `" + attributeName + "` is not present in schema of reference " +
               "`" + referenceSchema.Name + "` of entity `" + entitySchema.Name + "`.")
    {
    }
}