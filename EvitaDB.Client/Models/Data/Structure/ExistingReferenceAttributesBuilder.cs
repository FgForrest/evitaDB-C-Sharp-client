using System.Collections.Immutable;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

public class
    ExistingReferenceAttributesBuilder : ExistingAttributesBuilder<IAttributeSchema, ExistingReferenceAttributesBuilder>
{
    private IReferenceSchema ReferenceSchema { get; }

    private readonly string _location;

    public ExistingReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema,
        ICollection<AttributeValue> attributes, IDictionary<string, IAttributeSchema> attributeTypes) : base(
        entitySchema, attributes, attributeTypes)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public ExistingReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema,
        ICollection<AttributeValue> attributes, IDictionary<string, IAttributeSchema> attributeTypes,
        bool suppressVerification) : base(entitySchema, attributes, attributeTypes,
        suppressVerification)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public ExistingReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema,
        Attributes<IAttributeSchema> attributes) : base(entitySchema, attributes)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public ExistingReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema,
        Attributes<IAttributeSchema> attributes, bool suppressVerification) : base(entitySchema,
        attributes, suppressVerification)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public override Attributes<IAttributeSchema> Build()
    {
        if (AnyChangeInMutations())
        {
            ICollection<AttributeValue> newAttributeValues = GetAttributeValuesWithoutPredicate().ToList();
            IDictionary<string, IAttributeSchema> newAttributeTypes =
                BaseAttributes.AttributeTypes.Values.Concat(newAttributeValues
                        // filter out new attributes that has no type yet
                        .Where(it => !BaseAttributes.AttributeTypes.ContainsKey(it.Key.AttributeName))
                        // create definition for them on the fly
                        .Select(IAttributesBuilder<IAttributeSchema>.CreateImplicitReferenceAttributeSchema))
                    .ToImmutableDictionary(
                        x => x.Name,
                        x => x);
            return new ReferenceAttributes(
                BaseAttributes.EntitySchema,
                ReferenceSchema,
                newAttributeValues,
                newAttributeTypes
            );
        }

        return BaseAttributes;
    }

    protected override Attributes<IAttributeSchema> CreateAttributesContainer(IEntitySchema entitySchema,
        ICollection<AttributeValue> attributes, IDictionary<string, IAttributeSchema> attributeTypes)
    {
        return new ReferenceAttributes(entitySchema, ReferenceSchema, attributes, attributeTypes);
    }
    
    public override Func<string> GetLocationResolver()
    {
        return () => _location;
    }
}