using System.Globalization;
using Client.Exceptions;
using Client.Models.Schemas;
using Client.Utils;

namespace Client.Models.Data.Mutations.Attributes;

public class RemoveAttributeMutation : AttributeMutation
{
    public RemoveAttributeMutation(AttributeKey attributeKey) : base(attributeKey)
    {
    }
    
    public RemoveAttributeMutation(string attributeName) : base(new AttributeKey(attributeName))
    {
    }
    
    public RemoveAttributeMutation(string attributeName, CultureInfo locale) : base(new AttributeKey(attributeName, locale))
    {
    }

    public override AttributeValue MutateLocal(IEntitySchema entitySchema, AttributeValue? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException(
                "Cannot remove " + AttributeKey.AttributeName + " attribute - it doesn't exist!"
            )
        );
        return new AttributeValue(
            existingValue!.Version + 1,
            existingValue.Key,
            existingValue.Value ?? throw new EvitaInternalError("Attribute value is null when executing its removal!"),
            true
        );
    }
}