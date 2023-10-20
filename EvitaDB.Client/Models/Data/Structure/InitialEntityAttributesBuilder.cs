using System.Collections.Immutable;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Structure;

public class
    InitialEntityAttributesBuilder : InitialAttributesBuilder<IEntityAttributeSchema, InitialEntityAttributesBuilder>
{
    private readonly string _location;

    public InitialEntityAttributesBuilder(IEntitySchema entitySchema) : base(entitySchema)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public InitialEntityAttributesBuilder(IEntitySchema entitySchema, bool suppressVerification) : base(entitySchema,
        suppressVerification)
    {
        _location = $"`{entitySchema.Name}`";
    }

    public new IEntityAttributeSchema? GetAttributeSchema(string attributeName)
    {
        return EntitySchema.GetAttribute(attributeName);
    }

    public override EntityAttributes Build()
    {
        IDictionary<string, IEntityAttributeSchema> newAttributes = AttributeValues
            .Where(entry => EntitySchema.GetAttribute(entry.Key.AttributeName) is null)
            .Select(x => x.Value)
            .Select(IAttributesBuilder<IAttributeSchema>.CreateImplicitEntityAttributeSchema)
            .ToImmutableDictionary(x => x.Name, x => x);
        return new EntityAttributes(
            EntitySchema,
            AttributeValues.Values,
            !newAttributes.Any()
                ? EntitySchema.GetAttributes()
                : EntitySchema.GetAttributes().Concat(newAttributes)
                    .ToImmutableDictionary(
                        x => x.Key, 
                        x => x.Value)
                );
    }
    
    public override Func<string> GetLocationResolver()
    {
        return () => _location;
    }
}