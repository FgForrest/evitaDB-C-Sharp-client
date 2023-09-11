using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Interface that simply combines writer and builder contracts together.
/// </summary>
public interface IAttributesBuilder : IAttributesEditor<IAttributesBuilder>, IBuilder<Attributes, AttributeMutation>
{
    internal static IAttributeSchema CreateImplicitSchema(AttributeValue attributeValue)
    {
        return AttributeSchema.InternalBuild(
            attributeValue.Key.AttributeName,
            attributeValue.Value!.GetType(),
            attributeValue.Key.Localized
        );
    }
}