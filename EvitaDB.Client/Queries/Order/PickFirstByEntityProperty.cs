namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// The `pickFirstByEntityProperty` ordering constraint (usable inside `referenceProperty` ordering of references
/// with multiple occurrences) selects which single reference occurrence is used for ordering, by picking the first
/// one according to the inner ordering constraints.
/// Example:
/// <code>
/// pickFirstByEntityProperty(attributeNatural("orderInGroup", ASC))
/// </code>
/// </summary>
public class PickFirstByEntityProperty : AbstractOrderConstraintContainer
{
    public IOrderConstraint?[] OrderBy => Children;

    public PickFirstByEntityProperty(params IOrderConstraint?[] orderBy) : base(orderBy)
    {
    }

    public new bool Necessary => Applicable;

    public override IOrderConstraint GetCopyWithNewChildren(IOrderConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new PickFirstByEntityProperty(children);
    }
}
