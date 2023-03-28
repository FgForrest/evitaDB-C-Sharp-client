using System.Collections;
using Client.Exceptions;
using Client.Utils;

namespace Client.Queries;

public abstract class ConstraintContainer<T> : BaseConstraint, IConstraintContainer<T>, IEnumerable<T> where T : IConstraint
{
	private static readonly object[] NoArguments = Array.Empty<object>();
	private static readonly IConstraint[] NoAdditionalChildren = Array.Empty<IConstraint>();
	public T[] Children { get; }
	public IConstraint[] AdditionalChildren { get; }
	
	public bool Necessary => Children.Length > 1 || AdditionalChildren.Length > 1;
    public int GetChildrenCount() => Children.Length + AdditionalChildren.Length;
    public override bool Applicable => Children.Length > 0;

    public override string ToString()
    {
	    return Name +
	           QueryUtils.ArgOpening +
	           string.Join(",", 
		           Arguments.Select(ConvertToString)
			           .Concat(AdditionalChildren.Select(c => c.ToString()))
			           .Concat(Children.Select(c => c.ToString()))) +
	           QueryUtils.ArgClosing;
    }
    
    protected IConstraint? GetAdditionalChild(Type additionalChildType) {
	    if (GetType().IsSubclassOf(additionalChildType) ||
	        additionalChildType.IsSubclassOf(GetType())) {
		    throw new ArgumentException("Type of additional child must be different from type of children of the main container.");
	    }
	    return AdditionalChildren.FirstOrDefault(x => x.GetType() == additionalChildType);
    }

    private T[] ValidateAndFilterChildren(T?[] children)
    {
	    if (children.Length > 0 && GetType().IsAssignableFrom(children[0]?.Type))
	    {
		    throw new EvitaInvalidUsageException(
			    children[0]?.Type + " is not of expected type " + GetType()
		    );
	    }

	    // filter out null values, but avoid creating new array if not necessary
	    return (children.Any(x => x is null)
		    ?
		    //noinspection unchecked
		    children.Where(x => x is not null).ToArray()!
		    : children)!;
    }

    private IConstraint[] ValidateAndFilterAdditionalChildren(IConstraint?[] additionalChildren) {
		var additionalChildrenSize = additionalChildren.Length;
		if (additionalChildrenSize == 0) {
			return additionalChildren!;
		}

		// filter out null values, but avoid creating new array if not necessary
		IConstraint?[] newAdditionalChildren;
		if (additionalChildren.Any(x => x is null)) {
			newAdditionalChildren = additionalChildren.Where(x => x is not null).ToArray()!;
		} else {
			newAdditionalChildren = additionalChildren;
		}

		// validate additional child is not of same type as container and validate that there are distinct children
		for (int i = 0; i < additionalChildrenSize; i++) {
			Type? additionalChildType = additionalChildren[i]?.GetType();
			if (additionalChildType is null)
			{
				continue;
			}
			Assert.IsTrue(
				!GetType().IsSubclassOf(additionalChildType),
				"Type of additional child must be different from type of children of the main container."
			);

			for (int j = i + 1; j < additionalChildrenSize; j++) {
				Type? comparingAdditionalChildType = additionalChildren[j]?.GetType();
				if (comparingAdditionalChildType is null)
				{
					continue;
				}
				if (additionalChildType.IsSubclassOf(comparingAdditionalChildType) ||
					comparingAdditionalChildType.IsSubclassOf(additionalChildType)) {
					throw new EvitaInvalidUsageException(
						"There are multiple additional children of same type: " + additionalChildType + " and " + comparingAdditionalChildType
					);
				}
			}
		}

		return newAdditionalChildren!;
	}

    public IEnumerator<T> GetEnumerator()
    {
        return Children.ToList().GetEnumerator();
    }
    
    public abstract T GetCopyWithNewChildren(T[] children, IConstraint[] additionalChildren);
    
    protected ConstraintContainer(object[] arguments, T?[] children, params IConstraint[] additionalChildren) : base(arguments) {
	    Children = ValidateAndFilterChildren(children);
	    AdditionalChildren = ValidateAndFilterAdditionalChildren(additionalChildren);
    }

    protected ConstraintContainer(object[] arguments, params T?[] children) : this(arguments, children, NoAdditionalChildren) {
    }

    protected ConstraintContainer(params T?[] children) : this(NoArguments, children) {
    }

    protected ConstraintContainer(T[] children, params IConstraint[] additionalChildren) : this(NoArguments, children, additionalChildren) {
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}