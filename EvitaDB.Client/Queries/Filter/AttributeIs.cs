namespace Client.Queries.Filter;

public class AttributeIs : AbstractAttributeFilterConstraintLeaf
{
    private AttributeIs(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeIs(string attributeName, AttributeSpecialValue value) : base(attributeName, value)
    {
    }
    
    public AttributeSpecialValue SpecialValue => (AttributeSpecialValue) Arguments[1]!;
    
    public override bool Applicable => Arguments.Length == 2 && IsArgumentsNonNull();
}