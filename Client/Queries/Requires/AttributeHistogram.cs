namespace Client.Queries.Requires;

public class AttributeHistogram : AbstractRequireConstraintLeaf, IExtraResultRequireConstraint
{
    private AttributeHistogram(params object[] arguments) : base(arguments)
    {
    }
    
    public AttributeHistogram(int requestedBucketCount, params string[] attributeNames) : base(new object[]{requestedBucketCount}.Concat(attributeNames))
    {
    }
    
    public int RequestedBucketCount => (int) Arguments[0]!;
    
    public string[] AttributeNames => Arguments.Skip(1).Select(obj => (string) obj!).ToArray();
    
    public new bool Applicable => IsArgumentsNonNull() && Arguments.Length > 1;
}