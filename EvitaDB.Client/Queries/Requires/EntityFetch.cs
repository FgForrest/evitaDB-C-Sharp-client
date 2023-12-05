namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `entityFetch` requirement is used to trigger loading one or more entity data containers from the disk by its
/// primary key. This operation requires a disk access unless the entity is already loaded in the database cache
/// (frequently fetched entities have higher chance to stay in the cache).
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
///             attributeContent("code", "name")
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
public class EntityFetch : AbstractRequireConstraintContainer, IEntityFetchRequire
{
    protected EntityFetch(IRequireConstraint?[] requireConstraints) : base(requireConstraints)
    {
    }

    public EntityFetch() : base()
    {
    }

    public EntityFetch(params IEntityContentRequire?[] requirements) : base(requirements)
    {
    }
    
    public override IRequireConstraint GetCopyWithNewChildren(IRequireConstraint?[] children, IConstraint?[] additionalChildren)
    {
        return new EntityFetch(children);
    }

    public IEntityContentRequire?[] Requirements => Children.Select(x=>x as IEntityContentRequire).ToArray();
    public new bool Necessary => true;
    public new bool Applicable => true;
}
