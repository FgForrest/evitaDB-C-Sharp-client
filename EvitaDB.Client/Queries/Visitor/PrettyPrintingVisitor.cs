using System.Collections.Immutable;
using System.Text;
using EvitaDB.Client.DataTypes;
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
        if (query.Entities is not null)
        {
            query.Entities.Accept(this);
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

    public StringBuilder NextArgument() => _result.Append(",");

    public StringBuilder NextConstraint() => FirstConstraint ? _result : _result.Append(",");

    private void Indent(string? indent, int repeatCount)
    {
        if (indent != null)
        {
            _result.Append(string.Concat(Enumerable.Repeat(indent, repeatCount)));
        }
    }

    private void PrintContainer(IConstraintContainer<IConstraint> constraint)
    {
        if (constraint.Children.Length == 0 && constraint.AdditionalChildren.Length == 0)
        {
            PrintLeaf(constraint);
            return;
        }

        Level++;
        if (constraint.Applicable)
        {
            IConstraint[] children = constraint.Children;
            int childrenLength = children.Length;

            IConstraint[] additionalChildren = constraint.AdditionalChildren;
            int additionalChildrenLength = additionalChildren.Length;

            object?[] arguments = constraint.Arguments;
            int argumentsLength = arguments.Length;

            // print arguments
            for (int i = 0; i < argumentsLength; i++)
            {
                object? argument = arguments[i];
                if (constraint is IConstraintWithSuffix cws && cws.ArgumentImplicitForSuffix(argument))
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
                var additionalChild = additionalChildren[i];
                additionalChild.Accept(this);
                if (i + 1 < additionalChildren.Length || childrenLength > 0)
                {
                    NextConstraint();
                }
            }

            // print children
            for (int i = 0; i < childrenLength; i++)
            {
                var child = children[i];
                child.Accept(this);
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
        var arguments = constraint.Arguments;
        for (int i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            if (argument is null)
                continue;
            if (constraint is IConstraintWithSuffix cws && cws.ArgumentImplicitForSuffix(argument))
            {
                continue;
            }

            if (_extractParameters)
            {
                _result.Append('?');
                _parameters?.AddLast(argument);
            }
            else
            {
                _result.Append(EvitaDataTypes.FormatValue(argument));
            }

            if (i + 1 < arguments.Length)
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