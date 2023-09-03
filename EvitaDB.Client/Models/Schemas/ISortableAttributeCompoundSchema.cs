namespace EvitaDB.Client.Models.Schemas;

public interface ISortableAttributeCompoundSchema : INamedSchemaWithDeprecation
{
    IList<AttributeElement> AttributeElements { get; }
}