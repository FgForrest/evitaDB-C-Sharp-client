using EvitaDB.Client.Queries;
using EvitaDB.Client.Queries.Visitor;

namespace EvitaDB.Client.Utils;

public static class QueryUtils
{
    public const string ArgOpening = "(";
    public const string ArgClosing = ")";

    public static T? FindConstraint<T>(IConstraint constraint) where T : IConstraint
    {
        return FinderVisitor.FindConstraint<T>(constraint, typeof(T).IsInstanceOfType);
    }

    public static T? FindConstraint<T>(IConstraint constraint, Predicate<IConstraint> predicate) where T : IConstraint
    {
        return FinderVisitor.FindConstraint<T>(constraint, predicate);
    }

    public static TC? FindConstraint<TC, TS>(IConstraint constraint) where TC : IConstraint
    {
        return FinderVisitor.FindConstraint<TC>(constraint, typeof(TC).IsInstanceOfType,
            cnt => cnt != constraint && cnt is TS);
    }

    public static List<T> FindConstraints<T>(IConstraint constraint) where T : IConstraint
    {
        return FinderVisitor.FindConstraints<T>(constraint, typeof(T).IsInstanceOfType);
    }

    public static List<T> FindConstraints<T>(IConstraint constraint, Predicate<IConstraint> predicate)
        where T : IConstraint
    {
        return FinderVisitor.FindConstraints<T>(constraint, predicate);
    }

    public static List<TC> FindConstraints<TC, TS>(IConstraint constraint) where TC : IConstraint
    {
        return FinderVisitor.FindConstraints<TC>(constraint, typeof(TC).IsInstanceOfType,
            cnt => cnt != constraint && cnt.GetType().IsAssignableTo(typeof(TS)));
    }

    public static T? FindFilter<T>(Query query) where T : IFilterConstraint
    {
        return query.FilterBy is not null
            ? FinderVisitor.FindConstraint<T>(query.FilterBy, typeof(T).IsInstanceOfType)
            : default;
    }

    public static TF? FindFilter<TF, TS>(Query query) where TF : IFilterConstraint where TS : IRequireConstraint
    {
        return query.FilterBy is not null
            ? FinderVisitor.FindConstraint<TF>(query.FilterBy, typeof(TF).IsInstanceOfType, typeof(TS).IsInstanceOfType)
            : default;
    }

    public static List<T> FindFilters<T>(Query query) where T : IFilterConstraint
    {
        return query.FilterBy is not null
            ? FinderVisitor.FindConstraints<T>(query.FilterBy, typeof(T).IsInstanceOfType).ToList()
            : new List<T>();
    }

    public static T? FindOrder<T>(Query query) where T : IOrderConstraint
    {
        return query.OrderBy is not null
            ? FinderVisitor.FindConstraint<T>(query.OrderBy, typeof(T).IsInstanceOfType)
            : default;
    }

    public static T? FindRequire<T>(Query query) where T : IRequireConstraint
    {
        return query.Require is not null
            ? FinderVisitor.FindConstraint<T>(query.Require, typeof(T).IsInstanceOfType)
            : default;
    }

    public static TR? FindRequire<TR, TS>(Query query) where TR : IRequireConstraint where TS : IRequireConstraint
    {
        return query.Require is not null
            ? FinderVisitor.FindConstraint<TR>(query.Require, typeof(TR).IsInstanceOfType, typeof(TS).IsInstanceOfType)
            : default;
    }

    public static List<T> FindRequires<T>(Query query) where T : IRequireConstraint
    {
        return query.Require is not null
            ? FinderVisitor.FindConstraints<T>(query.Require, typeof(T).IsInstanceOfType).ToList()
            : new List<T>();
    }

    public static List<TR> FindRequires<TR, TS>(Query query) where TR : IRequireConstraint
    {
        return query.Require is not null
            ? FinderVisitor.FindConstraints<TR>(query.Require, typeof(TR).IsInstanceOfType, typeof(TS).IsInstanceOfType)
                .ToList()
            : new List<TR>();
    }

    public static bool ValueDiffers(object? thisValue, object? otherValue)
    {
        if (thisValue is object[] thisValueArray)
        {
            if (otherValue is not object[] otherValueArray)
            {
                return true;
            }

            if (thisValueArray.Length != otherValueArray.Length)
            {
                return true;
            }

            for (int i = 0; i < thisValueArray.Length; i++)
            {
                if (ValueDiffersInternal(thisValueArray[i], otherValueArray[i]))
                {
                    return true;
                }
            }

            return false;
        }

        return ValueDiffersInternal(thisValue, otherValue);
    }

    private static bool ValueDiffersInternal(object? thisValue, object? otherValue)
    {
        if (thisValue is IComparable comparable)
        {
            if (otherValue == null)
            {
                return true;
            }

            return !comparable.GetType().IsInstanceOfType(otherValue) || comparable.CompareTo(otherValue) != 0;
        }

        return !Equals(thisValue, otherValue);
    }
}