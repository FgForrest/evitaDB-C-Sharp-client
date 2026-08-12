using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `inScope` require container is used in queries targeting multiple scopes to limit a part of the requirements
/// to entities in a particular scope only.
/// Example:
/// <code>
/// inScope(LIVE, facetSummary())
/// </code>
/// </summary>
public class RequireInScope : AbstractRequireConstraintContainer
{
    private const string ConstraintName = "inScope";

    public Scope Scope => (Scope) Arguments[0]!;

    public IRequireConstraint?[] Require => Children;

    private RequireInScope(object?[] arguments, params IRequireConstraint?[] children) : base(ConstraintName, arguments, children)
    {
    }

    public RequireInScope(Scope scope, params IRequireConstraint?[] require) : base(ConstraintName,
        new object[] {scope}, require)
    {
    }

    public new bool Necessary => Applicable;

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new RequireInScope(Scope, children);
    }
}
