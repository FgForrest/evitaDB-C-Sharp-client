using Client.Models.Data.Structure;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IAttributeBuilder : IAttributeEditor<IAttributeBuilder>, IBuilder<Attributes>
{
    public static AttributeSchema CreateImplicitSchema(AttributeValue attributeValue)
    {
        return AttributeSchema.InternalBuild(
            attributeValue.Key.AttributeName,
            attributeValue.Value!.GetType(),
            attributeValue.Key.Localized
        );
    }
}