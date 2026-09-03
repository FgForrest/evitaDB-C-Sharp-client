using System.Reflection;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Data;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Reference, parent and nested-view readers of <see cref="BoundTypedContract"/>.
/// <remarks>
/// Nested views are bound from the schema carried by the nested data itself
/// (<see cref="IEntity.Schema"/> on a fetched referenced entity), so no catalog-wide schema index is
/// needed - the Java driver threads a <c>Map&lt;String, EntitySchemaContract&gt;</c> through the proxy
/// factory only because its decorators do not always carry one.
/// </remarks>
/// </summary>
public sealed partial class BoundTypedContract
{
    /// <summary>
    /// Builds a reader for a member bound to an entity reference. The member's projection decides what
    /// each reference materialises into, and the collection shape decides whether one or many are
    /// returned.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildReferenceReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchema schema)
    {
        var referenceName = member.DataName!;
        var referenceSchema = schema.GetReference(referenceName)
                              ?? schema.GetReferenceByName(referenceName, Utils.NamingConvention.CamelCase);

        if (referenceSchema is null && member.NameDeclaredExplicitly)
        {
            throw new EvitaInvalidUsageException(
                $"Reference `{referenceName}` referenced by " +
                $"`{descriptor.ContractType.Name}.{member.MemberName}` does not exist in schema of entity " +
                $"`{schema.Name}`."
            );
        }

        var resolvedName = referenceSchema?.Name ?? referenceName;
        var single = member.CollectionShape == TypedCollectionShape.Scalar;

        // a filter argument is resolved once, here - the reader only applies it
        var filter = ResolveReferenceFilter(descriptor, member, referenceSchema);

        return (source, args) =>
        {
            var entity = AsEntity(descriptor, member, source);

            if (!entity.ReferencesAvailable())
            {
                return Wrap(member, null, fetched: false, member.ItemType);
            }

            var references = entity.GetReferences(resolvedName)
                .Where(it => !it.Dropped)
                .ToList();

            if (filter is not null)
            {
                var argument = args is { Length: > 0 } ? args[0] : null;
                references = references.Where(it => filter(it, argument)).ToList();
            }

            if (single)
            {
                var first = references.Count > 0 ? references[0] : null;
                return Wrap(
                    member,
                    first is null
                        ? null
                        : ProjectReference(descriptor, member, first, referenceSchema, source as IEntityBuilder),
                    fetched: true,
                    member.ItemType
                );
            }

            var projected = references
                .Select(it => ProjectReference(
                    descriptor, member, it, referenceSchema, source as IEntityBuilder))
                .ToList();
            return Wrap(member, MaterializeCollection(member, projected), fetched: true, member.ItemType);
        };
    }

    /// <summary>
    /// Resolves how a reference member's single argument narrows the references it returns, from the
    /// argument's declared shape. Mirrors the Java driver, which accepts a referenced-entity id, a
    /// predicate, or a value compared against one of the reference's attributes.
    /// </summary>
    private static Func<IReference, object?, bool>? ResolveReferenceFilter(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IReferenceSchema? referenceSchema)
    {
        var parameters = member.Accessor.GetParameters();

        if (parameters.Length != 1)
        {
            return null;
        }

        var parameter = parameters[0];

        // an [AttributeRef] on the parameter compares it against that attribute of the reference
        var attributeRef = parameter.GetCustomAttribute<AttributeRefAttribute>();
        if (attributeRef is not null)
        {
            var attributeName = string.IsNullOrWhiteSpace(attributeRef.Name)
                ? TypedContractAnalyzer.ToCamelCase(parameter.Name ?? string.Empty)
                : attributeRef.Name;

            var attributeSchema = referenceSchema is null
                ? null
                : ResolveAttributeSchema(referenceSchema, attributeName);

            if (attributeSchema is null && referenceSchema is not null)
            {
                throw new EvitaInvalidUsageException(
                    $"Parameter `{parameter.Name}` of " +
                    $"`{descriptor.ContractType.Name}.{member.MemberName}` filters on reference attribute " +
                    $"`{attributeName}`, which reference `{referenceSchema.Name}` does not declare."
                );
            }

            var resolvedAttribute = attributeSchema?.Name ?? attributeName;
            return (reference, argument) => MatchesAttribute(reference, resolvedAttribute, argument);
        }

        // a predicate over the projected value filters whatever the member exposes
        if (typeof(Delegate).IsAssignableFrom(parameter.ParameterType))
        {
            return (reference, argument) => argument is Delegate predicate
                                            && predicate.DynamicInvoke(
                                                ProjectReference(descriptor, member, reference,
                                                    referenceSchema)) is true;
        }

        // anything integer-convertible is the referenced entity's primary key
        return (reference, argument) =>
            argument is not null
            && ConvertValue(argument, typeof(int), 0) is int key
            && reference.ReferencedPrimaryKey == key;
    }

