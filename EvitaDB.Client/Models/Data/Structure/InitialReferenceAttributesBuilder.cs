using System.Collections.Immutable;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

public class
    InitialReferenceAttributesBuilder : InitialAttributesBuilder<IAttributeSchema, InitialReferenceAttributesBuilder>
{
    private readonly string _location;
    
    private IReferenceSchema ReferenceSchema { get; }

    public InitialReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema) :
        base(entitySchema)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public InitialReferenceAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema referenceSchema,
        bool suppressVerification) : base(entitySchema, suppressVerification)
    {
        ReferenceSchema = referenceSchema;
        _location = $"`{entitySchema.Name} reference {referenceSchema.Name}`";
    }

    public new IAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return ReferenceSchema.GetAttribute(attributeName);
    }

    public override Attributes<IAttributeSchema> Build()
    {
        IDictionary<string, IAttributeSchema> newAttributes = AttributeValues
            .Where(entry => ReferenceSchema.GetAttribute(entry.Key.AttributeName) is null)
            .Select(x => x.Value)
            .Select(IAttributesBuilder<IAttributeSchema>.CreateImplicitReferenceAttributeSchema)
            .ToImmutableDictionary(x => x.Name, x => x);

        return new ReferenceAttributes(
            EntitySchema,
            ReferenceSchema,
            AttributeValues.Values,
            !newAttributes.Any()
                ? ReferenceSchema.GetAttributes()
                : ReferenceSchema.GetAttributes().Concat(newAttributes)
                    .ToImmutableDictionary(x => x.Key, x => x.Value));
    }
    
    public override Func<string> GetLocationResolver()
    {
        return () => _location;
    }
}