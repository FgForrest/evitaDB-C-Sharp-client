using Client.Models.Data.Structure;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IReference : IAttributes, IDroppable
{
    public ReferenceKey ReferenceKey { get; }
    public GroupEntityReference? Group { get; }
    public SealedEntity? GroupEntity { get; }
    public SealedEntity? ReferencedEntity { get; }
    public IReferenceSchema? ReferenceSchema { get; }
    public Cardinality? ReferenceCardinality { get; }
    public string? ReferencedEntityType { get; }
    public string ReferenceName { get; }
    public int ReferencedPrimaryKey { get; }
}