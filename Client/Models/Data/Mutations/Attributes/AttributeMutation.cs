using System.Globalization;
using Client.Exceptions;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;
using Client.Utils;

namespace Client.Models.Data.Mutations.Attributes;

public abstract class AttributeMutation : ILocalMutation
{
    public AttributeKey AttributeKey { get; }

    protected AttributeMutation(AttributeKey attributeKey)
    {
        AttributeKey = attributeKey;
    }

    public void VerifyOrEvolveSchema(
        CatalogSchema catalogSchema,
        IEntitySchemaBuilder entitySchemaBuilder,
        AttributeSchema? attributeSchema,
        object attributeValue,
        Action<CatalogSchema, IEntitySchemaBuilder> schemaEvolutionApplicator
    )
    {
        // when attribute definition is known execute first encounter formal verification
        if (attributeSchema != null)
        {
            // we need to unwrap array classes - we need to check base class for compatibility with Comparable
            Type? attributeClass;
            if (attributeValue is object[])
            {
                attributeClass = attributeValue.GetType().GetElementType();
            }
            else
            {
                attributeClass = attributeValue.GetType();
            }

            Assert.IsTrue(
                attributeSchema.GetType().IsInstanceOfType(attributeValue),
                () => new InvalidMutationException(
                    "Invalid type: `" + attributeValue.GetType() + "`! " +
                    "Attribute `" + AttributeKey.AttributeName + "` in schema `" + entitySchemaBuilder.Name +
                    "` was already stored as type " + attributeSchema.GetType() + ". " +
                    "All values of attribute `" + AttributeKey.AttributeName + "` must respect this data type!"
                )
            );
            if (attributeSchema.Sortable)
            {
                Assert.IsTrue(
                    typeof(IComparable).IsAssignableFrom(attributeClass),
                    () => new InvalidMutationException(
                        "Attribute `" + AttributeKey.AttributeName + "` in schema `" +
                        entitySchemaBuilder.Name +
                        "` is filterable and needs to implement " +
                        "Comparable interface, but it doesn't: `" + attributeValue.GetType() + "`!"
                    ));
            }

            if (attributeSchema.Localized)
            {
                Assert.IsTrue(
                    AttributeKey.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + AttributeKey.AttributeName + "` in schema `" +
                        entitySchemaBuilder.Name +
                        "` was already stored as localized value. " +
                        "All values of attribute `" + AttributeKey.AttributeName + "` must be localized now " +
                        "- use different attribute name for locale independent variant of attribute!"
                    )
                );
                CultureInfo? locale = AttributeKey.Locale;
                if (!entitySchemaBuilder.Locales.Contains(locale))
                {
                    if (entitySchemaBuilder.Allows(EvolutionMode.AddingLocales))
                    {
                        // evolve schema automatically
                        schemaEvolutionApplicator.Invoke(catalogSchema, entitySchemaBuilder);
                    }
                    else
                    {
                        throw new InvalidMutationException(
                            "Attribute `" + AttributeKey.AttributeName + "` in schema `" +
                            entitySchemaBuilder.Name + "` is localized to `" + locale +
                            "` which is not allowed by the schema" +
                            " (allowed are only: " +
                            string.Join(", ", entitySchemaBuilder.Locales.Select(x => x.ToString())) + "). " +
                            "You must first alter entity schema to be able to add this localized attribute to the entity!"
                        );
                    }
                }
            }
            else
            {
                Assert.IsTrue(
                    !AttributeKey.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + AttributeKey.AttributeName + "` in schema `" +
                        entitySchemaBuilder.Name +
                        "` was not stored as localized value. " +
                        "No values of attribute `" + AttributeKey.AttributeName + "` can be localized now " +
                        "- use different attribute name for localized variant of attribute!"
                    )
                );
            }
            // else check whether adding attributes on the fly is allowed
        }
        else if (entitySchemaBuilder.Allows(EvolutionMode.AddingAttributes))
        {
            // evolve schema automatically
            schemaEvolutionApplicator.Invoke(catalogSchema, entitySchemaBuilder);
        }

        else
        {
            throw new InvalidMutationException(
                "Unknown attribute `" + AttributeKey.AttributeName + "` in schema `" +
                entitySchemaBuilder.Name +
                "``! " +
                "You must first alter entity schema to be able to add this attribute to the entity!"
            );
        }
    }
}