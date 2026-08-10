using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Attributes;

public abstract class AttributeMutation : ILocalMutation<AttributeValue>
{
    public AttributeKey AttributeKey { get; }
    public abstract Operation Operation { get; }

    protected AttributeMutation(AttributeKey attributeKey)
    {
        AttributeKey = attributeKey;
    }

    public abstract AttributeValue MutateLocal(IEntitySchema entitySchema, AttributeValue? existingValue);
}
