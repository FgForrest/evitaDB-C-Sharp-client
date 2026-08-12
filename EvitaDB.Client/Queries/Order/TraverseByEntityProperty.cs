namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `traverseByEntityProperty` ordering constraint (usable inside `referenceProperty` ordering of hierarchical
/// references) orders the referenced hierarchy nodes by traversing the hierarchy tree in the given
/// <see cref="TraversalMode"/> and sorting the nodes on each level by the inner ordering constraints.
/// Example:
/// <code>
/// traverseByEntityProperty(DEPTH_FIRST, attributeNatural("orderInCategory", ASC))
/// </code>
/// </summary>
public class TraverseByEntityProperty : AbstractOrderConstraintContainer
{
    public TraversalMode TraversalMode => Arguments.OfType<TraversalMode>().FirstOrDefault(TraversalMode.DepthFirst);

    public IOrderConstraint?[] OrderBy => Children;

    private TraverseByEntityProperty(object?[] arguments, params IOrderConstraint?[] children) : base(arguments, children)
    {
    }

    public TraverseByEntityProperty(params IOrderConstraint?[] orderBy) : this(TraversalMode.DepthFirst, orderBy)
    {
    }

    public TraverseByEntityProperty(TraversalMode traversalMode, params IOrderConstraint?[] orderBy) : base(
        new object[] {traversalMode}, orderBy)
    {
    }

    public new bool Necessary => Applicable;

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new TraverseByEntityProperty(TraversalMode, children);
    }
}
