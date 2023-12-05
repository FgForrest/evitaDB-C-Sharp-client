namespace EvitaDB.Client.Queries.Head;

/// <summary>
/// Each query must specify collection. This mandatory <see cref="string"/> entity type controls what collection
/// the query will be applied on.
/// 
/// Sample of the header is:
/// <code>
/// collection('category')
/// </code>
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