    /// <summary>
    /// Compares one of a reference's attributes against the filter argument. An array-valued attribute
    /// matches when it contains the argument, and enums compare by name, as they are stored.
    /// </summary>
    private static bool MatchesAttribute(IReference reference, string attributeName, object? argument)
    {
        var stored = reference.GetAttribute(attributeName);

        if (stored is null || argument is null)
        {
            return stored is null && argument is null;
        }

        var expected = argument is Enum enumValue ? enumValue.ToString() : argument;

        if (stored is object[] storedArray)
        {
            return storedArray.Any(it => AreEquivalent(it, expected));
        }

        return AreEquivalent(stored, expected);
    }

    private static bool AreEquivalent(object? stored, object? expected)
    {
        if (Equals(stored, expected))
        {
            return true;
        }

        if (stored is null || expected is null)
        {
            return false;
        }

        // the stored value carries evitaDB's own type, which may differ from the argument's
        try
        {
            return Equals(ConvertValue(expected, stored.GetType(), 0), stored);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Builds a reader for a member bound to the parent of a hierarchical entity.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildParentReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        return (source, _) =>
        {
            var entity = AsEntity(descriptor, member, source);

            if (!entity.ParentAvailable())
            {
                return Wrap(member, null, fetched: false, member.ItemType);
            }

            // the parent primary key is available on its own (Parent), while the classifier chain
            // (ParentEntity) is populated only when the hierarchy was fetched with bodies or references
            var parentKey = entity.Parent;
            var parent = entity.ParentEntity;

            if (parentKey is null && parent is null)
            {
                return Wrap(member, null, fetched: true, member.ItemType);
            }

            object? value = member.ReferenceProjection switch
            {
                ReferenceProjection.PrimaryKey =>
                    EvitaDataTypes.ToTargetType(parentKey ?? parent?.PrimaryKey, member.ItemType),
                ReferenceProjection.EntityReference => parent as IEntityReference
                    ?? new EntityReference(parent?.Type ?? entity.Type, parentKey ?? parent?.PrimaryKey),
                ReferenceProjection.ReferencedEntity => parent is ISealedEntity parentBody
                    ? TypedEntityFactory.Create(member.ItemType, parentBody)
                    : throw new ContextMissingException(
                        $"Member `{descriptor.ContractType.Name}.{member.MemberName}` needs the parent " +
                        $"entity body, but only its primary key (#{parentKey ?? parent?.PrimaryKey}) was " +
                        $"fetched - add hierarchyContent() with an entityFetch() to the query."),
                _ => throw new EvitaInvalidUsageException(
                    $"Member `{descriptor.ContractType.Name}.{member.MemberName}` cannot expose the parent " +
                    $"as `{member.ItemType.Name}` - use an integer, an IEntityReference, or an annotated " +
                    $"entity contract.")
            };

            return Wrap(member, value, fetched: true, member.ItemType);
        };
    }

    /// <summary>
    /// Builds a reader for the referenced entity of a reference contract.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildReferencedEntityReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        return (source, _) =>
        {
            var reference = AsReference(descriptor, member, source);
            return Wrap(
                member,
                ProjectReferencedEntity(descriptor, member, reference),
                fetched: true,
                member.ItemType
            );
        };
    }

