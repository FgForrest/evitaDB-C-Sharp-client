using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas.Dtos;
using EvitaDB.Client.Models.Schemas.Mutations;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Schemas.Builders;

public static class SchemaBuilderHelper
{
    public static bool AddMutations(
        ICatalogSchema currentCatalogSchema,
        IList<ILocalCatalogSchemaMutation> existingMutations,
        params ILocalCatalogSchemaMutation[] newMutations
    )
    {
        int existingMutationsCount = existingMutations.Count;
        foreach (ILocalCatalogSchemaMutation localCatalogSchemaMutation in newMutations)
        {
            existingMutations.Add(localCatalogSchemaMutation);
        }
        return existingMutationsCount < existingMutations.Count;
    }

    public static bool AddMutations(
        ICatalogSchema currentCatalogSchema,
        IEntitySchema currentEntitySchema,
        IList<IEntitySchemaMutation> existingMutations,
        params IEntitySchemaMutation[] newMutations
    )
    {
        int existingMutationsCount = existingMutations.Count;
        foreach (IEntitySchemaMutation entitySchemaMutation in newMutations)
        {
            existingMutations.Add(entitySchemaMutation);
        }
        return existingMutationsCount < existingMutations.Count;
    }

    /**
     * Method checks that the attribute schema has all its possible variants for all {@link io.evitadb.utils.NamingConvention}
     * unique among all other attribute schemas on the same level of the parent schema.
     */
    public static void CheckNamesAreUniqueInAllNamingConventions<T>(
        ICollection<T> attributeSchemas,
        ICollection<SortableAttributeCompoundSchema> compoundSchemas,
        INamedSchema newSchema) where T : IAttributeSchema
    {
        attributeSchemas
            .Where(it => !Equals(it.Name, newSchema.Name) && newSchema is IAttributeSchema)
            .SelectMany(it => it.NameVariants
                .Where(
                    nameVariant => nameVariant.Value!.Equals(newSchema.GetNameVariant(nameVariant.Key)))
                .Select(nameVariant => new InternalEntitySchemaBuilder.AttributeNamingConventionConflict(it, null,
                    nameVariant.Key,
                    nameVariant.Value!))
            )
            .Concat(
                compoundSchemas
                    .Where(it => !(Equals(it.Name, newSchema.Name) && newSchema is ISortableAttributeCompoundSchema))
                    .SelectMany(it => it.NameVariants
                        .Where(nameVariant => nameVariant.Value!.Equals(newSchema.GetNameVariant(nameVariant.Key)))
                        .Select(
                            nameVariant => new InternalEntitySchemaBuilder.AttributeNamingConventionConflict(
                                null, it, nameVariant.Key, nameVariant.Value!
                            )
                        )
                    )
            )
            .ToList()
            .ForEach(conflict =>
            {
                if (newSchema is IAttributeSchema newAttributeSchema)
                {
                    if (conflict.ConflictingAttributeSchema == null)
                    {
                        throw new AttributeAlreadyPresentInEntitySchemaException(
                            conflict.ConflictingCompoundSchema!,
                            newAttributeSchema,
                            conflict.Convention, conflict.ConflictingName
                        );
                    }

                    throw new AttributeAlreadyPresentInEntitySchemaException(
                        conflict.ConflictingAttributeSchema,
                        newAttributeSchema,
                        conflict.Convention, conflict.ConflictingName
                    );
                }

                if (newSchema is ISortableAttributeCompoundSchema newCompoundSchema)
                {
                    if (conflict.ConflictingAttributeSchema == null)
                    {
                        throw new AttributeAlreadyPresentInEntitySchemaException(
                            conflict.ConflictingCompoundSchema!,
                            newCompoundSchema,
                            conflict.Convention, conflict.ConflictingName
                        );
                    }

                    throw new AttributeAlreadyPresentInEntitySchemaException(
                        conflict.ConflictingAttributeSchema,
                        newCompoundSchema,
                        conflict.Convention, conflict.ConflictingName
                    );
                }

                throw new("Should not be possible");
            });
    }

    /**
 * Method checks whether the sortable attribute is not an array type. It's not possible to sort entities that would
 * provide multiple values to sort by.
 */
    public static void CheckSortableTraits(string attributeName, IAttributeSchema attributeSchema)
    {
        if (attributeSchema.Sortable())
        {
            Assert.IsTrue(
                !attributeSchema.Type.IsArray,
                () => new InvalidSchemaMutationException(
                    "Attribute " + attributeName + " is marked as sortable and thus cannot be the array of " +
                    attributeSchema.Type + "!"
                )
            );
        }
    }

    public static void CheckSortableTraits<T> (
        string compoundSchemaName,
        ISortableAttributeCompoundSchema compoundSchemaContract,
        IDictionary<string, T> attributeSchemas
    ) where T : IAttributeSchema
    {
        foreach (AttributeElement attributeElement in compoundSchemaContract.AttributeElements)
        {
            IAttributeSchema? attributeSchema =
                attributeSchemas.TryGetValue(attributeElement.AttributeName, out T? result)
                    ? result
                    : null;
            if (attributeSchema == null)
            {
                throw new SortableAttributeCompoundSchemaException(
                    "Attribute `" + attributeElement.AttributeName + "` the sortable attribute compound" +
                    " `" + compoundSchemaName + "` consists of doesn't exist!",
                    compoundSchemaContract
                );
            }

            Assert.IsTrue(
                !attributeSchema.Type.IsArray,
                () => new InvalidSchemaMutationException(
                    "Attribute `" + attributeElement.AttributeName + "` the sortable attribute compound" +
                    " `" + compoundSchemaName + "` consists of cannot be the array of " +
                    attributeSchema.Type + "!"
                )
            );
        }
    }

    /**
     * Method checks whether there is any sortable attribute compound using attribute with particular name and
     * throws {@link SortableAttributeCompoundSchemaException} if it does.
     *
     * @throws SortableAttributeCompoundSchemaException when there is sortable attribute compound using attribute
     */
    public static void CheckSortableAttributeCompoundsWithoutAttribute(
        string attributeName,
        ICollection<SortableAttributeCompoundSchema> sortableAttributeCompounds
    )
    {
        SortableAttributeCompoundSchema? conflictingCompounds = sortableAttributeCompounds
            .FirstOrDefault(it => it.AttributeElements
                .Any(attr => attributeName.Equals(attr.AttributeName)));
        Assert.IsTrue(
            conflictingCompounds is null,
            () => new SortableAttributeCompoundSchemaException(
                "The attribute `" + attributeName + "` cannot be removed because there is sortable attribute compound" +
                " relying on it! Please, remove the compound first. ",
                conflictingCompounds!
            )
        );
    }
}
