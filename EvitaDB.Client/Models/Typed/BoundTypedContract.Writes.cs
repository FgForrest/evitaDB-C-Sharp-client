using System.Globalization;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Writer construction for editor contracts.
/// <remarks>
/// Writers mutate an <see cref="IEntityBuilder"/>; the sealed entity behind a typed view is never
/// touched, matching both the driver's own builder contract and the Java driver's proxy behaviour.
/// </remarks>
/// </summary>
public sealed partial class BoundTypedContract
{
    /// <summary>
    /// Builds a writer for one writing or removing member.
    /// </summary>
    private static Action<IEntityBuilder, object?[]?>? BuildWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema)
    {
        return member.Kind switch
        {
            TypedMemberKind.Attribute => BuildAttributeWriter(descriptor, member, schema),
            TypedMemberKind.AssociatedData => BuildAssociatedDataWriter(descriptor, member, schema),
            TypedMemberKind.ParentEntity => BuildParentWriter(descriptor, member),
            TypedMemberKind.Price => BuildPriceWriter(descriptor, member),
            TypedMemberKind.Reference => BuildReferenceWriter(descriptor, member, schema),
            _ => null
        };
    }

    /// <summary>
    /// Builds a writer for an attribute declared on a reference contract - the attributes that live on the
    /// reference itself rather than on either entity it joins.
    /// </summary>
    private static Action<IReferenceBuilder, object?[]?>? BuildReferenceAttributeWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IReferenceSchema? referenceSchema)
    {
        if (member.Kind != TypedMemberKind.Attribute)
        {
            return null;
        }

        var attributeSchema = referenceSchema is null
            ? null
            : ResolveAttributeSchema(referenceSchema, member.DataName!);

        if (attributeSchema is null && referenceSchema is not null && member.NameDeclaredExplicitly)
        {
            throw new EvitaInvalidUsageException(
                $"Attribute `{member.DataName}` written by " +
                $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist on reference " +
                $"`{referenceSchema.Name}`."
            );
        }

        var name = attributeSchema?.Name ?? member.DataName!;
        var decimalPlaces = attributeSchema?.IndexedDecimalPlaces ?? 0;
        var storedType = attributeSchema?.Type;

        return (reference, args) =>
        {
            var (value, locale) = SplitArguments(descriptor, member, args);

            if (member.Access == TypedMemberAccess.Remove)
            {
                if (locale is null)
                {
                    reference.RemoveAttribute(name);
                }
                else
                {
                    reference.RemoveAttribute(name, locale);
                }

                return;
            }

            var converted = CoerceForStorage(value, storedType, decimalPlaces);

            if (locale is null)
            {
                reference.SetAttribute(name, converted);
            }
            else
            {
                reference.SetAttribute(name, locale, converted);
            }
        };
    }

    private static Action<IEntityBuilder, object?[]?>? BuildAttributeWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema)
    {
        var attributeSchema = schema is null ? null : ResolveAttributeSchema(schema, member.DataName!);

        if (attributeSchema is null && schema is not null)
        {
            if (member.NameDeclaredExplicitly)
            {
                throw new EvitaInvalidUsageException(
                    $"Attribute `{member.DataName}` written by " +
                    $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of " +
                    $"entity `{schema.Name}`."
                );
            }

            return null;
        }

        var name = attributeSchema?.Name ?? member.DataName!;
        var decimalPlaces = attributeSchema?.IndexedDecimalPlaces ?? 0;
        var storedType = attributeSchema?.Type;

        return (builder, args) =>
        {
            var (value, locale) = SplitArguments(descriptor, member, args);

            if (member.Access == TypedMemberAccess.Remove)
            {
                if (locale is null)
                {
                    builder.RemoveAttribute(name);
                }
                else
                {
                    builder.RemoveAttribute(name, locale);
                }

                return;
            }

            // always the single-value overload: it stores the value's own type, so a string[] stays a
            // string[]. The object[] overload would store Object[] and fail the schema type check.
            var converted = CoerceForStorage(value, storedType, decimalPlaces);

            if (locale is null)
            {
                builder.SetAttribute(name, converted);
            }
            else
            {
                builder.SetAttribute(name, locale, converted);
            }
        };
    }

    private static Action<IEntityBuilder, object?[]?>? BuildAssociatedDataWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema? schema)
    {
        var dataSchema = schema is null
            ? null
            : schema.GetAssociatedData(member.DataName!)
              ?? schema.GetAssociatedDataByName(member.DataName!, Utils.NamingConvention.CamelCase);

        if (dataSchema is null && schema is not null)
        {
            if (member.NameDeclaredExplicitly)
            {
                throw new EvitaInvalidUsageException(
                    $"Associated data `{member.DataName}` written by " +
                    $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of " +
                    $"entity `{schema.Name}`."
                );
            }

            return null;
        }

        var name = dataSchema?.Name ?? member.DataName!;

        return (builder, args) =>
        {
            var (value, locale) = SplitArguments(descriptor, member, args);

            if (member.Access == TypedMemberAccess.Remove)
            {
                if (locale is null)
                {
                    builder.RemoveAssociatedData(name);
                }
                else
                {
                    builder.RemoveAssociatedData(name, locale);
                }

                return;
            }

            // a POCO is handed over as-is: the driver serializes anything outside its own data types into
            // a complex object
            if (locale is null)
            {
                builder.SetAssociatedData(name, value);
            }
            else
            {
                builder.SetAssociatedData(name, locale, value);
            }
        };
    }

    private static Action<IEntityBuilder, object?[]?> BuildParentWriter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        return (builder, args) =>
        {
            var (value, _) = SplitArguments(descriptor, member, args);

            if (member.Access == TypedMemberAccess.Remove || value is null)
            {
                builder.RemoveParent();
                return;
            }

            var parentKey = EvitaDataTypes.ToTargetType(value, typeof(int))
                            ?? throw new EvitaInvalidUsageException(
                                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` cannot set " +
                                $"the parent from `{value}`."
                            );

            builder.SetParent((int)parentKey);
        };
    }

    /// <summary>
    /// Splits a writer invocation into the value and the optional locale, honouring the position the
    /// contract declared them in.
    /// </summary>
    private static (object? Value, CultureInfo? Locale) SplitArguments(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        object?[]? args)
    {
        args ??= [];

        CultureInfo? locale = null;
        object? value = null;
        var valueSeen = false;

        for (var i = 0; i < args.Length; i++)
        {
            if (i == member.LocaleParameterIndex || args[i] is CultureInfo)
            {
                locale = args[i] as CultureInfo;
                continue;
            }

            value = args[i];
            valueSeen = true;
        }

        if (member.Access == TypedMemberAccess.Write && !valueSeen && args.Length > 0)
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` was invoked without a value."
            );
        }

        if (member.LocaleParameterIndex >= 0 && locale is null)
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` requires a non-null " +
                $"CultureInfo argument."
            );
        }

        return (value, locale);
    }

    /// <summary>
    /// Converts a written value into the form the schema stores. Collections become <c>object[]</c>,
    /// which is the shape the driver's array setters accept.
    /// </summary>
    private static object? CoerceForStorage(object? value, Type? storedType, int decimalPlaces)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string or byte[])
        {
            return value;
        }

        // evitaDB stores enums by name; the schema declares them as string
        if (value is Enum)
        {
            return value.ToString();
        }

        if (value is System.Collections.IEnumerable enumerable)
        {
            var items = enumerable.Cast<object?>().ToList();
            var elementType = storedType is { IsArray: true }
                ? storedType.GetElementType()!
                : items.FirstOrDefault(it => it is not null)?.GetType() ?? typeof(object);

            // a typed array, not object[] - the driver stores the array's own runtime type
            // a collection of enums is stored as a string array
            if (elementType.IsEnum)
            {
                elementType = typeof(string);
            }

            var typedArray = Array.CreateInstance(elementType, items.Count);
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i] is Enum enumItem ? enumItem.ToString() : items[i];
                typedArray.SetValue(EvitaDataTypes.ToTargetType(item, elementType, decimalPlaces), i);
            }

            return typedArray;
        }

        var scalarType = storedType is { IsArray: true } ? storedType.GetElementType() : storedType;

        return scalarType is null || scalarType.IsEnum
            ? value
            : EvitaDataTypes.ToTargetType(value, scalarType, decimalPlaces);
    }
}
