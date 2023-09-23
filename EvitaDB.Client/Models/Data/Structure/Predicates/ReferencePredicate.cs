using System.Globalization;
using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Models.Data.Structure.Predicates;

/// <summary>
/// This predicate allows limiting number of references visible to the client based on query constraints.
/// </summary>
public class ReferencePredicate
{
    public static readonly ReferencePredicate DefaultInstance = new();

    /// <summary>
    /// Contains information about all reference names that has been fetched / requested for the entity.
    /// </summary>
    public IDictionary<string, AttributeRequest> ReferenceSet { get; }

    /// <summary>
    /// Contains true if any of the references of the entity has been fetched / requested.
    /// </summary>
    public bool RequiresEntityReferences { get; }

    /// <summary>
    /// Contains information about implicitly derived locale during entity fetch.
    /// </summary>
    public CultureInfo? ImplicitLocale { get; }

    /// <summary>
    /// Contains information about all attribute locales that has been fetched / requested for the entity.
    /// </summary>
    public ISet<CultureInfo> Locales { get; }

    public ReferencePredicate()
    {
        RequiresEntityReferences = true;
        ReferenceSet = new Dictionary<string, AttributeRequest>();
        ImplicitLocale = null;
        Locales = new HashSet<CultureInfo>();
    }

    public ReferencePredicate(EvitaRequest evitaRequest)
    {
        RequiresEntityReferences = evitaRequest.RequiresEntityReferences();
        ReferenceSet = evitaRequest.GetReferenceEntityFetch()
            .ToDictionary(x=>x.Key, x=>x.Value.AttributeRequest);
        ImplicitLocale = evitaRequest.GetImplicitLocale();
        Locales = evitaRequest.GetRequiredLocales();
    }

    public ReferencePredicate(bool requiresEntityReferences)
    {
        RequiresEntityReferences = requiresEntityReferences;
        ReferenceSet = new Dictionary<string, AttributeRequest>();
        ImplicitLocale = null;
        Locales = new HashSet<CultureInfo>();
    }

    internal ReferencePredicate(
        IDictionary<string, AttributeRequest> referenceSet,
        bool requiresEntityReferences,
        CultureInfo? implicitLocale,
        ISet<CultureInfo> locales
    )
    {
        ReferenceSet = referenceSet;
        RequiresEntityReferences = requiresEntityReferences;
        ImplicitLocale = implicitLocale;
        Locales = locales;
    }

    /// <summary>
    /// Returns true if the references were fetched along with the entity.
    /// </summary>
    public bool WasFetched() => RequiresEntityReferences;

    /// <summary>
    /// Returns true if the references of particular name were fetched along with the entity.
    /// </summary>
    /// <param name="referenceName">name of the reference to inspect</param>
    public bool WasFetched(string referenceName) =>
        RequiresEntityReferences && (!ReferenceSet.Any() || ReferenceSet.ContainsKey(referenceName));
    
    /// <summary>
    /// Method verifies that references were fetched with the entity.
    /// </summary>
    /// <exception cref="ContextMissingException">thrown when no references have been requested</exception>
    public void CheckFetched()
    {
        if (!RequiresEntityReferences)
        {
            throw ContextMissingException.ReferenceContextMissing();
        }
    }
    
    /// <summary>
    /// Method verifies that the requested reference was fetched with the entity.
    /// </summary>
    /// <param name="referenceName">name of the reference to check</param>
    /// <exception cref="ContextMissingException">thrown when the reference was not fetched</exception>
    public void CheckFetched(string referenceName)
    {
        if (!(RequiresEntityReferences && (!ReferenceSet.Any() || ReferenceSet.ContainsKey(referenceName))))
        {
            throw ContextMissingException.ReferenceContextMissing(referenceName);
        }
    }
}