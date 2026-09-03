using System.Globalization;
using System.Reflection;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Typed;

/// <summary>
/// Derives an entity schema from an annotated contract type, by applying the contract's declarations to
/// a fluent <see cref="IEntitySchemaBuilder"/>.
/// <remarks>
/// <para>
/// Counterpart of the Java driver's <c>ClassSchemaAnalyzer</c>. Like it, this class only *describes*
/// the schema - it produces a builder whose mutations the caller applies, so nothing reaches the server
/// until the caller decides to send it.
/// </para>
/// <para>
/// <b>Deliberate divergence from the Java driver:</b> reconciliation is <b>additive</b>. Java treats the
/// annotated class as the sole source of truth and actively resets schema flags that sit at their
/// annotation default, so re-analysing a bare <c>[EvitaAttribute] string Name</c> clears a
/// previously-set <c>filterable</c> flag. That behaviour turns a partial contract into silent schema
/// demotion, so here a contract only ever adds or widens. Removal stays an explicit act through the
/// schema API.
/// </para>
/// </remarks>
/// </summary>
public static class ModelSchemaAnalyzer
{
    /// <summary>
    /// Applies the contract's schema declarations to the given builder.
    /// </summary>
    /// <param name="descriptor">the analyzed contract</param>
    /// <param name="builder">builder for the entity collection the contract maps to</param>
    /// <returns>the same builder, for chaining</returns>
    /// <exception cref="EvitaInvalidUsageException">
    /// when the contract is read-only (annotated only with <see cref="EvitaEntityRefAttribute"/>)
    /// </exception>
    public static IEntitySchemaBuilder Apply(TypedContractDescriptor descriptor, IEntitySchemaBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(builder);

        if (!descriptor.DrivesSchema)
        {
            throw new EvitaInvalidUsageException(
                $"Contract `{descriptor.ContractType.Name}` is annotated with [EvitaEntityRef] and is " +
                $"read-only - annotate it with [EvitaEntity] to derive a schema from it."
            );
        }

        var declaration = descriptor.EntityDeclaration!;

        if (!string.IsNullOrWhiteSpace(declaration.Description))
        {
            builder.WithDescription(declaration.Description);
        }

        if (!string.IsNullOrWhiteSpace(declaration.DeprecationNotice))
        {
            builder.Deprecated(declaration.DeprecationNotice);
        }

        if (declaration.AllowedLocales is { Length: > 0 })
        {
            builder.WithLocale(
                declaration.AllowedLocales.Select(CultureInfo.GetCultureInfo).ToArray()
            );
        }

        ApplyPrimaryKeyGeneration(descriptor, declaration, builder);

        if (descriptor.Members.Any(it => it.Kind == TypedMemberKind.ParentEntity
                                         && it.Access == TypedMemberAccess.Read))
        {
            builder.WithHierarchy();
        }

        foreach (var member in descriptor.Members)
        {
            // a setter and its getter describe the same data, so only read members are considered here -
            // otherwise every property with both would declare its schema twice
            if (member.Access != TypedMemberAccess.Read)
            {
                continue;
            }

            // only members that declare schema intent contribute; the *Ref shortcuts and inferred
            // members read existing data without claiming to define it
            switch (member.Kind)
            {
                case TypedMemberKind.Attribute when member.AttributeDeclaration is not null:
                    ApplyAttribute(descriptor, member, builder);
                    break;
                case TypedMemberKind.AssociatedData when member.AssociatedDataDeclaration is not null:
                    ApplyAssociatedData(member, builder);
                    break;
                case TypedMemberKind.Price when member.PriceDeclaration is not null:
                    ApplyPrice(member.PriceDeclaration.IndexedPricePlaces,
                        member.PriceDeclaration.AllowedCurrencies, builder);
                    break;
                case TypedMemberKind.PriceForSale when member.PriceForSaleDeclaration is not null:
                    ApplyPrice(member.PriceForSaleDeclaration.IndexedPricePlaces,
                        member.PriceForSaleDeclaration.AllowedCurrencies, builder);
                    break;
                case TypedMemberKind.Reference when member.ReflectedDeclaration is not null:
                    ApplyReflectedReference(descriptor, member, builder);
                    break;
                case TypedMemberKind.Reference when member.ReferenceDeclaration is not null:
                    ApplyReference(descriptor, member, builder);
                    break;
            }
        }

        return builder;
    }

