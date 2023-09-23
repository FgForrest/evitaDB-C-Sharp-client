using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Exceptions;

public class ReferenceAlreadyPresentInEntitySchemaException : EvitaInvalidUsageException
{
    public IReferenceSchema ExistingSchema { get; }
    
    public ReferenceAlreadyPresentInEntitySchemaException(
        IReferenceSchema existingReferenceSchema,
        IReferenceSchema updatedReferenceSchema,
        NamingConvention convention,
        string conflictingName) : base(
        $"Reference schema `{updatedReferenceSchema}` and existing reference schema `{existingReferenceSchema}` produce the same name `{conflictingName}` in `{convention}` convention! " +
        "Please choose different reference schema name.")
    {
        ExistingSchema = existingReferenceSchema;
    }
}