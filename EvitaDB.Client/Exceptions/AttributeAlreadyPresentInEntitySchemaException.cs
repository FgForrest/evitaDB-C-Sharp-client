using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Exceptions;

public class AttributeAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public string? CatalogName { get; }
    public AttributeSchema? ExistingSchema { get; }

    public AttributeAlreadyPresentInEntitySchemaException(
        AttributeSchema existingAttribute,
        AttributeSchema updatedAttribute,
        NamingConvention convention,
        string conflictingName) : base(
        $"Attribute `{updatedAttribute.Name}` and existing attribute `{existingAttribute.Name}` produce the same name `{conflictingName}` in `{convention}` convention! Please choose different attribute name.")

    {
        CatalogName = null;
        ExistingSchema = existingAttribute;
    }

    public AttributeAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage) : base(
        privateMessage, publicMessage)
    {
    }

    public AttributeAlreadyPresentInEntitySchemaException(string publicMessage, Exception exception) : base(
        publicMessage, exception)
    {
    }

    public AttributeAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage,
        Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public AttributeAlreadyPresentInEntitySchemaException(string publicMessage) : base(publicMessage)
    {
    }
}