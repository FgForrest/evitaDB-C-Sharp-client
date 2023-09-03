using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Data.Mutations.Reference;

public class InsertReferenceMutation : ReferenceMutation
{
    private new ReferenceKey ReferenceKey { get; }
    public Cardinality? ReferenceCardinality { get; }
    public string? ReferencedEntityType { get; }


    public InsertReferenceMutation(ReferenceKey referenceKey, Cardinality? referenceCardinality,
        string referencedEntityType) : base(referenceKey)
    {
        ReferenceKey = referenceKey;
        ReferenceCardinality = referenceCardinality;
        ReferencedEntityType = referencedEntityType;
    }

    public override IReference MutateLocal(IEntitySchema entitySchema, IReference? existingValue)
    {
        if (existingValue == null)
        {
            return new Structure.Reference(
                entitySchema,
                1,
                ReferenceKey.ReferenceName,
                ReferenceKey.PrimaryKey,
                ReferencedEntityType,
                ReferenceCardinality,
                null,
                // attributes are inserted in separate mutation
                new List<AttributeValue>()
            );
        }

        if (existingValue.Dropped)
        {
            return new Structure.Reference(
                entitySchema,
                existingValue.Version + 1,
                ReferenceKey.ReferenceName,
                ReferenceKey.PrimaryKey,
                existingValue.ReferencedEntityType,
                existingValue.ReferenceCardinality,
                existingValue.Group is not null && !existingValue.Group.Dropped
                    ? new GroupEntityReference(existingValue.Group.ReferencedEntity,
                        existingValue.Group.PrimaryKey!.Value, existingValue.Group.Version + 1, true)
                    : null,
                // attributes are inserted in separate mutation
                new List<AttributeValue>()
            );
        }

        /* SHOULD NOT EVER HAPPEN */
        throw new InvalidMutationException(
            "This mutation cannot be used for updating reference."
        );
    }
}