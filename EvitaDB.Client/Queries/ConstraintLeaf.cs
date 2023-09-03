using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Queries;

public abstract class ConstraintLeaf : BaseConstraint
{
    protected ConstraintLeaf(params object?[] arguments) : base(arguments)
    {
        if (arguments.Any(x => x is IConstraint))
        {
            throw new EvitaInvalidUsageException("Constraint argument is not allowed for leaf query (" + Name + ").");
        }
    }
    
    protected ConstraintLeaf(string? name, params object?[] arguments) : base(name, arguments)
    {
        if (arguments.Any(x => x is IConstraint))
        {
            throw new EvitaInvalidUsageException("Constraint argument is not allowed for leaf query (" + Name + ").");
        }
    }
}