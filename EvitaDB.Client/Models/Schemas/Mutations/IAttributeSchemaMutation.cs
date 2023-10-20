namespace EvitaDB.Client.Models.Schemas.Mutations;

public interface IAttributeSchemaMutation : ISchemaMutation
{
    string Name { get; }
    TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema;
}