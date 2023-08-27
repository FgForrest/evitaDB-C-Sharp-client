using Client.Models.Schemas;

namespace Client.Models.Data.Mutations.Attributes;

public abstract class AttributeMutation : ILocalMutation<AttributeValue>
{
    public AttributeKey AttributeKey { get; }

    protected AttributeMutation(AttributeKey attributeKey)
    {
        AttributeKey = attributeKey;
    }

    public abstract AttributeValue MutateLocal(IEntitySchema entitySchema, AttributeValue? existingValue);
}