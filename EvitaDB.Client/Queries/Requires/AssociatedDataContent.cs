using EvitaDB.Client.Queries.Filter;

namespace EvitaDB.Client.Queries.Requires;

/// <summary>
/// This `associatedData` requirement changes default behaviour of the query engine returning only entity primary keys in
/// the result. When this requirement is used result contains entity bodies along with associated data with names
/// specified in one or more arguments of this requirement.
/// This requirement implicitly triggers <see cref="EntityFetch"/> requirement because attributes cannot be returned without entity.
/// Localized associated data is returned according to <see cref="EntityLocaleEquals"/> query. Requirement might be combined
/// with <see cref="AttributeContent"/> requirement.
/// Example:
/// <code>
/// associatedData("description", "gallery-3d")
/// </code>
/// </summary>
public class AssociatedDataContent : AbstractRequireConstraintLeaf, IEntityContentRequire, IConstraintWithSuffix
{
    public string[] AssociatedDataNames => Arguments.Select(obj => (string) obj!).ToArray();
    private const string Suffix = "all";
    public bool AllRequested => AssociatedDataNames.Length == 0;
    
    private AssociatedDataContent(params object?[] arguments) : base(arguments)
    {
    }

    public AssociatedDataContent() : base()
    {
    }
    
    public AssociatedDataContent(params string[] associatedDataNames) : base(associatedDataNames)
    {
    }

    public string? SuffixIfApplied => AllRequested ? Suffix : null;
    public bool ArgumentImplicitForSuffix(object argument) => false;

}