    /// <summary>
    /// Builds a reader for the group of a reference contract.
    /// </summary>
    private static Func<object, object?[]?, object?> BuildReferenceGroupReader(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member)
    {
        return (source, _) =>
        {
            var reference = AsReference(descriptor, member, source);
            var group = reference.Group;

            if (group is null || group.Dropped)
            {
                return Wrap(member, null, fetched: true, member.ItemType);
            }

            object? value = member.ReferenceProjection switch
            {
                ReferenceProjection.PrimaryKey =>
                    EvitaDataTypes.ToTargetType(group.ReferencedEntityPrimaryKey, member.ItemType),
                ReferenceProjection.EntityReference => group,
                ReferenceProjection.ReferencedEntity => reference.GroupEntity is { } groupBody
                    ? TypedEntityFactory.Create(member.ItemType, groupBody)
                    : throw new ContextMissingException(
                        $"Member `{descriptor.ContractType.Name}.{member.MemberName}` needs the reference " +
                        $"group body, but only its primary key was fetched - add an entityGroupFetch() to " +
                        $"the referenceContent() of the query."),
                _ => throw new EvitaInvalidUsageException(
                    $"Member `{descriptor.ContractType.Name}.{member.MemberName}` cannot expose the " +
                    $"reference group as `{member.ItemType.Name}`.")
            };

            return Wrap(member, value, fetched: true, member.ItemType);
        };
    }

    /// <summary>
    /// Turns one reference into the value the member declares.
    /// </summary>
    private static object? ProjectReference(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IReference reference,
        IReferenceSchema? referenceSchema,
        IEntityBuilder? owner = null) =>
        member.ReferenceProjection switch
        {
            ReferenceProjection.PrimaryKey =>
                EvitaDataTypes.ToTargetType(reference.ReferencedPrimaryKey, member.ItemType),
            ReferenceProjection.EntityReference => new EntityReference(
                reference.ReferencedEntityType ?? referenceSchema?.ReferencedEntityType ?? string.Empty,
                reference.ReferencedPrimaryKey),
            ReferenceProjection.ReferencedEntity =>
                ProjectReferencedEntity(descriptor, member, reference),
            ReferenceProjection.ReferenceContract =>
                TypedEntityFactory.CreateReferenceView(
                    member.ItemType, reference, referenceSchema, owner),
            _ => throw new EvitaInvalidUsageException(
                $"Unsupported projection for `{descriptor.ContractType.Name}.{member.MemberName}`.")
        };

    /// <summary>
    /// Materialises a nested typed view of a referenced entity, insisting that its body was fetched.
    /// </summary>
    private static object ProjectReferencedEntity(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IReference reference)
    {
        if (member.ReferenceProjection == ReferenceProjection.PrimaryKey)
        {
            return EvitaDataTypes.ToTargetType(reference.ReferencedPrimaryKey, member.ItemType)!;
        }

        if (member.ReferenceProjection == ReferenceProjection.EntityReference)
        {
            return new EntityReference(
                reference.ReferencedEntityType ?? string.Empty,
                reference.ReferencedPrimaryKey
            );
        }

        var body = reference.ReferencedEntity
                   ?? throw new ContextMissingException(
                       $"Member `{descriptor.ContractType.Name}.{member.MemberName}` needs the body of " +
                       $"referenced entity `{reference.ReferenceName}#{reference.ReferencedPrimaryKey}`, " +
                       $"which was not fetched - add an entityFetch() to the referenceContent() of the " +
                       $"query."
                   );

        return TypedEntityFactory.Create(member.ItemType, body);
    }

    /// <summary>
    /// Narrows the read source to an entity. <see cref="IEntity"/> rather than
    /// <see cref="ISealedEntity"/>, because an editor's builder is also an entity view - that is what
    /// lets a contract read back what it just wrote.
    /// </summary>
    private static IEntity AsEntity(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        object source) =>
        source as IEntity
        ?? throw new EvitaInvalidUsageException(
            $"Member `{descriptor.ContractType.Name}.{member.MemberName}` reads entity-level data, but the " +
            $"contract is bound to `{source.GetType().Name}`, which is not an entity."
        );

    private static IReference AsReference(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        object source) =>
        source as IReference
        ?? throw new EvitaInvalidUsageException(
            $"Member `{descriptor.ContractType.Name}.{member.MemberName}` reads reference-level data but " +
            $"the contract was bound to an entity."
        );
}
