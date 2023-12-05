namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `entityGroupFetch` requirement is similar to <see cref="EntityFetch"/> but is used to trigger loading one or more
/// referenced group entities in the <see cref="ReferenceContent"/> parent.
/// 
/// Example:
/// <code>
/// query(
///     collection("Brand"),
///     filterBy(
///         entityPrimaryKeyInSet(64703),
///         entityLocaleEquals("en")
///     ),
///     require(
///         entityFetch(
///             referenceContent(
///                "parameterValues",
///                entityGroupFetch(
///                   attributeContent("code", "name")
///                )
///             )
///         )
///     )
/// )
/// </code>
/// See internal contents available for fetching in <see cref="IEntityContentRequire"/>:
/// <list type="bullet">
/// <item><term><see cref="AttributeContent"/></term></item>
/// <item><term><see cref="AssociatedDataContent"/></term></item>
/// <item><term><see cref="PriceContent"/></term></item>
/// <item><term><see cref="HierarchyContent"/></term></item>
/// <item><term><see cref="ReferenceContent"/></term></item>
/// </list>
/// </summary>
public class EntityGroupFetch : AbstractRequireConstraintContainer, IEntityFetchRequire
{
    private EntityGroupFetch(IRequireConstraint?[] requireConstraints) : base(requireConstraints)
    {
    }
    
    public EntityGroupFetch() : base()
    {
    }
    
    public EntityGroupFetch(params IEntityContentRequire?[] requirements) : base(requirements)
    {
    }
    
    public new bool Applicable => true;
    
    public IEntityContentRequire?[] Requirements => Children
        .Select(x => x as IEntityContentRequire)
        .ToArray();
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityGroupFetch(children);
    }
}
