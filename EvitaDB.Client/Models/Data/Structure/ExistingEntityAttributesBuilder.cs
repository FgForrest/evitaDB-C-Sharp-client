using System.Collections.Immutable;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

public class ExistingEntityAttributesBuilder : ExistingAttributesBuilder<IEntityAttributeSchema, ExistingEntityAttributesBuilder>
{
    private readonly string _location;

    public ExistingEntityAttributesBuilder(IEntitySchema entitySchema, ICollection<AttributeValue> attributes,
        IDictionary<string, IEntityAttributeSchema> attributeTypes)
        : base(
            entitySchema, attributes, attributeTypes)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public ExistingEntityAttributesBuilder(IEntitySchema entitySchema, ICollection<AttributeValue> attributes,
        IDictionary<string, IEntityAttributeSchema> attributeTypes, bool suppressVerification)
        : base(entitySchema, attributes, attributeTypes,
            suppressVerification)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public ExistingEntityAttributesBuilder(IEntitySchema entitySchema, Attributes<IEntityAttributeSchema> attributes)
        : base(entitySchema, attributes)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public ExistingEntityAttributesBuilder(IEntitySchema entitySchema, Attributes<IEntityAttributeSchema> attributes,
        bool suppressVerification) : base(entitySchema, attributes, suppressVerification)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public override Attributes<IEntityAttributeSchema> Build()
    {
        if (AnyChangeInMutations())
        {
            ICollection<AttributeValue> newAttributeValues = GetAttributeValuesWithoutPredicate().ToList();
            IDictionary<string, IEntityAttributeSchema> newAttributeTypes =
                BaseAttributes.AttributeTypes.Values.Concat(newAttributeValues
                        // filter out new attributes that has no type yet
                        .Where(it => !BaseAttributes.AttributeTypes.ContainsKey(it.Key.AttributeName))
                        // create definition for them on the fly
                        .Select(IAttributesBuilder<IAttributeSchema>.CreateImplicitEntityAttributeSchema))
                    .ToImmutableDictionary(
                        x => x.Name,
                        x => x);
            return new EntityAttributes(
                BaseAttributes.EntitySchema,
                newAttributeValues,
                newAttributeTypes
            );
        }

        return BaseAttributes;
    }

    protected override Attributes<IEntityAttributeSchema> CreateAttributesContainer(IEntitySchema entitySchema,
        ICollection<AttributeValue> attributes, IDictionary<string, IEntityAttributeSchema> attributeTypes)
    {
        return new EntityAttributes(entitySchema, attributes, attributeTypes);
    }

    public override Func<string> GetLocationResolver()
    {
        return () => _location;
    }
}