using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data.Mutations.Attributes;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class ReferenceAttributeMutation : ReferenceMutation
{
    public AttributeMutation AttributeMutation { get; }
    public AttributeKey AttributeKey { get; }
    private ReferenceKeyWithAttributeKey ComparableKey { get; }

    public ReferenceAttributeMutation(ReferenceKey referenceKey, AttributeMutation attributeMutation) :
        base(referenceKey)
    {
        AttributeMutation = attributeMutation;
        AttributeKey = attributeMutation.AttributeKey;
        ComparableKey = new ReferenceKeyWithAttributeKey(referenceKey, attributeMutation.AttributeKey);
    }

    public ReferenceAttributeMutation(string referenceName, int primaryKey, AttributeMutation attributeMutation) : this(
        new ReferenceKey(referenceName, primaryKey), attributeMutation)
    {
    }

    public override IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue)
    {
        Assert.IsTrue(
            existingValue is {Dropped: false},
            () => new InvalidMutationException("Cannot update attributes on reference " + ReferenceKey +
                                               " - reference doesn't exist!")
        );
        // this is kind of expensive, let's hope references will not have many attributes on them that frequently change
        ExistingAttributesBuilder attributeBuilder = new ExistingAttributesBuilder(
            entitySchema,
            existingValue!.ReferenceSchema,
            existingValue.GetAttributeValues()!,
            existingValue.ReferenceSchema is not null
                ? existingValue.ReferenceSchema.GetAttributes()
                : new Dictionary<string, IAttributeSchema>()
        );
        Structure.Attributes newAttributes = attributeBuilder
            .MutateAttribute(AttributeMutation)
            .Build();

        if (attributeBuilder.Differs(newAttributes))
        {
            return new Structure.Reference(
                entitySchema,
                existingValue.Version + 1,
                existingValue.ReferenceName, existingValue.ReferencedPrimaryKey,
                existingValue.ReferencedEntityType, existingValue.ReferenceCardinality,
                existingValue.Group,
                newAttributes
            );
        }

        return existingValue;
    }
}