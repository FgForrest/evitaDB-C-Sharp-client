using Client.Models.Data.Mutations.Attributes;
using Client.Models.Data.Structure;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

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