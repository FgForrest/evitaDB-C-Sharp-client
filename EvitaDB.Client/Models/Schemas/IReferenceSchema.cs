using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas;

public interface IReferenceSchema : INamedSchemaWithDeprecation, ISortableAttributeCompoundSchemaProvider
{
    Cardinality Cardinality { get; }
    string ReferencedEntityType { get; }
    string? ReferencedGroupType { get; }
    bool ReferencedEntityTypeManaged { get; }
    bool ReferencedGroupTypeManaged { get; }
    bool IsIndexed { get; }
    bool IsFaceted { get; }

    IDictionary<NamingConvention, string> GetEntityTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher);

    string GetReferencedEntityTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher);

    IDictionary<NamingConvention, string> GetGroupTypeNameVariants(
        Func<string, EntitySchema> entitySchemaFetcher);

    string GetReferencedGroupTypeNameVariants(
        NamingConvention namingConvention,
        Func<string, EntitySchema> entitySchemaFetcher);
}