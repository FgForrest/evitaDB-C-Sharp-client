namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// This `between` is query that compares value of the attribute with name passed in first argument with the value passed
/// in the second argument and value passed in third argument. First argument must be <see cref="string"/>, second and third
/// argument may be any of supported comparable types.
/// 
/// Type of the attribute value and second argument must be convertible one to another otherwise `between` function
/// returns false.
/// 
/// Function returns true if value in a filterable attribute of such a name is greater than or equal to value in second argument
/// and lesser than or equal to value in third argument.
/// 
/// Example:
/// <code>
/// between("age", 20, 25)
/// </code>
/// Function supports attribute arrays and when attribute is of array type `between` returns true if *any of attribute* values
/// is between the passed interval the value in the query. If we have the attribute `amount` with value `[1, 9]` all
/// these constraints will match:
/// <code>
/// between("amount", 0, 50)
/// between("amount", 0, 5)
/// between("amount", 8, 10)
/// </code>
/// If attribute is of `Range` type `between` query behaves like overlap - it returns true if examined range and
/// any of the attribute ranges (see previous paragraph about array types) share anything in common. All the following
/// constraints return true when we have the attribute `validity` with following `NumberRange` values: `[[2,5],[8,10]]`:
/// <code>
/// between("validity", 0, 3)
/// between("validity", 0, 100)
/// between("validity", 9, 10)
/// </code>
/// ... but these constraints will return false:
/// <code>
/// between("validity", 11, 15)
/// between("validity", 0, 1)
/// between("validity", 6, 7)
/// </code>
/// </summary>
public class AttributeBetween<T> : AbstractAttributeFilterConstraintLeaf where T : IComparable
{
    private AttributeBetween(params object?[] arguments) : base(arguments)
    {
    }

    public AttributeBetween(string attributeName, T? from, T? to) : base(attributeName, from, to)
    {
    }

    public T? From => (T?) Arguments[1];

    public T? To => (T?) Arguments[2];

    public override bool Applicable =>
        Arguments.Length == 3 && (From is not null || To is not null);
}
