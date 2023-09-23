using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Exceptions;

public class AssociatedDataAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public IAssociatedDataSchema ExistingSchema { get; }

    public AssociatedDataAlreadyPresentInEntitySchemaException(
        IAssociatedDataSchema existingAssociatedData,
        IAssociatedDataSchema updatedAssociatedData,
        NamingConvention convention,
        string conflictingName
    ) : base($"Associated data `{updatedAssociatedData}` and existing associated data `{existingAssociatedData}` produce the same name `{conflictingName}` in `{convention}` convention! " +
             "Please choose different associated data name.")
    {
        ExistingSchema = existingAssociatedData;
    }
}