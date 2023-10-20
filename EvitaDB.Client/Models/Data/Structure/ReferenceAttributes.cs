using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Extension of <see cref="Attributes"/> for reference attributes.
/// </summary>
public class ReferenceAttributes : Attributes<IAttributeSchema>
{
    private IReferenceSchema ReferenceSchema { get; }

    public ReferenceAttributes(IEntitySchema entitySchema, IReferenceSchema referenceSchema)
        : base(entitySchema, new List<AttributeValue>(), referenceSchema.GetAttributes())
    {
        ReferenceSchema = referenceSchema;
    }

    public ReferenceAttributes(
        IEntitySchema entitySchema,
        IReferenceSchema referenceSchema,
        ICollection<AttributeValue> attributeValues,
        IDictionary<string, IAttributeSchema> attributeTypes
    ) : base(entitySchema, attributeValues, attributeTypes)
    {
        ReferenceSchema = referenceSchema;
    }

    protected override AttributeNotFoundException CreateAttributeNotFoundException(string attributeName)
    {
        return new AttributeNotFoundException(attributeName, ReferenceSchema, EntitySchema);
    }
}