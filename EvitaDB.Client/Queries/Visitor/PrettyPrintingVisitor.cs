using System.Collections.Immutable;
using System.Text;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Visitor;

public class PrettyPrintingVisitor : IConstraintVisitor
{
    private readonly StringBuilder _result = new();
    private readonly LinkedList<object>? _parameters;
    private readonly string? _indent;
    private readonly bool _extractParameters;
    private int Level { get; set; }
    private bool FirstConstraint { get; set; } = true;

    public static StringWithParameters ToStringWithParameterExtraction(Query query, string? indent = null)
    {
        PrettyPrintingVisitor visitor = new(indent, true);
        visitor.Traverse(query);
        return visitor.GetResultWithExtractedParameters();
    }

    public static StringWithParameters ToStringWithParameterExtraction(string? indent = null,
        params IConstraint?[] constraints)
    {
        PrettyPrintingVisitor visitor = new(indent, true);
        foreach (var constraint in constraints)
        {
            constraint?.Accept(visitor);
        }

        return visitor.GetResultWithExtractedParameters();
    }

    public static StringWithParameters ToStringWithParameterExtraction(params IConstraint[] constraints)
    {
        PrettyPrintingVisitor visitor = new PrettyPrintingVisitor(null, true);
        foreach (IConstraint theConstraint in constraints)
        {
            visitor.NextConstraint();
            theConstraint.Accept(visitor);
        }

        return visitor.GetResultWithExtractedParameters();
    }

    private PrettyPrintingVisitor(string? indent)
    {
        Level = 0;
        _indent = indent;
        _extractParameters = false;
        _parameters = null;
    }

    private PrettyPrintingVisitor(string? indent, bool extractParameters)
    {
        Level = 0;
        _indent = indent;
        _extractParameters = extractParameters;
        _parameters = new LinkedList<object>();
    }

    public void Traverse(Query query)
    {
        _result.Append("query" + QueryUtils.ArgOpening).Append(NewLine());
        Level = 1;
        if (query.Collection is not null)
        {
            query.Collection.Accept(this);
            _result.Append(',');
        }

        if (query.FilterBy is not null)
        {
            query.FilterBy.Accept(this);
            _result.Append(',');
        }

        if (query.OrderBy is not null)
        {
            query.OrderBy.Accept(this);
            _result.Append(',');
        }

        if (query.Require is not null)
        {
            query.Require.Accept(this);
            _result.Append(',');
        }

        _result.Length -= ",".Length;
        _result.Append(NewLine()).Append(QueryUtils.ArgClosing);
    }

    public string GetResult()
    {
        return _result.ToString();
    }

    public StringWithParameters GetResultWithExtractedParameters()
    {
        return new StringWithParameters(
            _result.ToString(),
            _parameters == null ? new List<object>() : _parameters.ToList().ToImmutableList()
        );
    }

    private string NewLine() => _indent == null ? "" : "\n";

    public StringBuilder NextArgument() => _result.Append(',');

    public StringBuilder NextConstraint() => FirstConstraint ? _result : _result.Append(',');

    private void Indent(string? indent, int repeatCount)
    {
        if (indent != null)
        {
            _result.Append(string.Concat(Enumerable.Repeat(indent, repeatCount)));
        }
    }

    private void PrintContainer(IConstraintContainer<IConstraint> constraint)
    {
        if (constraint.ExplicitChildren.Length == 0 && constraint.ExplicitAdditionalChildren.Length == 0)
        {
            PrintLeaf(constraint);
            return;
        }

        Level++;
        if (constraint.Applicable)
        {
            IConstraint?[] children = constraint.ExplicitChildren;
            int childrenLength = children.Length;

            IConstraint?[] additionalChildren = constraint.ExplicitAdditionalChildren;
            int additionalChildrenLength = additionalChildren.Length;

            object?[]? arguments = constraint.Arguments;
            int? argumentsLength = arguments.Length;

            // print arguments
            for (int i = 0; i < argumentsLength; i++)
            {
                object? argument = arguments?[i];
                if (constraint is IConstraintWithSuffix cws && cws.ArgumentImplicitForSuffix(argument!))
                {
                    continue;
                }

                if (argument is null)
                {
                    continue;
                }

                _result.Append(NewLine());
                Indent(_indent, Level);
                if (_extractParameters)
                {
                    _result.Append('?');
                    _parameters?.AddLast(argument);
                }
                else
                {
                    _result.Append(EvitaDataTypes.FormatValue(argument));
                }

                if (i + 1 < childrenLength || additionalChildrenLength > 0 || childrenLength > 0)
                {
                    NextArgument();
                }
            }

            // print additional children
            for (int i = 0; i < additionalChildren.Length; i++)
            {
                IConstraint? additionalChild = additionalChildren[i];

                if (constraint is IConstraintContainerWithSuffix cws &&
                    cws.AdditionalChildImplicitForSuffix(additionalChild))
                {
                    continue;
                }

                additionalChild?.Accept(this);
                if (i + 1 < additionalChildren.Length || childrenLength > 0)
                {
                    NextConstraint();
                }
            }

            // print children
            for (int i = 0; i < childrenLength; i++)
            {
                IConstraint? child = children[i];

                if (constraint is IConstraintContainerWithSuffix cws && cws.ChildImplicitForSuffix(child))
                {
                    continue;
                }

                child?.Accept(this);
                if (i + 1 < childrenLength)
                {
                    NextConstraint();
                }
            }
        }

        Level--;
        _result.Append(NewLine());
        Indent(_indent, Level);
        _result.Append(QueryUtils.ArgClosing);
    }

