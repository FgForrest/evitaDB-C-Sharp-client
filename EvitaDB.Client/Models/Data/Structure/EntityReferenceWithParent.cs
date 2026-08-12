namespace EvitaDB.Client.Models.Data.Structure;

public record EntityReferenceWithParent(string Type, int? PrimaryKey, IEntityClassifierWithParent? ParentEntity) : IEntityReference, IEntityClassifierWithParent
{
    /// <summary>
    /// Declared on the record - not just inherited from <see cref="IEntityClassifier"/> - because this type is
    /// serialized by reflection, and a default interface member is not a member of the concrete type, so a
    /// serializer never sees it. Java's Jackson picks the getter up off the interface, which is why the
    /// documentation fixtures for `hierarchyContent` carry a `primaryKeyOrThrowException` field.
    /// </summary>
    public int PrimaryKeyOrThrowException =>
        PrimaryKey ?? throw new Exceptions.PrimaryKeyNotAssignedException(Type);
}