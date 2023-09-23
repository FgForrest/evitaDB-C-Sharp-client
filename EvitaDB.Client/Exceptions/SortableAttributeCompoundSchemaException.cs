using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Exceptions;

/// <summary>
/// Exception is thrown when invalid sortable attribute compound schema is about to be created or made invalid.
/// </summary>
public class SortableAttributeCompoundSchemaException : InvalidSchemaMutationException
{
    public ISortableAttributeCompoundSchema SortableAttributeCompoundSchema { get; }
    
    public SortableAttributeCompoundSchemaException(string message, ISortableAttributeCompoundSchema sortableAttributeCompoundSchema) : base(message + " Compound schema: " + sortableAttributeCompoundSchema)
    {
        SortableAttributeCompoundSchema = sortableAttributeCompoundSchema;
    }
}