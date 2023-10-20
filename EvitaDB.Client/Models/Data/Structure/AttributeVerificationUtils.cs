using System.Globalization;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Data.Structure;

public static class AttributeVerificationUtils
{
    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type? type,
        Func<string> locationResolver
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, attributeName, type, null, attributeSchema, locationResolver);
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type type,
        CultureInfo locale,
        Func<string> locationResolver
    )
    {
        IAttributeSchema? attributeSchema = entitySchema.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(entitySchema, attributeName, type, locale, attributeSchema, locationResolver);
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        string attributeName,
        Type? type,
        CultureInfo? locale,
        IAttributeSchema? attributeSchema,
        Func<string> locationResolver
    )
    {
        Assert.IsTrue(
            attributeSchema != null || entitySchema.Allows(EvolutionMode.AddingAttributes),
            () => new InvalidMutationException(
                "Attribute " + attributeName + " is not configured in entity `" + locationResolver.Invoke() +
                " schema and automatic evolution is not enabled for attributes!"
            )
        );
        if (attributeSchema != null)
        {
            if (type != null)
            {
                Assert.IsTrue(attributeSchema.Type.IsAssignableFrom(type),
                    () => new InvalidDataTypeMutationException(
                        "Attribute " + attributeName + " in entity " + locationResolver.Invoke() +
                        " schema accepts only type " + attributeSchema.Type.Name +
                        " - value type is different: " + type.Name + "!",
                        attributeSchema.Type, type
                    )
                );
                if (attributeSchema.Sortable)
                {
                    Assert.IsTrue(!type.IsArray,
                        () => new InvalidDataTypeMutationException(
                            "Attribute " + attributeName + " in entity " + locationResolver.Invoke() +
                            " schema is sortable and can't hold arrays of " + type.Name + "!",
                            attributeSchema.Type, type
                        )
                    );
                }
            }

            if (locale == null)
            {
                Assert.IsTrue(
                    !attributeSchema.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity " + locationResolver.Invoke() +
                        " schema is localized and doesn't accept non-localized attributes!"
                    )
                );
            }
            else
            {
                Assert.IsTrue(
                    attributeSchema.Localized,
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity " + locationResolver.Invoke() +
                        " schema is not localized and doesn't accept localized attributes!"
                    )
                );
                Assert.IsTrue(
                    entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                    () => new InvalidMutationException(
                        "Attribute `" + attributeName + "` in entity " + locationResolver.Invoke() +
                        " schema is localized, but schema doesn't support locale " + locale + "! " +
                        "Supported locales are: " +
                        string.Join(", ", entitySchema.Locales.Select(x=>x.TwoLetterISOLanguageName))
                    )
                );
            }
        }
        else if (locale != null)
        {
            // at least verify supported locale
            Assert.IsTrue(
                entitySchema.SupportsLocale(locale) || entitySchema.Allows(EvolutionMode.AddingLocales),
                () => new InvalidMutationException(
                    "Attribute `" + attributeName + "` in entity " + locationResolver.Invoke() +
                    " schema is localized, but schema doesn't support locale " + locale + "! " +
                    "Supported locales are: " +
                    string.Join(", ", entitySchema.Locales.Select(x=>x.TwoLetterISOLanguageName))
                )
            );
        }
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        string attributeName,
        Type? type,
        Func<string> locationSupplier
    )
    {
        IAttributeSchema? attributeSchema = referenceSchema?.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(
            entitySchema, attributeName, type, null, attributeSchema, locationSupplier
        );
    }

    internal static void VerifyAttributeIsInSchemaAndTypeMatch(
        IEntitySchema entitySchema,
        IReferenceSchema? referenceSchema,
        string attributeName,
        Type? type,
        CultureInfo locale,
        Func<string> locationSupplier
    )
    {
        IAttributeSchema? attributeSchema = referenceSchema?.GetAttribute(attributeName);
        VerifyAttributeIsInSchemaAndTypeMatch(
            entitySchema, attributeName, type, locale, attributeSchema, locationSupplier
        );
    }
}