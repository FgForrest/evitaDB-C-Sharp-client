using Client.DataTypes;
using Client.Utils;

namespace Client.Queries;

public abstract class BaseConstraint : IConstraint
{
    private readonly string _name;

    public string Name => _name + (this is IConstraintWithSuffix cws
        ? cws.SuffixIfApplied is not null ? StringUtils.Capitalize(cws.SuffixIfApplied) : ""
        : "");

    public object?[] Arguments { get; }

    internal static string ConvertToString(object? value)
    {
        return value == null ? "<NULL>" : EvitaDataTypes.FormatValue(value);
    }

    protected string DefaultName => StringUtils.Uncapitalize(GetType().Name);

    protected BaseConstraint(params object?[] arguments) : base()
    {
        _name = DefaultName;
        /*Arguments = arguments.Any(x => x != EvitaDataTypes.ToSupportedType(x))
            ? arguments.Select(EvitaDataTypes.ToSupportedType).ToArray()
            : arguments;*/
        Arguments = arguments;
        //TODO:  Array problem
    }

    protected BaseConstraint(string? name, params object?[] arguments) : base()
    {
        _name = name ?? DefaultName;
        /*Arguments = arguments.Any(x => x != EvitaDataTypes.ToSupportedType(x))
            ? arguments.Select(EvitaDataTypes.ToSupportedType).ToArray()
            : arguments;*/
        Arguments = arguments;
        //TODO:  Array problem
    }

    public abstract Type Type { get; }
    public abstract bool Applicable { get; }
    public abstract void Accept(IConstraintVisitor visitor);

    protected bool IsArgumentsNonNull()
    {
        return Arguments.All(arg => arg != null);
    }

    public override string ToString()
    {
        return Name + QueryUtils.ArgOpening +
               string.Join(
                   ",",
                   Arguments.Where(x =>
                       this is not IConstraintWithSuffix cws || !cws.ArgumentImplicitForSuffix(x!)
                   ).Select(ConvertToString) +
                   QueryUtils.ArgClosing);
    }
}