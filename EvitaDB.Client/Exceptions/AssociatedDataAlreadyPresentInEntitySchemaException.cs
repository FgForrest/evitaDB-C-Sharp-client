using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Exceptions;

public class AssociatedDataAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public AssociatedDataSchema ExistingSchema { get; }

    public AssociatedDataAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage) : base(
        privateMessage, publicMessage)
    {
    }

    public AssociatedDataAlreadyPresentInEntitySchemaException(string publicMessage, Exception exception) : base(
        publicMessage, exception)
    {
    }

    public AssociatedDataAlreadyPresentInEntitySchemaException(string privateMessage, string publicMessage,
        Exception exception) : base(privateMessage, publicMessage, exception)
    {
    }

    public AssociatedDataAlreadyPresentInEntitySchemaException(string publicMessage) : base(publicMessage)
    {
    }

    public AssociatedDataAlreadyPresentInEntitySchemaException(
        AssociatedDataSchema existingAssociatedData,
        AssociatedDataSchema updatedAssociatedData,
        NamingConvention convention,
        string conflictingName
    ) : base($"Associated data `{updatedAssociatedData}` and existing associated data `{existingAssociatedData}` produce the same name `{conflictingName}` in `{convention}` convention! " +
             "Please choose different associated data name.")
    {
        ExistingSchema = existingAssociatedData;
    }
}