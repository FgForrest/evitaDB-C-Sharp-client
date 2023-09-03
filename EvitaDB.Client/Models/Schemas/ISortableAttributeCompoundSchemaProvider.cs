using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public interface ISortableAttributeCompoundSchemaProvider : IAttributeSchemaProvider<IAttributeSchema>
{
    IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds();
    SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name);
    SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name, NamingConvention namingConvention);
    IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName);
}