using Client.Queries.Filter;
using Client.Queries.Order;
using Client.Utils;

namespace Client.Queries.Requires;

//TODO other constructors, group fetch
public class ReferenceContent : AbstractRequireConstraintContainer, IEntityContentRequire,
    ISeparateEntityContentRequireContainer, IConstraintWithSuffix
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
    private static readonly ReferenceContent AllReferences = new ReferenceContent();


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


    private ReferenceContent(string[] referencedEntityType, IRequireConstraint[] requirements,
        IConstraint[] additionalChildren) : base(referencedEntityType, requirements, additionalChildren)
    {
    }

    public ReferenceContent(params string[] referenceNames) : base(referenceNames)
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
        : base(new[] {referenceName}, entityFetch, entityGroupFetch)
    {
    }

    public ReferenceContent(string[] referenceNames, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(referenceNames, entityFetch, entityGroupFetch)
    {
    }

    public ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(new[] {referenceName}, new IRequireConstraint[] {entityFetch, entityGroupFetch},
            filterBy, orderBy)
    {
    }

    public ReferenceContent(string referenceName, FilterBy? filterBy, OrderBy? orderBy,
        AttributeContent? attributeContent, EntityFetch? entityFetch, EntityGroupFetch? entityGroupFetch)
        : base(new[] {referenceName}, new IRequireConstraint[] {attributeContent, entityFetch, entityGroupFetch},
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

    public ReferenceContent(string referencedEntityType) : base(new[] {referencedEntityType})
    {
    }


    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint[] children,
        IConstraint[] additionalChildren)
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