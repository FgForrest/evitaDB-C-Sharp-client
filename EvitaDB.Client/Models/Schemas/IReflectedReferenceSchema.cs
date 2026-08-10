namespace EvitaDB.Client.Models.Schemas;

/// <summary>
/// <para>
/// Schema allows to set up bi-directional relation between two entities. By default, all properties and reference
/// attributes of the reflected reference are inherited from the target reference. This can be changed by setting
/// appropriate properties on the reflected reference schema definition, however. You'd probably want to alter
/// the original <see cref="IReferenceSchema.Cardinality"/> as it may not be the same for the reflected reference.
/// </para>
/// <para>
/// Reflected reference behaves the same as normal reference - it can be created, updated or deleted both from the source
/// and target entity. It always modifies data in the source entity (entity that maintains the primary reference), but
/// updates all the involved indexes so that the data remains consistent from both sides.
/// </para>
/// <para>
/// Note: the original reference <see cref="IReferenceSchema.ReferencedEntityType"/> must target the entity where
/// the reflected reference is defined, also the <see cref="IReferenceSchema.ReferencedEntityTypeManaged"/> must be
/// set to true.
/// </para>
/// </summary>
public interface IReflectedReferenceSchema : IReferenceSchema
{
    string ReflectedReferenceName { get; }
    bool DescriptionInherited { get; }
    bool DeprecatedInherited { get; }
    bool CardinalityInherited { get; }
    bool FacetedInherited { get; }
    AttributeInheritanceBehavior AttributeInheritanceBehavior { get; }
    string[] AttributeInheritanceFilter { get; }
    bool ReflectedReferenceAvailable { get; }
}
