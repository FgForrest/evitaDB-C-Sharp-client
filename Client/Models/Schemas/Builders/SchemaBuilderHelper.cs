using Client.Exceptions;
using Client.Models.Schemas.Dtos;
using Client.Models.Schemas.Mutations;
using Client.Utils;

namespace Client.Models.Schemas.Builders;

public class SchemaBuilderHelper
{
    public static bool AddMutations(
        CatalogSchema currentCatalogSchema,
        List<ILocalCatalogSchemaMutation> existingMutations,
        ILocalCatalogSchemaMutation[] newMutations
    )
    {
        return AddMutations(
            typeof(ILocalCatalogSchemaMutation),
            (existingMutation, newMutation) => ((ICombinableCatalogSchemaMutation) newMutation)
                .CombineWith(currentCatalogSchema, existingMutation),
            existingMutations,
            newMutations
        );
    }

    public static bool AddMutations(
        CatalogSchema currentCatalogSchema,
        EntitySchema currentEntitySchema,
        List<IEntitySchemaMutation> existingMutations,
        params IEntitySchemaMutation[] newMutations
    )
    {
        return AddMutations(
            typeof(IEntitySchemaMutation),
            (existingMutation, newMutation) => ((ICombinableEntitySchemaMutation) newMutation)
                .CombineWith(currentCatalogSchema, currentEntitySchema, existingMutation),
            existingMutations,
            newMutations
        );
    }

    /**
	 * Method does the real heavy lifting of adding the mutation to the existing set of mutations.
	 * This method is quite slow - it has addition factorial complexity O(n!+). For each added mutation we need to
	 * compute combination result with all previous mutations.
	 *
	 * @param mutationType just for the Java generics sake, damn!
	 * @param combiner again a lambda that satisfies the damned Java generics - we just need to call combineWith method
	 * @param existingMutations set of existing mutations in the pipeline
	 * @param newMutations array of new mutations we want to add to the pipeline
	 * @return TRUE if the pipeline was modified and the cached schema needs te "recalculated"
	 * @param <T> :) having Java more clever, we wouldn't need this
	 */
    public static bool AddMutations<T>(
        Type mutationType,
        Func<T, T, MutationCombinationResult<T>?> combiner,
        List<T> existingMutations,
        params T[] newMutations
    ) where T : ISchemaMutation
    {
        bool schemaUpdated = false;
        // go through all new mutations
        foreach (T newMutation in newMutations)
        {
            if (newMutation is ICombinableEntitySchemaMutation || newMutation is ICombinableCatalogSchemaMutation)
            {
                List<MutationReplacement<T>> replacements = new();
                // for each - traverse all existing mutations
                T[] mutationsToExamine = (T[]) Array.CreateInstance(mutationType, 1);
                mutationsToExamine[0] = newMutation;

                do
                {
                    T[] mutationsToGoThrough = new T[mutationsToExamine.Length];
                    Array.Copy(mutationsToExamine, mutationsToGoThrough, mutationsToExamine.Length);
                    mutationsToExamine = null;
                    for (int i = 0; i < mutationsToGoThrough.Length; i++)
                    {
                        T? examinedMutation = mutationsToGoThrough[i];
                        var existingEnumerator = existingMutations.GetEnumerator();
                        int index = -1;
                        while (existingEnumerator.MoveNext())
                        {
                            index++;
                            T existingMutation = existingEnumerator.Current;

                            // and try to combine them together
                            MutationCombinationResult<T>? combinationResult = combiner.Invoke(
                                existingMutation, examinedMutation
                            );
                            if (combinationResult is not null)
                            {
                                // now check the result
                                if (combinationResult.Origin == null)
                                {
                                    // or we may find out that the new mutation makes previous mutation obsolete
                                    existingMutations.RemoveAt(index); //TODO: check if this is correct
                                    index--;
                                    schemaUpdated = true;
                                    examinedMutation = default;
                                }
                                else if (!combinationResult.Origin.Equals(existingMutation))
                                {
                                    // or we may find out that the new mutation makes previous mutation partially obsolete
                                    replacements.Add(new MutationReplacement<T>(index, combinationResult.Origin));
                                    examinedMutation = default;
                                }

                                // we may find out that the new mutation is not necessary, or partially not necessary
                                if (combinationResult.Current is not null && !combinationResult.Current!.Any())
                                {
                                    break;
                                }

                                if (combinationResult.Current.Length == 1 &&
                                    combinationResult.Current[0].Equals(examinedMutation))
                                {
                                    // continue with this mutation
                                }
                                else
                                {
                                    T[] copy = new T[mutationsToGoThrough.Length];
                                    Array.ConstrainedCopy(mutationsToGoThrough, i + 1, copy, 0,
                                        mutationsToGoThrough.Length);
                                    mutationsToExamine = copy.Concat(combinationResult.Current).ToArray();
                                    break;
                                }
                            }
                        }

                        // replace all partially obsolete existing mutations outside the loop to avoid ConcurrentModificationException
                        foreach (MutationReplacement<T> replacement in replacements)
                        {
                            existingMutations[replacement.Index] = replacement.ReplaceMutation;
                            schemaUpdated = true;
                        }

                        // clear applied replacements
                        replacements.Clear();
                        // and if the new mutation still applies, append it to the end
                        if (examinedMutation != null)
                        {
                            existingMutations.Add(examinedMutation);
                            schemaUpdated = true;
                        }
                    }
                } while (mutationsToExamine != null);
            }
            else
            {
                existingMutations.Add(newMutation);
                schemaUpdated = true;
            }
        }

        return schemaUpdated;
    }

    /**
     * Method checks that the attribute schema has all its possible variants for all {@link io.evitadb.utils.NamingConvention}
     * unique among all other attribute schemas on the same level of the parent schema.
     */
    public static void CheckNamesAreUniqueInAllNamingConventions(ICollection<AttributeSchema> values, AttributeSchema attributeSchema)
    {
        /*
        values
            .Where(it => !Equals(it.Name, attributeSchema.Name))
            .SelectMany(it=>it.NameVariants
                .Where(
                    nameVariant=>nameVariant.Value.Equals(attributeSchema.GetNameVariant(nameVariant.Key)))
                .Select(nameVariant=> new EntitySchemaBuilder.AttributeNamingConventionConflict(it, nameVariant.Key,
                    nameVariant.Value))
            )
            .ToList()
            .ForEach(conflict => throw new AttributeAlreadyPresentInEntitySchemaException(
                conflict.ConflictingSchema, attributeSchema,
                conflict.Convention, conflict.ConflictingName
            ));
            */
    }

    /**
 * Method checks whether the sortable attribute is not an array type. It's not possible to sort entities that would
 * provide multiple values to sort by.
 */
    public static void CheckSortableTraits(string attributeName, AttributeSchema attributeSchema)
    {
        if (attributeSchema.Sortable)
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
}