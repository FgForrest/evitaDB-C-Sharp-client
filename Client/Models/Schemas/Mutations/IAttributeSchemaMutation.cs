using Client.Models.Schemas.Dtos;

namespace Client.Models.Schemas.Mutations;

public interface IAttributeSchemaMutation : ISchemaMutation
{
    string Name { get; }
    TS? Mutate<TS>(CatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema;
}