    /// <summary>
    /// Settles whether the collection generates its primary keys, mirroring the Java driver's
    /// <c>ClassSchemaAnalyzer.definePrimaryKey</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three properties, all of which the first port lacked. <b>Both directions:</b> a contract that says
    /// <c>[PrimaryKey(AutoGenerate = false)]</c> actively emits
    /// <see cref="IEntitySchemaEditor{TS}.WithoutGeneratedPrimaryKey"/>, so a collection that acquired the
    /// flag once can be talked out of it. <b>State guard:</b> nothing is emitted when the schema already
    /// agrees, so re-running the definition against an existing catalog is a no-op rather than a mutation
    /// storm. <b>One winner:</b> a member annotated with <see cref="PrimaryKeyAttribute"/> states the
    /// intent explicitly and beats <see cref="EvitaEntityAttribute.GeneratedPrimaryKey"/>, exactly as that
    /// property's documentation promises; the two are never OR'd.
    /// </para>
    /// <para>
    /// A primary key that was only <em>inferred</em> from its name (<c>int Id</c> with no annotation)
    /// expresses no schema intent, in line with the rule every other member follows here. The entity-level
    /// flag is then the only source, and when it is not set the schema is left exactly as it is - which on a
    /// fresh collection means the server's own default and its <c>ADAPT_PRIMARY_KEY_GENERATION</c> evolution
    /// rule decide.
    /// </para>
    /// </remarks>
    private static void ApplyPrimaryKeyGeneration(
        TypedContractDescriptor descriptor,
        EvitaEntityAttribute declaration,
        IEntitySchemaBuilder builder)
    {
        var primaryKey = descriptor.Members.FirstOrDefault(
            it => it.Kind == TypedMemberKind.PrimaryKey && it.NameDeclaredExplicitly
        );

        bool? generated = primaryKey is not null
            ? primaryKey.AutoGeneratePrimaryKey
            : declaration.GeneratedPrimaryKey ? true : null;

        if (generated is null)
        {
            return;
        }

        // the builder is also a schema view of its own current state; the explicit-interface cast is how
        // that state is read, since the editor surface of the same name is the mutation
        bool current = ((IEntitySchema)builder).WithGeneratedPrimaryKey();
        if (generated.Value && !current)
        {
            builder.WithGeneratedPrimaryKey();
        }
        else if (!generated.Value && current)
        {
            builder.WithoutGeneratedPrimaryKey();
        }
    }

