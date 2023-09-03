using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Mutations.Attributes;

public class SetAttributeSchemaGloballyUniqueMutation : IGlobalAttributeSchemaMutation, ILocalCatalogSchemaMutation
{
    public string Name { get; }
    public bool UniqueGlobally { get; }

    public SetAttributeSchemaGloballyUniqueMutation(string name, bool uniqueGlobally)
    {
        Name = name;
        UniqueGlobally = uniqueGlobally;
    }

    public TS? Mutate<TS>(ICatalogSchema? catalogSchema, TS? attributeSchema) where TS : IAttributeSchema
    {
        Assert.IsPremiseValid(attributeSchema != null, "Attribute schema is mandatory!");
        if (attributeSchema is GlobalAttributeSchema globalAttributeSchema)
        {
            return (TS) Convert.ChangeType(GlobalAttributeSchema.InternalBuild(
                Name,
                globalAttributeSchema.Description,
                globalAttributeSchema.DeprecationNotice,
                globalAttributeSchema.Unique,
                UniqueGlobally,
                globalAttributeSchema.Filterable,
                globalAttributeSchema.Sortable,
                globalAttributeSchema.Localized,
                globalAttributeSchema.Nullable,
                globalAttributeSchema.GetType(),
                globalAttributeSchema.DefaultValue,
                globalAttributeSchema.IndexedDecimalPlaces
            ), typeof(TS));
        }

        throw new EvitaInternalError("Unexpected input!");
    }

    public ICatalogSchema? Mutate(ICatalogSchema? catalogSchema)
    {
        Assert.IsPremiseValid(catalogSchema != null, "Catalog schema is mandatory!");
        IGlobalAttributeSchema existingAttributeSchema = catalogSchema?.GetAttribute(Name) ??
                                                         throw new InvalidSchemaMutationException("The attribute `" +
                                                             Name + "` is not defined in catalog `" +
                                                             catalogSchema?.Name + "` schema!");
        IGlobalAttributeSchema? updatedAttributeSchema = Mutate(catalogSchema, existingAttributeSchema);
        return (this as IGlobalAttributeSchemaMutation).ReplaceAttributeIfDifferent(
            catalogSchema, existingAttributeSchema, updatedAttributeSchema!
        );
    }
}