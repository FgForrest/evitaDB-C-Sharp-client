using System.Runtime.Serialization;
using Client.DataTypes;
using Client.Utils;

namespace Client.Queries;

public abstract class BaseConstraint : IConstraint
{
    public string Name => StringUtils.Uncapitalize(GetType().Name);
    public object?[] Arguments { get; }

    internal static string ConvertToString(object? value)
    {
        return value == null ? "<NULL>" : EvitaDataTypes.FormatValue(value);
    }
    
    protected BaseConstraint(params object?[] arguments)
    {
        Arguments = arguments;
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
               string.Join(", ", Arguments.Select(ConvertToString)) + 
               QueryUtils.ArgClosing;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        throw new NotSupportedException();
    }
}