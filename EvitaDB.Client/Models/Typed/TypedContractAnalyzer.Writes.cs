using System.Globalization;
using System.Reflection;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Write-member analysis: property setters, <c>SetXxx</c> methods and <c>[RemoveWhenExists]</c> removers.
/// <remarks>
/// Writing is deliberately narrower than reading. Attributes, associated data and the parent are
/// settable declaratively, because each is a single named value. Prices and references need several
/// coordinated values (price id, price list, currency, amounts; reference name plus target key), so they
/// are written through <c>ITypedEntityEditor.EntityBuilder</c> instead of through invented setter shapes.
/// </remarks>
/// </summary>
public static partial class TypedContractAnalyzer
{
    /// <summary>
    /// Describes every writing member of a contract: property setters, methods named <c>SetXxx</c>, and
    /// methods annotated with <see cref="RemoveWhenExistsAttribute"/> (or named <c>RemoveXxx</c>).
    /// </summary>
    private static IEnumerable<TypedMemberDescriptor> DescribeWriteMembers(Type contractType)
    {
        var types = contractType.IsInterface
            ? new[] { contractType }.Concat(contractType.GetInterfaces())
            : [contractType];

        var seen = new HashSet<MethodInfo>();

        foreach (var type in types)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            foreach (var property in type.GetProperties(flags))
            {
                if (property.SetMethod is { } setter && seen.Add(setter))
                {
                    var descriptor = DescribeWriteMember(
                        property, setter, property.PropertyType, setter.GetParameters(),
                        TypedMemberAccess.Write
                    );
                    if (descriptor is not null)
                    {
                        yield return descriptor;
                    }
                }
            }

            foreach (var method in type.GetMethods(flags))
            {
                if (method.IsSpecialName || method.DeclaringType == typeof(object) || !seen.Add(method))
                {
                    continue;
                }

                var parameters = method.GetParameters();
                // a remover may take a value - removing one reference of a collection needs its key
                var removes = method.GetCustomAttribute<RemoveWhenExistsAttribute>() is not null
                              || (method.Name.StartsWith("Remove", StringComparison.Ordinal)
                                  && method.Name.Length > "Remove".Length
                                  && parameters.Length <= 2);

                if (removes)
                {
                    var removedValueType = parameters
                        .FirstOrDefault(it => it.ParameterType != typeof(CultureInfo))?.ParameterType;
                    var descriptor = DescribeWriteMember(
                        method, method, removedValueType ?? typeof(object), parameters,
                        TypedMemberAccess.Remove
                    );
                    if (descriptor is not null)
                    {
                        yield return descriptor;
                    }

                    continue;
                }

                // a writing method is recognised by carrying a value parameter; the Set prefix is
                // conventional but not required, so an annotated method of any name works too
                var valueParameter = parameters.FirstOrDefault(it => it.ParameterType != typeof(CultureInfo));
                if (valueParameter is null || parameters.Length > 2)
                {
                    continue;
                }

                if (!WritesItsArgument(method))
                {
                    continue;
                }

                var writeDescriptor = DescribeWriteMember(
                    method, method, valueParameter.ParameterType, parameters, TypedMemberAccess.Write
                );
                if (writeDescriptor is not null)
                {
                    yield return writeDescriptor;
                }
            }
        }
    }

    /// <summary>
    /// Tells whether a method writes or removes data, and therefore belongs to the write analysis rather
    /// than the read analysis.
    /// </summary>
    internal static bool IsWriteShaped(MethodInfo method)
    {
        if (method.GetCustomAttribute<RemoveWhenExistsAttribute>() is not null)
        {
            return true;
        }

        var parameters = method.GetParameters();

        if (method.Name.StartsWith("Remove", StringComparison.Ordinal)
            && method.Name.Length > "Remove".Length
            && parameters.Length <= 2)
        {
            return true;
        }

        return parameters.Any(it => it.ParameterType != typeof(CultureInfo))
               && parameters.Length <= 2
               && WritesItsArgument(method);
    }

    /// <summary>
    /// Decides whether a value-taking method writes that value or uses it to narrow a read.
    /// <remarks>
    /// The <c>Set</c> prefix always means a write. Otherwise the <b>return type</b> decides: a method that
    /// hands nothing back (or hands back the contract for chaining) is writing, while one that returns data
    /// is reading - which is what lets <c>void AddCategory(int)</c> and
    /// <c>ICategoryLink? GetCategory(int)</c> carry the same annotation and mean opposite things.
    /// </remarks>
    /// </summary>
    private static bool WritesItsArgument(MethodInfo method)
    {
        if (method.Name.StartsWith("Set", StringComparison.Ordinal))
        {
            return true;
        }

        var handsNothingBack = method.ReturnType == typeof(void)
                               || (method.DeclaringType is not null
                                   && method.ReturnType.IsAssignableFrom(method.DeclaringType));

        return handsNothingBack && CarriesWritableAnnotation(method);
    }

    /// <summary>Any annotation that marks data this layer knows how to write.</summary>
    private static bool CarriesWritableAnnotation(MemberInfo member) =>
        CarriesDataAnnotation(member)
        || member.GetCustomAttribute<EvitaReferenceAttribute>() is not null
        || member.GetCustomAttribute<ReferenceRefAttribute>() is not null
        || member.GetCustomAttribute<EvitaPriceAttribute>() is not null;

    /// <summary>Tells whether a member reads a reference, in any of its annotated forms.</summary>
    internal static bool CarriesReferenceAnnotation(MemberInfo member) =>
        member.GetCustomAttribute<EvitaReferenceAttribute>() is not null
        || member.GetCustomAttribute<ReferenceRefAttribute>() is not null
        || member.GetCustomAttribute<ReflectedReferenceAttribute>() is not null;

    private static bool CarriesDataAnnotation(MemberInfo member) =>
        member.GetCustomAttribute<EvitaAttributeAttribute>() is not null
        || member.GetCustomAttribute<AttributeRefAttribute>() is not null
        || member.GetCustomAttribute<EvitaAssociatedDataAttribute>() is not null
        || member.GetCustomAttribute<AssociatedDataRefAttribute>() is not null
        || member.GetCustomAttribute<ParentEntityAttribute>() is not null;

    /// <summary>
    /// Classifies one writing member. Returns <c>null</c> when the member writes something this layer
    /// does not support declaratively, so that it can be reported precisely if it is ever called.
    /// </summary>
    private static TypedMemberDescriptor? DescribeWriteMember(
        MemberInfo member,
        MethodInfo accessor,
        Type valueType,
        ParameterInfo[] parameters,
        TypedMemberAccess access)
    {
        if (member.GetCustomAttribute<NotBoundAttribute>() is not null)
        {
            return null;
        }

        if (accessor.DeclaringType?.IsInterface == true && !accessor.IsAbstract)
        {
            return Unbound(
                accessor, member.Name, valueType, UnwrapShape(valueType), false,
                "the member declares a default interface implementation, which the runtime proxy " +
                "cannot delegate to"
            );
        }

        var localeIndex = Array.FindIndex(parameters, it => it.ParameterType == typeof(CultureInfo));
        var shape = UnwrapShape(valueType);

        // a member returning the contract itself is fluent and must hand the editor back
        var fluent = accessor.ReturnType != typeof(void)
                     && accessor.ReturnType.IsAssignableFrom(accessor.DeclaringType);

        var classified = ClassifyWritableData(member);
        var (kind, dataName, declaredExplicitly, attribute, associatedData) =
            (classified.Kind, classified.DataName, classified.DeclaredExplicitly, classified.Attribute,
                classified.AssociatedData);

        if (kind is null)
        {
            return null;
        }

        if (kind == TypedMemberKind.Unbound)
        {
            var reason = classified.Reflected
                ? "a reflected reference is maintained by the server as the inverse of a reference on the " +
                  "other entity - change that reference instead"
                : "the selling price is computed by the server from the query's price context and cannot " +
                  "be written";

            return new TypedMemberDescriptor(
                accessor, member.Name, TypedMemberKind.Unbound, null, false, valueType, shape.ItemType,
                shape.WrapInMaybe, shape.Collection, false, null, false, reason, null,
                ReferenceProjection.PrimaryKey, null, null, null, null, access, fluent, localeIndex
            );
        }

        return new TypedMemberDescriptor(
            accessor, member.Name, kind.Value, dataName, declaredExplicitly, valueType, shape.ItemType,
            shape.WrapInMaybe, shape.Collection, hasLocaleParameter: false, attribute, false, null,
            classified.Reference, ResolveReferenceProjection(shape.ItemType), associatedData,
            classified.Price, null, classified.Price?.PriceList, access, fluent, localeIndex
        );
    }

    /// <summary>
    /// Maps a writing member to the kind of data it writes. Only the kinds that carry a single named
    /// value are supported; anything else yields <c>null</c>.
    /// </summary>
    private readonly record struct WritableClassification(
        TypedMemberKind? Kind,
        string? DataName,
        bool DeclaredExplicitly,
        EvitaAttributeAttribute? Attribute = null,
        EvitaAssociatedDataAttribute? AssociatedData = null,
        EvitaReferenceAttribute? Reference = null,
        EvitaPriceAttribute? Price = null,
        bool Reflected = false);

    private static WritableClassification ClassifyWritableData(MemberInfo member)
    {
        if (member.GetCustomAttribute<ParentEntityAttribute>() is not null)
        {
            return new WritableClassification(TypedMemberKind.ParentEntity, null, true);
        }

        if (member.GetCustomAttribute<EvitaAttributeAttribute>() is { } attribute)
        {
            var name = string.IsNullOrWhiteSpace(attribute.Name) ? DeriveDataName(member) : attribute.Name;
            return new WritableClassification(TypedMemberKind.Attribute, name, true, Attribute: attribute);
        }

        if (member.GetCustomAttribute<AttributeRefAttribute>() is { } attributeRef)
        {
            var name = string.IsNullOrWhiteSpace(attributeRef.Name)
                ? DeriveDataName(member)
                : attributeRef.Name;
            return new WritableClassification(TypedMemberKind.Attribute, name, true);
        }

        if (member.GetCustomAttribute<EvitaAssociatedDataAttribute>() is { } associatedData)
        {
            var name = string.IsNullOrWhiteSpace(associatedData.Name)
                ? DeriveDataName(member)
                : associatedData.Name;
            return new WritableClassification(
                TypedMemberKind.AssociatedData, name, true, AssociatedData: associatedData);
        }

        if (member.GetCustomAttribute<AssociatedDataRefAttribute>() is { } associatedDataRef)
        {
            var name = string.IsNullOrWhiteSpace(associatedDataRef.Name)
                ? DeriveDataName(member)
                : associatedDataRef.Name;
            return new WritableClassification(TypedMemberKind.AssociatedData, name, true);
        }

        if (member.GetCustomAttribute<EvitaPriceAttribute>() is { } price)
        {
            return new WritableClassification(TypedMemberKind.Price, null, true, Price: price);
        }

        if (member.GetCustomAttribute<EvitaReferenceAttribute>() is { } reference)
        {
            var name = string.IsNullOrWhiteSpace(reference.Name) ? DeriveDataName(member) : reference.Name;
            return new WritableClassification(
                TypedMemberKind.Reference, name, true, Reference: reference);
        }

        if (member.GetCustomAttribute<ReferenceRefAttribute>() is { } referenceRef)
        {
            var name = string.IsNullOrWhiteSpace(referenceRef.Name)
                ? DeriveDataName(member)
                : referenceRef.Name;
            return new WritableClassification(TypedMemberKind.Reference, name, true);
        }

        // the selling price is computed by the server from the query context; there is nothing to write
        if (member.GetCustomAttribute<PriceForSaleAttribute>() is not null)
        {
            return new WritableClassification(TypedMemberKind.Unbound, null, false);
        }

        // a reflected reference mirrors a reference on another entity; that other side is what changes
        if (member.GetCustomAttribute<ReflectedReferenceAttribute>() is not null)
        {
            return new WritableClassification(TypedMemberKind.Unbound, null, false, Reflected: true);
        }

        // unannotated members are inferred as attributes, matching the read side's lenient branch
        var derived = DeriveDataName(member);
        return string.Equals(derived, "parent", StringComparison.OrdinalIgnoreCase)
            ? new WritableClassification(TypedMemberKind.ParentEntity, null, false)
            : new WritableClassification(TypedMemberKind.Attribute, derived, false);
    }
}
