namespace EvitaDB.Client.Models.Schemas;

/// <summary>
///  Enum specifies different modes for reference attributes inheritance in reflected schema.
/// </summary>
public enum AttributeInheritanceBehavior
{
    /// <summary>
    /// Inherit all attributes by default except those listed in the <see cref="IReflectedReferenceSchema.AttributeInheritanceFilter"/>
    /// </summary>
    InheritAllExcept,
    /// <summary>
    /// Do not inherit any attributes by default except those listed in the <see cref="IReflectedReferenceSchema.AttributeInheritanceFilter"/>
    /// </summary>
    InheritOnlySpecified
}
