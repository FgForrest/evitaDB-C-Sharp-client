using System.Globalization;
using Client.Models.Data.Mutations.Attributes;
using Client.Models.Mutations;
using Client.Models.Schemas;

namespace Client.Models.Data.Structure;

public class ExistingAttributesBuilder : IAttributeBuilder
{
    private IEntitySchema EntitySchema { get; }
    private IReferenceSchema? ReferenceSchema { get; }
    private Attributes BaseAttributes { get; }
    private bool SuppressVerification { get; }
    private IDictionary<AttributeKey, AttributeMutation> AttributeMutations { get; }
    public bool AttributesAvailable => BaseAttributes.AttributesAvailable;

    public ExistingAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema? referenceSchema,
        ICollection<AttributeValue> attributes, IDictionary<string, IAttributeSchema> attributeTypes)
    {
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = new Attributes(entitySchema, referenceSchema, attributes, attributeTypes);
        SuppressVerification = false;
    }
    
    public ExistingAttributesBuilder(IEntitySchema entitySchema, IReferenceSchema? referenceSchema,
        Attributes attributes)
    {
        EntitySchema = entitySchema;
        ReferenceSchema = referenceSchema;
        AttributeMutations = new Dictionary<AttributeKey, AttributeMutation>();
        BaseAttributes = attributes;
        SuppressVerification = false;
    }

    public object? GetAttribute(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object? GetAttribute(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public object[]? GetAttributeArray(string attributeName)
    {
        throw new NotImplementedException();
    }

    public object[]? GetAttributeArray(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(string attributeName)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public AttributeValue? GetAttributeValue(AttributeKey attributeKey)
    {
        throw new NotImplementedException();
    }

    public IAttributeSchema GetAttributeSchema(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<string> GetAttributeNames()
    {
        throw new NotImplementedException();
    }

    public ISet<AttributeKey> GetAttributeKeys()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue?> GetAttributeValues()
    {
        throw new NotImplementedException();
    }

    public ICollection<AttributeValue> GetAttributeValues(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ISet<CultureInfo> GetAttributeLocales()
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder RemoveAttribute(string attributeName)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder SetAttribute(string attributeName, object? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder SetAttribute(string attributeName, object[]? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder RemoveAttribute(string attributeName, CultureInfo locale)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder SetAttribute(string attributeName, CultureInfo locale, object? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue)
    {
        throw new NotImplementedException();
    }

    public IAttributeBuilder MutateAttribute(AttributeMutation mutation)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<AttributeMutation> BuildChangeSet()
    {
        throw new NotImplementedException();
    }

    public Attributes Build()
    {
        throw new NotImplementedException();
    }
    
    public bool Differs(Attributes attributes) =>  BaseAttributes != attributes;
}