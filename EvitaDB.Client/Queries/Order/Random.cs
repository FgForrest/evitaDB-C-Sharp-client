namespace EvitaDB.Client.Queries.Order;

/// <summary>
/// Random ordering is useful in situations where you want to present the end user with the unique entity listing every
/// time he/she accesses it. The constraint makes the order of the entities in the result random.
/// Example:
/// <code>
/// random()
/// </code>
/// When a seed is supplied the ordering is still randomized but reproducible - the same seed always yields the
/// same order, which is what makes a randomized listing testable. The seeded form renders with a `withSeed`
/// suffix:
/// <code>
/// randomWithSeed(42)
/// </code>
/// </summary>
public class Random : AbstractOrderConstraintLeaf, IConstraintWithSuffix
{
    private const string SuffixWithSeed = "withSeed";

    public new bool Applicable => true;

    private Random(params object?[] arguments) : base(arguments)
    {
    }

    public Random() : base()
    {
    }

    // NOTE: goes through the object?[] constructor rather than `base(seed)` - see the overload trap
    // documented on AttributeNatural, where a first argument bound to the (string? name, params object?[])
    // overload silently became the constraint's name.
    public Random(long seed) : this(new object?[] { seed })
    {
    }

    /// <summary>Seed for the random number generator, or null when the ordering is not reproducible.</summary>
    public long? Seed => Arguments.OfType<long>().Cast<long?>().FirstOrDefault();

    public string? SuffixIfApplied => Seed is null ? null : SuffixWithSeed;

    public bool ArgumentImplicitForSuffix(object argument) => false;
}
