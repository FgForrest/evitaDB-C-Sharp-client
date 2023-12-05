using EvitaDB.Client.Queries.Filter;
using EvitaDB.Client.Queries.Order;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `referenceContent` requirement allows you to access the information about the references the entity has towards
/// other entities (either managed by evitaDB itself or by any other external system). This variant of referenceContent
/// doesn't return the attributes set on the reference itself - if you need those attributes, use the
/// `referenceContentWithAttributes` variant of it.
/// Example:
/// <code>
/// entityFetch(
///    attributeContent("code"),
///    referenceContent("brand"),
///    referenceContent("categories")
/// )
/// </code>
/// ## Referenced entity (group) fetching
/// In many scenarios, you'll need to fetch not only the primary keys of the referenced entities, but also their bodies
/// and the bodies of the groups the references refer to. One such common scenario is fetching the parameters of
/// a product:
/// <code>
/// referenceContent(
///     "parameterValues",
///     entityFetch(
///         attributeContent("code")
///     ),
///     entityGroupFetch(
///         attributeContent("code")
///     )
/// )
/// </code>
/// ## Filtering references
/// Sometimes your entities have a lot of references and you don't need all of them in certain scenarios. In this case,
/// you can use the filter constraint to filter out the references you don't need.
/// The referenceContent filter implicitly targets the attributes on the same reference it points to, so you don't need
/// to specify a referenceHaving constraint. However, if you need to declare constraints on referenced entity attributes,
/// you must wrap them in the <see cref="EntityHaving"/> container constraint.
/// For example, your product has got a lot of parameters, but on product detail page you need to fetch only those that
/// are part of group which contains an attribute `isVisibleInDetail` set to TRUE.To fetch only those parameters,
/// use the following query:
/// <code>
/// referenceContent(
///     "parameterValues",
///     filterBy(
///         entityHaving(
///             referenceHaving(
///                 "parameter",
///                 entityHaving(
///                     attributeEquals("isVisibleInDetail", true)
///                 )
///             )
///         )
///     ),
///     entityFetch(
///         attributeContent("code")
///     ),
///     entityGroupFetch(
///         attributeContent("code", "isVisibleInDetail")
///     )
/// )
/// </code>
/// ##Ordering references
/// By default, the references are ordered by the primary key of the referenced entity. If you want to order
/// the references by a different property - either the attribute set on the reference itself or the property of the
/// referenced entity - you can use the order constraint inside the referenceContent requirement.
/// The `referenceContent` filter implicitly targets the attributes on the same reference it points to, so you don't need
/// to specify a referenceHaving constraint. However, if you need to declare constraints on referenced entity attributes,
/// you must wrap them in the entityHaving container constraint.
/// Let's say you want your parameters to be ordered by an English name of the parameter. To do this, use the following
/// query:
/// <code>
/// referenceContent(
///     "parameterValues",
///     orderBy(
///         entityProperty(
///             attributeNatural("name", ASC)
///         )
///     ),
///     entityFetch(
///         attributeContent("name")
///     )
/// )
/// </code>
/// </summary>
public class ReferenceContent : AbstractRequireConstraintContainer, IEntityContentRequire,
    ISeparateEntityContentRequireContainer, IConstraintContainerWithSuffix
{
    public EntityFetch? EntityRequirements => Children.OfType<EntityFetch>().FirstOrDefault();
    public EntityGroupFetch? EntityGroupRequirements => Children.OfType<EntityGroupFetch>().FirstOrDefault();
    public FilterBy? FilterBy => (FilterBy?) GetAdditionalChild(typeof(FilterBy));
    public OrderBy? OrderBy => (OrderBy?) GetAdditionalChild(typeof(OrderBy));
    public AttributeContent? AttributeContent => Children.OfType<AttributeContent>().FirstOrDefault();

    public new bool Necessary => true;
    public new bool Applicable => true;
    public bool AllRequested => Arguments.Length == 0;

    private static ReferenceContent WithRequiredAttributes(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent attributeContent, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
    {
        return new ReferenceContent(referenceName, filterBy, orderBy, attributeContent, entityFetch, entityGroupFetch);
    }

    public string? SuffixIfApplied
    {
        get
        {
            if (AllRequested && AttributeContent is null)
            {
                return SuffixAll;
            }

            if (AllRequested && AttributeContent is not null)
            {
                return SuffixAllWithAttributes;
            }

            if (AttributeContent is not null)
            {
                return SuffixWithAttributes;
            }

            return null;
        }
    }

    public bool ArgumentImplicitForSuffix(object argument) => false;

    private const string SuffixAll = "all";
    private const string SuffixWithAttributes = "withAttributes";
    private const string SuffixAllWithAttributes = "allWithAttributes";
    private static readonly ReferenceContent AllReferences = new();
    
    public string ReferenceName
    {
        get
        {
            string[] referenceNames = ReferencedNames;
            Assert.IsTrue(referenceNames.Length == 1, "There are multiple reference names, cannot return single name.");
            return referenceNames[0];
        }
    }

    public string[] ReferencedNames => Arguments.Select(obj => (string) obj!).ToArray();


    private ReferenceContent(string?[] referencedEntityType, IRequireConstraint?[] requirements,
        IConstraint?[] additionalChildren) : base(referencedEntityType, requirements, additionalChildren)
    {
    }

    public ReferenceContent(params string?[] referenceNames) : base(referenceNames)
    {
    }

    public ReferenceContent(string referenceName, AttributeContent? attributeContent) : base(referenceName,
        attributeContent)
    {
    }

    public ReferenceContent(EntityFetch? entityRequirement, EntityGroupFetch? groupEntityRequirement) : base(
        entityRequirement, groupEntityRequirement)
    {
    }

    public ReferenceContent(string referenceName, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(new object?[] {referenceName}, entityFetch, entityGroupFetch)
    {
    }

    public ReferenceContent(string?[] referenceNames, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(referenceNames, entityFetch, entityGroupFetch)
    {
    }

    public ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(new object?[] {referenceName}, new IRequireConstraint?[] {entityFetch, entityGroupFetch},
            filterBy, orderBy)
    {
    }

    public ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(new object?[] {referenceName}, new IRequireConstraint?[] {attributeContent, entityFetch, entityGroupFetch},
            filterBy, orderBy)
    {
    }

    public ReferenceContent(AttributeContent? attributeContent, EntityFetch? entityRequirement,
        EntityGroupFetch? entityGroupRequirements) : base(attributeContent ?? new AttributeContent(), entityRequirement,
        entityGroupRequirements)
    {
    }

    public ReferenceContent() : base()
    {
    }

    public ReferenceContent(AttributeContent? attributeContent) : base(attributeContent ?? new AttributeContent())
    {
    }

    public ReferenceContent(string referencedEntityType) : base(new object?[] {referencedEntityType})
    {
    }

    public bool ChildImplicitForSuffix(IConstraint? child)
    {
        return child is AttributeContent {AllRequested: true};
    }

    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        if (AdditionalChildren.Length > 2 || (AdditionalChildren.Length == 2 &&
                                              !AdditionalChildren[0].GetType()
                                                  .IsAssignableFrom(typeof(IFilterConstraint)) && !AdditionalChildren[1]
                                                  .GetType().IsAssignableFrom(typeof(IOrderConstraint))))
        {
            throw new ArgumentException("Expected single or no additional filter and order child query.");
        }

        return new ReferenceContent(ReferencedNames, children, additionalChildren);
    }
}
