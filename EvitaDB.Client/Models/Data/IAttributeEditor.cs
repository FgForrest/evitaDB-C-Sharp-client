using System.Globalization;
using EvitaDB.Client.Models.Data.Mutations.Attributes;

namespace EvitaDB.Client.Models.Data;

public interface IAttributeEditor<out TW> : IAttributes where TW : IAttributeEditor<TW>
{
    TW RemoveAttribute(string attributeName);

    /**
	 * Stores value associated with the key.
	 * Setting null value effectively removes the attribute as if the {@link #removeAttribute(String)} was called.
	 *
	 * @return self (builder pattern)
	 */
    TW SetAttribute(string attributeName, object? attributeValue);

    /**
	 * Stores array of values associated with the key.
	 * Setting null value effectively removes the attribute as if the {@link #removeAttribute(String)} was called.
	 *
	 * @return self (builder pattern)
	 */
    TW SetAttribute(string attributeName, object[]? attributeValue);

    /**
	 * Removes locale specific value associated with the key or null when the attribute is missing.
	 *
	 * @return self (builder pattern)
	 */
    TW RemoveAttribute(string attributeName, CultureInfo locale);

    /**
	 * Stores locale specific value associated with the key.
	 * Setting null value effectively removes the attribute as if the {@link #removeAttribute(String, Locale)} was called.
	 *
	 * @return self (builder pattern)
	 */
    TW SetAttribute(string attributeName, CultureInfo locale, object? attributeValue);

    /**
	 * Stores array of locale specific values associated with the key.
	 * Setting null value effectively removes the attribute as if the {@link #removeAttribute(String, Locale)} was called.
	 *
	 * @return self (builder pattern)
	 */
    TW SetAttribute(string attributeName, CultureInfo locale, object[]? attributeValue);

    /**
	 * Alters attribute value in a way defined by the passed mutation implementation.
	 * There may never me multiple mutations for the same attribute - if you need to compose mutations you must wrap
	 * them into single one, that is then handed to the builder.
	 * <p>
	 * Remember each setAttribute produces a mutation itself - so you cannot set attribute and mutate it in the same
	 * round. The latter operation would overwrite the previously registered mutation.
	 *
	 * @return self (builder pattern)
	 */
    TW MutateAttribute(AttributeMutation mutation);
}