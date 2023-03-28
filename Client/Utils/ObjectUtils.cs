namespace Client.Queries;

public class QueryUtils
{
    public const string ArgOpening = "(";
    public const string ArgClosing = ")";
    
    public static bool ValueDiffers(object? thisValue, object? otherValue) {
        if (thisValue is object[] thisValueArray) {
            if (otherValue is not object[] otherValueArray) {
                return true;
            }
            if (thisValueArray.Length != otherValueArray.Length) {
                return true;
            }
            for (int i = 0; i < thisValueArray.Length; i++) {
                if (ValueDiffersInternal(thisValueArray[i], otherValueArray[i])) {
                    return true;
                }
            }
            return false;
        } 
        return ValueDiffersInternal(thisValue, otherValue);
    }
    
    private static bool ValueDiffersInternal(object? thisValue, object? otherValue) {
        if (thisValue is IComparable comparable) {
            if (otherValue == null) {
                return true;
            } 
            return !comparable.GetType().IsInstanceOfType(otherValue) || comparable.CompareTo(otherValue) != 0;
        } 
        return !Equals(thisValue, otherValue);
    }
}