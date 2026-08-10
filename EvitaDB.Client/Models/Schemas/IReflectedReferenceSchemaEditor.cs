using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Schemas;

public interface IReflectedReferenceSchemaEditor<TS> : IReferenceSchemaEditor<TS> where TS : IReflectedReferenceSchemaEditor<TS>
{
    public const string GroupTypeExceptionMessage = "Group type can be set only on original reference. It makes no sense to change it on reflected one.";

    TS WithDescriptionInherited();

    TS WithDeprecatedInherited();
    
    TS WithCardinalityInherited();
    
    TS WithAttributesInherited();
    
    TS WithoutAttributesInherited();

    TS WithAttributesInheritedExcept();

    TS IReferenceSchemaEditor<TS>.NonIndexed()
    {
        throw new InvalidSchemaException(
            "Reflected schema and original schema must always be indexed!"
        );
    }
    
    TS WithFacetedInherited();

    TS IReferenceSchemaEditor<TS>.WithGroupType(string groupType)
    {
        throw new NotSupportedException(GroupTypeExceptionMessage);
    }

    TS IReferenceSchemaEditor<TS>.WithGroupTypeRelatedToEntity(string groupType)
    {
        throw new NotSupportedException(GroupTypeExceptionMessage);
    }

    TS IReferenceSchemaEditor<TS>.WithoutGroupType()
    {
        throw new NotSupportedException(GroupTypeExceptionMessage);
    }
}
