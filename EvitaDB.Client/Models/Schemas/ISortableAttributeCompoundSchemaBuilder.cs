using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Models.Schemas;

public interface ISortableAttributeCompoundSchemaBuilder : ISortableAttributeCompoundSchemaEditor<ISortableAttributeCompoundSchemaBuilder>
{
    ICollection<IEntitySchemaMutation> ToMutation();
    
    ICollection<ISortableAttributeCompoundSchemaMutation> ToSortableAttributeCompoundSchemaMutation();
    
    ICollection<IReferenceSchemaMutation> ToReferenceMutation(string referenceName);
    
    ISortableAttributeCompoundSchema ToInstance();
}