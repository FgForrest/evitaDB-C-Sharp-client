using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class SetAttributeSchemaGloballyUniqueMutation : IGlobalAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public GlobalAttributeUniquenessType UniqueGlobally { get; }
    public Operation Operation => Operation.Upsert;

    public SetAttributeSchemaGloballyUniqueMutation(string name, GlobalAttributeUniquenessType uniqueGlobally)
    {
        Name = name;
        UniqueGlobally = uniqueGlobally;
    }

    public TS Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema, Type schemaType) where TS : class, IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (AttributeSchema.InternalBuild(
                Name,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.UniquenessType,
                UniqueGlobally,
                globalAttributeSchema.Filterable(),
                globalAttributeSchema.Sortable(),
                globalAttributeSchema.Localized(),
                globalAttributeSchema.Nullable(),
                globalAttributeSchema.Representative,
                globalAttributeSchema.Type,
                globalAttributeSchema.DefaultValue,
                globalAttributeSchema.IndexedDecimalPlaces
            ) as TS)!;
        }

        throw new EvitaInternalError("Unexpected input!");
    }

    public ICatalogSchemaMutation.CatalogSchemaWithImpactOnEntitySchemas Mutate(ICatalogSchema? catalogSchema, IEntitySchemaProvider entitySchemaProvider)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema existingAttributeSchema = catalogSchema?.GetAttribute(Name) ??
                                                         throw new InvalidSchemaException("The attribute `" +
                                                             Name + "` is not defined in catalog `" +
                                                             catalogSchema?.Name + "` schema!");
        IGlobalAttributeSchema updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema, typeof(IGlobalAttributeSchema));
        return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            catalogSchema, existingAttributeSchema, updatedAttributeSchema, entitySchemaProvider, new UseGlobalAttributeSchemaMutation(Name)
        );
    }
}
