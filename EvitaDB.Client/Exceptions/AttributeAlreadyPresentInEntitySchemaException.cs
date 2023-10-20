using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Exceptions;

public class AttributeAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public string? CatalogName { get; }
    public IAttributeSchema? ExistingAttributeSchema { get; }

    public ISortableAttributeCompoundSchema? ExistingAttributeCompoundSchema { get; }

    public AttributeAlreadyPresentInEntitySchemaException(
        IAttributeSchema existingAttribute,
        IAttributeSchema updatedAttribute,
        NamingConvention? convention,
        string conflictingName) : base(
        $"Attribute `{updatedAttribute.Name}` and existing attribute `{existingAttribute.Name}` produce the same name `{conflictingName}` in `{convention}` convention! Please choose different attribute name.")

    {
        CatalogName = null;
        ExistingAttributeSchema = existingAttribute;
        ExistingAttributeCompoundSchema = null;
    }

    public AttributeAlreadyPresentInEntitySchemaException(
        ISortableAttributeCompoundSchema existingAttributeCompound,
        IAttributeSchema updatedAttribute,
        NamingConvention? convention,
        string conflictingName) : base("Attribute `" + updatedAttribute.Name +
                                       "` and existing sortable attribute compound `" + existingAttributeCompound.Name +
                                       "` produce the same name `" + conflictingName + "`" +
                                       (convention == null ? "" : " in `" + convention + "` convention") +
                                       "! Please choose different attribute name.")
    {
        CatalogName = null;
        ExistingAttributeSchema = null;
        ExistingAttributeCompoundSchema = existingAttributeCompound;
    }

    public AttributeAlreadyPresentInEntitySchemaException(
        ISortableAttributeCompoundSchema existingAttributeCompound,
        ISortableAttributeCompoundSchema updatedAttributeCompound,
        NamingConvention? convention,
        string conflictingName) : base("Sortable attribute compound `" + updatedAttributeCompound.Name +
                                       "` and existing sortable attribute compound `" + existingAttributeCompound.Name +
                                       "` produce the same name `" + conflictingName + "`" +
                                       (convention == null ? "" : " in `" + convention + "` convention") +
                                       "! Please choose different attribute name.")
    {
        CatalogName = null;
        ExistingAttributeSchema = null;
        ExistingAttributeCompoundSchema = existingAttributeCompound;
    }

    public AttributeAlreadyPresentInEntitySchemaException(
        IAttributeSchema updatedAttribute,
        ISortableAttributeCompoundSchema existingAttributeCompound,
        NamingConvention? convention,
        string conflictingName) : base("Attribute `" + updatedAttribute.Name +
                                       "` and existing sortable attribute compound `" + existingAttributeCompound.Name +
                                       "` produce the same name `" + conflictingName + "`" +
                                       (convention == null ? "" : " in `" + convention + "` convention") +
                                       "! Please choose different attribute name.")
    {
        CatalogName = null;
        ExistingAttributeSchema = null;
        ExistingAttributeCompoundSchema = existingAttributeCompound;
    }
}