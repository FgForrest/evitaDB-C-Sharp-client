using Client.Exceptions;
using Client.Utils;

namespace Client.Queries.Visitor;

public class FinderVisitor : IConstraintVisitor
{
    private readonly List<IConstraint> _result = new();
    private readonly Predicate<IConstraint> _matcher;
    private readonly Predicate<IConstraint> _stopper;

    public List<IConstraint> Results => _result;

    public IConstraint? Result
    {
        get
        {
            return _result.Count switch
            {
                0 => null,
                1 => _result[0],
                _ => throw new MoreThanSingleResultException($"Total {_result.Count} constraints found in query!")
            };
        }
    }

    private FinderVisitor(Predicate<IConstraint> matcher)
    {
        _matcher = matcher;
        _stopper = constraint => false;
    }

    private FinderVisitor(Predicate<IConstraint> matcher, Predicate<IConstraint> stopper)
    {
        _matcher = matcher;
        _stopper = stopper;
    }

    public static List<T> FindConstraints<T>(IConstraint constraint, Predicate<IConstraint> matcher) where T : IConstraint
    {
        FinderVisitor visitor = new(matcher);
        constraint.Accept(visitor);
        return visitor._result.Cast<T>().ToList();
    }

    public static T? FindConstraint<T>(IConstraint constraint, Predicate<IConstraint> matcher) where T : IConstraint
    {
        FinderVisitor visitor = new(matcher);
        constraint.Accept(visitor);
        return visitor._result.Count > 0 ? (T?) visitor._result[0] : default;
    }

    public static List<T> FindConstraints<T>(IConstraint constraint, Predicate<IConstraint> matcher, Predicate<IConstraint> stopper) where T : IConstraint
    {
        FinderVisitor visitor = new(matcher, stopper);
        constraint.Accept(visitor);
        return visitor.Results.Cast<T>().ToList();
    }
    
    public static T? FindConstraint<T>(IConstraint constraint, Predicate<IConstraint> matcher, Predicate<IConstraint> stopper) where T : IConstraint
    {
        FinderVisitor visitor = new(matcher, stopper);
        constraint.Accept(visitor);
        return (T?) visitor.Result;
    }

    public void Visit(IConstraint constraint)
    {
        if (_matcher.Invoke(constraint))
        {
            _result.Add(constraint);
        }
        if (!constraint.GetType().IsAssignableToGenericType(typeof(ConstraintContainer<>)))
        {
            return;
        }
        ConstraintContainer<IConstraint> cnt = (ConstraintContainer<IConstraint>) constraint;
        if (_stopper.Invoke(cnt))
        {
            return;
        }
        foreach (IConstraint child in cnt.Children)
        {
            child.Accept(this);
        }
        foreach (IConstraint additionalChild in cnt.AdditionalChildren)
        {
            additionalChild.Accept(this);
        }
    }
}