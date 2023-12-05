namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `attributeIs` is query that checks attribute for "special" value or constant that cannot be compared
/// like comparable of attribute with name passed in first argument.
/// First argument must be <see cref="string"/>. Second is one of the <see cref="AttributeSpecialValue"/>:
/// - <see cref="AttributeSpecialValue.Null"/>
/// - <see cref="AttributeSpecialValue.NotNull"/>
/// Function returns true if attribute has (explicitly or implicitly) passed special value.
/// Example:
/// <code>
/// attributeIs("visible", NULL)
/// </code>
/// Function supports attribute arrays in the same way as plain values.
/// </summary>
/// <seealso cref="AttributeSpecialValue"/>
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