    private void PrintLeaf(IConstraint constraint)
    {
        object?[] arguments = constraint.Arguments;

        // work out which argument positions actually get printed, so the separator can be decided by
        // "is another argument still coming" rather than by the raw index - the latter emitted a dangling
        // `priceBetween(?, )` whenever the last argument was null
        List<int> printable = [];
        for (int i = 0; i < arguments.Length; i++)
        {
            object? argument = arguments[i];
            if (argument is null)
            {
                continue;
            }
            if (constraint is IConstraintWithSuffix cws && cws.ArgumentImplicitForSuffix(argument))
            {
                continue;
            }
            printable.Add(i);
        }

        // A null sitting *before* a printed argument cannot be represented: evitaQL has no null literal and
        // no empty argument slot (`priceBetween(null,500)`, `priceBetween(100,)` and `priceBetween(100)` are
        // all rejected by the grammar). Silently omitting it would shift every later value one position to
        // the left - e.g. `priceBetween(null, 500)` would be sent as `priceBetween(500)`, filtering
        // "from 500" instead of "up to 500". Fail loudly instead of querying for the wrong thing.
        if (printable.Count > 0)
        {
            for (int i = 0; i < printable[^1]; i++)
            {
                if (arguments[i] is null)
                {
                    throw new EvitaInvalidUsageException(
                        $"Constraint `{constraint.Name}` has an undefined argument at position {i} followed by " +
                        "a defined one. evitaQL cannot express a missing argument in the middle of an argument " +
                        "list - supply the argument, or use a one-sided constraint instead."
                    );
                }
            }
        }

        for (int p = 0; p < printable.Count; p++)
        {
            object argument = arguments[printable[p]]!;
            if (_extractParameters)
            {
                _result.Append('?');
                _parameters?.AddLast(argument);
            }
            else
            {
                _result.Append(EvitaDataTypes.FormatValue(argument));
            }

            if (p + 1 < printable.Count)
            {
                _result.Append(", ");
            }
        }

        _result.Append(QueryUtils.ArgClosing);
    }

    public static string ToString(Query query, string? indent = null)
    {
        PrettyPrintingVisitor visitor = new(indent);
        visitor.Traverse(query);
        return visitor.GetResult();
    }

    public static string ToString(IConstraint constraint, string? indent = null)
    {
        PrettyPrintingVisitor visitor = new(indent);
        constraint.Accept(visitor);
        return visitor.GetResult();
    }

    public void Visit(IConstraint constraint)
    {
        if (FirstConstraint)
        {
            FirstConstraint = false;
        }
        else
        {
            _result.Append(NewLine());
        }

        Indent(_indent, Level);
        _result.Append(constraint.Name).Append(QueryUtils.ArgOpening);
        if (constraint.GetType().IsAssignableToGenericType(typeof(ConstraintContainer<>)))
        {
            switch (constraint)
            {
                case IConstraintContainer<IFilterConstraint> filterContainer:
                    PrintContainer(filterContainer);
                    break;
                case IConstraintContainer<IOrderConstraint> orderContainer:
                    PrintContainer(orderContainer);
                    break;
                case IConstraintContainer<IRequireConstraint> requireContainer:
                    PrintContainer(requireContainer);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
        else if (constraint is ConstraintLeaf leaf)
        {
            PrintLeaf(leaf);
        }
    }

    public record StringWithParameters(string Query, IList<object> Parameters);
}