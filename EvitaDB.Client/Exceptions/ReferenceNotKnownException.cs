namespace EvitaDB.Client.Exceptions;

/// <summary>
/// Exception is thrown when new reference is set on entity without name and cardinality specification that are expected
/// to be looked up in the reference schema. Unfortunately, reference schema of such name is not present in entity schema.
/// This would otherwise lead to new automatic reference schema setup, but this automatic setup requires to know
/// the reference name and cardinality in order to succeed.
/// </summary>
public class ReferenceNotKnownException : EvitaInvalidUsageException
{
    public string ReferenceName { get; }

    public ReferenceNotKnownException(string referenceName)
        : base("Reference schema for name `" + referenceName + "` doesn't exist." +
               " Use method that specifies target entity type and cardinality instead!")
    {
        ReferenceName = referenceName;
    }
}