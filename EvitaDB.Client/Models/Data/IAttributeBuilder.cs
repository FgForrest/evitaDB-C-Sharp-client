using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data;

public interface IAttributeBuilder : IAttributeEditor<IAttributeBuilder>, IBuilder<Attributes, AttributeMutation>
{
    public static IAttributeSchema CreateImplicitSchema(AttributeValue attributeValue)
    {
        return AttributeSchema.InternalBuild(
            attributeValue.Key.AttributeName,
            attributeValue.Value!.GetType(),
            attributeValue.Key.Localized
        );
    }
}