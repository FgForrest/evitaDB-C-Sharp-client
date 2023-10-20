using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Interface that simply combines writer and builder contracts together.
/// </summary>
public interface IAttributesBuilder<TS> : IAttributesEditor<IAttributesBuilder<TS>, TS>, IBuilder<Attributes<TS>, AttributeMutation>
    where TS : IAttributeSchema
{
    /// <summary>
    /// Returns human readable string representation of the attribute schema location.
    /// </summary>
    Func<string> GetLocationResolver();
    
    /// <summary>
    /// Method creates implicit attribute type for the attribute value that doesn't map to any existing (known) attribute
    /// type of the <see cref="IEntitySchema"/> schema.
    /// </summary>
    static IEntityAttributeSchema CreateImplicitEntityAttributeSchema(AttributeValue attributeValue) {
        return EntityAttributeSchema.InternalBuild(
            attributeValue.Key.AttributeName,
            attributeValue.Value!.GetType(),
            attributeValue.Key.Localized
        );
    }

    /// <summary>
    /// Method creates implicit attribute type for the attribute value that doesn't map to any existing (known) attribute
    /// type of the <see cref="IEntitySchema"/> schema.
    /// </summary>
    static IAttributeSchema CreateImplicitReferenceAttributeSchema(AttributeValue attributeValue) {
        return AttributeSchema.InternalBuild(
            attributeValue.Key.AttributeName,
            attributeValue.Value!.GetType(),
            attributeValue.Key.Localized
        );
    }
}