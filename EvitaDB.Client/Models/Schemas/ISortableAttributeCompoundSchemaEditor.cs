namespace EvitaDB.Client.Models.Schemas;

public interface ISortableAttributeCompoundSchemaEditor<out TS> : ISortableAttributeCompoundSchema, INamedSchemaWithDeprecationEditor<TS>
where TS : ISortableAttributeCompoundSchemaEditor<TS>
{
}