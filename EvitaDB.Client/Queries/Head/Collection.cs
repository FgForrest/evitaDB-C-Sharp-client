namespace Client.Queries.Head;

/// <summary>
/// Blabla
/// </summary>
public class Collection : ConstraintLeaf, IHeadConstraint
{
    public override Type Type => typeof(IHeadConstraint);
    public override bool Applicable => IsArgumentsNonNull() && Arguments.Length == 1;
    public string EntityType => Arguments[0]?.ToString()!;

    public Collection(string entityType) : base(null, entityType)
    {
    }
    private Collection(params object[] arguments) : base(arguments) { }
    public override void Accept(IConstraintVisitor visitor)
    {
        visitor.Visit(this);
    }
}