    /// <summary>
    /// Declares one reflected reference in the derived schema.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reflected reference is maintained by the server from the reference it reflects, so this declares the
    /// schema and nothing ever writes the data. Both halves of the pair have to name each other correctly:
    /// <c>OfEntity</c> is the entity declaring the original and <c>OfName</c> is that reference's name, and a
    /// mismatch is a schema error from the server rather than an empty reference.
    /// </para>
    /// <para>
    /// The driver refused these until the fluent builder gained
    /// <see cref="IEntitySchemaEditor{TS}.WithReflectedReferenceToEntity"/>; the create-mutation had existed
    /// all along with nothing reaching it.
    /// </para>
    /// </remarks>
    private static void ApplyReflectedReference(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchemaBuilder builder)
    {
        ReflectedReferenceAttribute declaration = member.ReflectedDeclaration
            ?? throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` was analysed as a reflected " +
                "reference but carries no [ReflectedReference] attribute.");

        if (string.IsNullOrWhiteSpace(declaration.OfEntity) || string.IsNullOrWhiteSpace(declaration.OfName))
        {
            throw new EvitaInvalidUsageException(
                $"Member `{descriptor.ContractType.Name}.{member.MemberName}` declares reflected reference " +
                $"`{member.DataName}` without naming what it reflects. Set both `OfEntity` (the entity that " +
                "declares the original reference) and `OfName` (that reference's name).");
        }

        builder.WithReflectedReferenceToEntity(
            member.DataName ?? member.MemberName,
            declaration.OfEntity,
            declaration.OfName,
            faceted: declaration.Faceted ? true : null,
            attributeInheritance: declaration.AttributeInheritance,
            attributeInheritanceFilter: declaration.AttributeInheritanceFilter);
    }

    /// <summary>
    /// Declares one associated data entry in the derived schema. The declared CLR type is passed through
    /// as-is: evitaDB stores anything it does not recognise as a complex object.
    /// </summary>
    private static void ApplyAssociatedData(TypedMemberDescriptor member, IEntitySchemaBuilder builder)
    {
        var declaration = member.AssociatedDataDeclaration!;

        var dataType = member.CollectionShape == TypedCollectionShape.Scalar
            ? member.ItemType
            : member.ItemType.MakeArrayType();

        void Configure(IAssociatedDataSchemaEditor data)
        {
            if (!string.IsNullOrWhiteSpace(declaration.Description))
            {
                data.WithDescription(declaration.Description);
            }

            if (!string.IsNullOrWhiteSpace(declaration.DeprecationNotice))
            {
                data.Deprecated(declaration.DeprecationNotice);
            }

            if (declaration.Localized)
            {
                data.Localized();
            }

            if (declaration.Nullable)
            {
                data.Nullable();
            }
        }

        var method = typeof(IEntitySchemaEditor<IEntitySchemaBuilder>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(it => it is { Name: nameof(IEntitySchemaBuilder.WithAssociatedData),
                              IsGenericMethod: true }
                          && it.GetParameters().Length == 2);

        method.MakeGenericMethod(dataType)
            .Invoke(builder, [member.DataName, (Action<IAssociatedDataSchemaEditor>)Configure]);
    }

    /// <summary>
    /// Declares the entity priced. Currencies are additive across members, so several price members can
    /// each contribute the currencies they care about.
    /// </summary>
    private static void ApplyPrice(
        int indexedPricePlaces,
        string[]? allowedCurrencies,
        IEntitySchemaBuilder builder)
    {
        if (allowedCurrencies is { Length: > 0 })
        {
            builder.WithPriceInCurrency(
                indexedPricePlaces,
                allowedCurrencies.Select(it => new Currency(it)).ToArray()
            );
        }
        else
        {
            builder.WithPrice(indexedPricePlaces);
        }
    }

    /// <summary>
    /// Declares one reference in the derived schema, including the attributes of its reference contract.
    /// </summary>
    private static void ApplyReference(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchemaBuilder builder)
    {
        var declaration = member.ReferenceDeclaration!;
        var referencedEntityType = ResolveReferencedEntityType(descriptor, member, declaration);
        var cardinality = ResolveCardinality(member, declaration);

        void Configure(IReferenceSchemaBuilder reference)
        {
            if (!string.IsNullOrWhiteSpace(declaration.Description))
            {
                reference.WithDescription(declaration.Description);
            }

            if (!string.IsNullOrWhiteSpace(declaration.DeprecationNotice))
            {
                reference.Deprecated(declaration.DeprecationNotice);
            }

            if (!string.IsNullOrWhiteSpace(declaration.GroupEntity))
            {
                if (declaration.GroupEntityManaged)
                {
                    reference.WithGroupTypeRelatedToEntity(declaration.GroupEntity);
                }
                else
                {
                    reference.WithGroupType(declaration.GroupEntity);
                }
            }

            // faceting implies indexing on the server, so declaring both is redundant but harmless
            if (declaration.Indexed)
            {
                reference.Indexed();
            }

            if (declaration.Faceted)
            {
                reference.Faceted();
            }

            ApplyReferenceAttributes(member, reference);
        }

        if (declaration.Managed)
        {
            builder.WithReferenceToEntity(member.DataName!, referencedEntityType, cardinality, Configure);
        }
        else
        {
            builder.WithReferenceTo(member.DataName!, referencedEntityType, cardinality, Configure);
        }
    }

    /// <summary>
    /// Declares the attributes a reference contract annotates on the reference itself.
    /// </summary>
    private static void ApplyReferenceAttributes(
        TypedMemberDescriptor member,
        IReferenceSchemaBuilder reference)
    {
        if (member.ReferenceProjection != ReferenceProjection.ReferenceContract)
        {
            return;
        }

        var referenceContract = TypedContractAnalyzer.DescribeReferenceContract(member.ItemType);

        foreach (var attributeMember in referenceContract.Members)
        {
            if (attributeMember.Kind != TypedMemberKind.Attribute ||
                attributeMember.AttributeDeclaration is null)
            {
                continue;
            }

            var attributeType = attributeMember.CollectionShape == TypedCollectionShape.Scalar
                ? attributeMember.ItemType
                : attributeMember.ItemType.MakeArrayType();

            var configure = BuildConfiguration<IAttributeSchemaBuilder>(
                attributeMember.AttributeDeclaration, attributeMember
            );

            var method = typeof(IAttributeProviderSchemaEditor<IReferenceSchemaBuilder, IAttributeSchema,
                    IAttributeSchemaBuilder>)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Single(it => it is { Name: nameof(IReferenceSchemaBuilder.WithAttribute),
                                  IsGenericMethod: true }
                              && it.GetParameters().Length == 2);

            method.MakeGenericMethod(attributeType)
                .Invoke(reference, [attributeMember.DataName, configure]);
        }
    }

    /// <summary>
    /// Resolves which entity type a reference points to: an explicit declaration wins, otherwise it is
    /// taken from the annotated entity contract the member exposes.
    /// </summary>
    private static string ResolveReferencedEntityType(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        EvitaReferenceAttribute declaration)
    {
        if (!string.IsNullOrWhiteSpace(declaration.Entity))
        {
            return declaration.Entity;
        }

        var fromContract = member.ReferenceProjection switch
        {
            ReferenceProjection.ReferencedEntity => TypedContractAnalyzer.ExtractEntityType(member.ItemType),
            ReferenceProjection.ReferenceContract => ReferencedEntityTypeOfReferenceContract(member.ItemType),
            _ => null
        };

        return fromContract ?? throw new EvitaInvalidUsageException(
            $"Reference `{descriptor.ContractType.Name}.{member.MemberName}` does not say which entity it " +
            $"points to - set [EvitaReference(Entity = \"...\")], or expose an annotated entity contract."
        );
    }

    /// <summary>
    /// Looks inside a reference contract for the entity type its referenced-entity member exposes.
    /// </summary>
    private static string? ReferencedEntityTypeOfReferenceContract(Type referenceContractType)
    {
        var contract = TypedContractAnalyzer.DescribeReferenceContract(referenceContractType);

        return contract.Members
            .Where(it => it.Kind == TypedMemberKind.ReferencedEntity
                         && it.ReferenceProjection == ReferenceProjection.ReferencedEntity)
            .Select(it => TypedContractAnalyzer.ExtractEntityType(it.ItemType))
            .FirstOrDefault(it => it is not null);
    }

    /// <summary>
    /// Derives cardinality from the declared collection shape and whether the reference may be missing,
    /// mirroring the Java driver's rule.
    /// </summary>
    private static Cardinality ResolveCardinality(
        TypedMemberDescriptor member,
        EvitaReferenceAttribute declaration) =>
        member.CollectionShape == TypedCollectionShape.Scalar
            ? declaration.AllowEmpty ? Cardinality.ZeroOrOne : Cardinality.ExactlyOne
            : declaration.AllowEmpty ? Cardinality.ZeroOrMore : Cardinality.OneOrMore;

    private static void ApplyAttribute(
        TypedContractDescriptor descriptor,
        TypedMemberDescriptor member,
        IEntitySchemaBuilder builder)
    {
        var declaration = member.AttributeDeclaration!;
        var attributeType = ResolveSchemaType(descriptor, member);

        var shared = BuildConfiguration<IEntityAttributeSchemaBuilder>(declaration, member);

        // Representative lives on the entity attribute editor only, so it is applied here rather than in
        // the shared configuration that reference attributes also use
        void configure(IEntityAttributeSchemaBuilder attribute)
        {
            shared(attribute);

            if (declaration.Representative)
            {
                attribute.Representative();
            }
        }

        // WithAttribute<TT> is generic over the CLR type, so the call is made reflectively - the
        // source generator will emit the direct, statically-typed call instead
        var method = typeof(IAttributeProviderSchemaEditor<IEntitySchemaBuilder, IEntityAttributeSchema,
                IEntityAttributeSchemaBuilder>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(it => it is { Name: nameof(IEntitySchemaBuilder.WithAttribute), IsGenericMethod: true }
                          && it.GetParameters().Length == 2);

        method.MakeGenericMethod(attributeType)
            .Invoke(builder, [member.DataName, (Action<IEntityAttributeSchemaBuilder>)configure]);
    }

    /// <summary>
    /// Builds the per-attribute configuration lambda. Only positive declarations are applied, per the
    /// additive-reconciliation rule documented on this class.
    /// </summary>
    private static Action<TBuilder> BuildConfiguration<TBuilder>(
        EvitaAttributeAttribute declaration,
        TypedMemberDescriptor member) where TBuilder : IAttributeSchemaEditor<TBuilder> =>
        attribute =>
        {
            if (!string.IsNullOrWhiteSpace(declaration.Description))
            {
                attribute.WithDescription(declaration.Description);
            }

            if (!string.IsNullOrWhiteSpace(declaration.DeprecationNotice))
            {
                attribute.Deprecated(declaration.DeprecationNotice);
            }

            if (declaration.Filterable)
            {
                attribute.Filterable();
            }

            if (declaration.Sortable)
            {
                attribute.Sortable();
            }

            if (declaration.Localized)
            {
                attribute.Localized();
            }

            if (declaration.Nullable)
            {
                attribute.Nullable();
            }

            switch (declaration.Unique)
            {
                case AttributeUniquenessType.UniqueWithinCollection:
                    attribute.Unique();
                    break;
                case AttributeUniquenessType.UniqueWithinCollectionLocale:
                    attribute.UniqueWithinLocale();
                    break;
                case AttributeUniquenessType.NotUnique:
                default:
                    break;
            }

            if (declaration.IndexedDecimalPlaces > 0)
            {
                attribute.IndexDecimalPlaces(declaration.IndexedDecimalPlaces);
            }

            // a member whose CLR type carries no null state still needs a schema default, otherwise a
            // non-nullable attribute would reject entities that never set it
            if (!declaration.Nullable && member is { WrapInMaybe: false } &&
                member.DeclaredType.IsValueType &&
                Nullable.GetUnderlyingType(member.DeclaredType) is null &&
                member.CollectionShape == TypedCollectionShape.Scalar)
            {
                attribute.WithDefaultValue(Activator.CreateInstance(member.DeclaredType));
            }
        };

    /// <summary>
    /// Resolves the CLR type declared to evitaDB: collection-shaped members become array types, which
    /// is how evitaDB models multi-value attributes.
    /// </summary>
    private static Type ResolveSchemaType(TypedContractDescriptor descriptor, TypedMemberDescriptor member)
    {
        // evitaDB has no enum data type: an enum member is stored by name, so the schema declares a string
        var storedType = member.ItemType.IsEnum ? typeof(string) : member.ItemType;

        return member.CollectionShape == TypedCollectionShape.Scalar
            ? storedType
            : storedType.MakeArrayType();
    }
}
