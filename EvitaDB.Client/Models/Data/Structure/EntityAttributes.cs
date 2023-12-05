using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

/// <summary>
/// Extension of <see cref="Attributes{T}"/> for entity attributes.
/// </summary>
public class EntityAttributes : Attributes<IEntityAttributeSchema>
{
    public EntityAttributes(IEntitySchema entitySchema, IDictionary<AttributeKey, AttributeValue> attributeValues,
        IDictionary<string, IEntityAttributeSchema> attributeTypes)
        : base(entitySchema, attributeValues, attributeTypes)
    {
    }
    
    public EntityAttributes(IEntitySchema entitySchema, ICollection<AttributeValue> attributeValues,
        IDictionary<string, IEntityAttributeSchema> attributeTypes)
        : base(entitySchema, attributeValues, attributeTypes)
    {
    }

    public EntityAttributes(IEntitySchema entitySchema) : base(entitySchema, new Dictionary<AttributeKey, AttributeValue>(),
        entitySchema.GetAttributes())
    {
    }

    protected override AttributeNotFoundException CreateAttributeNotFoundException(string attributeName)
    {
        return new AttributeNotFoundException(attributeName, EntitySchema);
    }
}
