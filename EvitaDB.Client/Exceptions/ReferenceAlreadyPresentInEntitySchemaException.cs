using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Exceptions;

public class ReferenceAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public ReferenceSchema ExistingSchema { get; }

    public ReferenceAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage) : base(
        privateMessage, publicMessage)
    {
    }

    public ReferenceAlreadyPresentInEntitySchemaException(string publicMessage, Exception exception) : base(
        publicMessage, exception)
    {
    }

    public ReferenceAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage,
        Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public ReferenceAlreadyPresentInEntitySchemaException(string publicMessage) : base(publicMessage)
    {
    }

    public ReferenceAlreadyPresentInEntitySchemaException(
        ReferenceSchema existingReferenceSchema,
        ReferenceSchema updatedReferenceSchema,
        NamingConvention convention,
        string conflictingName) : base(
        $"Reference schema `{updatedReferenceSchema}` and existing reference schema `{existingReferenceSchema}` produce the same name `{conflictingName}` in `{convention}` convention! " +
        "Please choose different reference schema name.")
    {
        ExistingSchema = existingReferenceSchema;
    }
}