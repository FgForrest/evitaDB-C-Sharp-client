using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `inScope` filtering container is used in queries targeting multiple scopes to limit a part of the filter to
/// entities in a particular scope only (attributes archived entities are not indexed for may only be filtered in
/// the live scope, for example).
/// Example:
/// <code>
/// inScope(LIVE, attributeEquals("code", "vouchers"))
/// </code>
/// </summary>
public class FilterInScope : AbstractFilterConstraintContainer
{
    private const string ConstraintName = "inScope";

    public Scope Scope => (Scope) Arguments[0]!;

    public IFilterConstraint?[] Filtering => Children;

    private FilterInScope(object?[] arguments, params IFilterConstraint?[] children) : base(ConstraintName, arguments, children)
    {
    }

    public FilterInScope(Scope scope, params IFilterConstraint?[] filtering) : base(ConstraintName,
        new object[] {scope}, filtering)
    {
    }

    public new bool Necessary => Applicable;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new FilterInScope(Scope, children);
    }
}
