namespace Client.DataTypes;

public enum ClassifierType
{
    Catalog,
    Entity,
    Attribute,
    AssociatedData,
    Reference,
    ReferenceAttribute
}

public class ClassifierTypeHelper
{
    public static string ToHumanReadableName(ClassifierType type)
    {
        return type switch
        {
            ClassifierType.Catalog => "Catalog",
            ClassifierType.Entity => "Entity",
            ClassifierType.Attribute => "Attribute",
            ClassifierType.AssociatedData => "Associated Data",
            ClassifierType.Reference => "Reference",
            ClassifierType.ReferenceAttribute => "Reference attribute",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}