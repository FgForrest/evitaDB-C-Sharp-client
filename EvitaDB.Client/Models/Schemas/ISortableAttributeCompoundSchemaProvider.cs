using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Schemas;

public interface ISortableAttributeCompoundSchemaProvider : IAttributeSchemaProvider<IAttributeSchema>
{
    IDictionary<string, SortableAttributeCompoundSchema> GetSortableAttributeCompounds();
    SortableAttributeCompoundSchema? GetSortableAttributeCompound(string name);
    SortableAttributeCompoundSchema? GetSortableAttributeCompoundByName(string name, NamingConvention namingConvention);
    IList<SortableAttributeCompoundSchema> GetSortableAttributeCompoundsForAttribute(string attributeName);
}