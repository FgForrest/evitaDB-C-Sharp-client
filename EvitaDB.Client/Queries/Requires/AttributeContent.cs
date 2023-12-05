using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// The `attributeContent` requirement is used to retrieve one or more entity or reference attributes. Localized attributes
/// are only fetched if there is a locale context in the query, either by using the <see cref="EntityLocaleEquals"/> filter
/// constraint or the dataInLocales require constraint.
/// All entity attributes are fetched from disk in bulk, so specifying only a few of them in the `attributeContent`
/// requirement only reduces the amount of data transferred over the network. It's not bad to fetch all the attributes
/// of an entity using `attributeContentAll`.
/// Example:
/// <code>
/// entityFetch(
///    attributeContent("code", "name")
/// )
/// </code>
/// </summary>
public class AttributeContent : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public static readonly AttributeContent AllAttributes = new();
    public new bool Applicable => true;
    private const string Suffix = "all";
    
    public bool AllRequested => Arguments.Length == 0;
    
    private AttributeContent(params object?[] arguments) : base(arguments)
    {
    }
    
    public AttributeContent() : base()
    {
    }
    
    public AttributeContent(params string[] attributeNames) : base(attributeNames)
    {
    }
    
    public string[] GetAttributeNames()
    {
        return Arguments.Select(obj => (string) obj!).ToArray();
    }

    public string? SuffixIfApplied => AllRequested ? Suffix : null;
    public bool ArgumentImplicitForSuffix(object argument) => false;

}
