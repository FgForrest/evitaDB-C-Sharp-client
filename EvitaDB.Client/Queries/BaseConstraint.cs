using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries;

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

    protected string DefaultName => StringUtils.Uncapitalize(RemoveGenericsFromConstraintNameIfPresent(GetType()));

    protected BaseConstraint(object?[] arguments) : base()
    {
        _name = DefaultName;
        Arguments = arguments.Any(x => x != EvitaDataTypes.ToSupportedType(x))
            ? arguments.Select(EvitaDataTypes.ToSupportedType).ToArray()
            : arguments;
    }

    protected BaseConstraint(string? name, object?[] arguments) : base()
    {
        _name = name ?? DefaultName;
        Arguments = arguments.Any(x => x != EvitaDataTypes.ToSupportedType(x))
            ? arguments.Select(EvitaDataTypes.ToSupportedType).ToArray()
            : arguments;
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
        // NOTE: the closing bracket must be appended to the *joined* string. It used to sit inside the
        // string.Join argument, which concatenated it onto the IEnumerable and printed the LINQ iterator's
        // type name instead of the arguments.
        return Name + QueryUtils.ArgOpening +
               string.Join(
                   ",",
                   Arguments.Where(x =>
                       this is not IConstraintWithSuffix cws || !cws.ArgumentImplicitForSuffix(x!)
                   ).Select(ConvertToString)
               ) +
               QueryUtils.ArgClosing;
    }

    private string RemoveGenericsFromConstraintNameIfPresent(Type type)
    {
        string name = type.Name;
        int index = name.IndexOf('`');
        if (index > 0)
        {
            name = name.Remove(index); 
        }
        return name;
    } 
}