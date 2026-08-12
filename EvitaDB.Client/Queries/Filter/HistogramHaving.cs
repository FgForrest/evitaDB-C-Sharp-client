using EvitaDB.Client.Exceptions;

namespace EvitaDB.Client.Queries.Filter;

/// <summary>
/// The `histogramHaving` constraint narrows a reference histogram to a range of values (both bounds inclusive),
/// optionally selecting a single group within a grouped reference. It is the counterpart of `priceBetween` for
/// histograms computed over references, and belongs inside the `userFilter` container so that the histogram it
/// filters on is not itself affected by the selection.
///
/// Example - narrow the histogram of a reference that hosts exactly one:
/// <code>
/// histogramHaving("price-range", 50, 120)
/// </code>
/// Example - name the histogram explicitly when the reference hosts several:
/// <code>
/// histogramHaving("parameterValues", "basicUnitValue", 50, 120)
/// </code>
/// Example - pick the group whose histogram is meant, for a grouped reference:
/// <code>
/// histogramHaving("parameterValues", 50, 120, groupHaving(attributeEquals("code", "height")))
/// </code>
///
/// At least one bound must be given, and when both are present `from` must not exceed `to`.
/// </summary>
public class HistogramHaving : AbstractFilterConstraintContainer
{
    private HistogramHaving(object?[] arguments, params IFilterConstraint?[] children) : base(arguments, children)
    {
    }

    public HistogramHaving(string referenceName, string? histogramName, decimal? from, decimal? to,
        GroupHaving? groupHaving = null)
        : base(
            BuildArguments(referenceName, histogramName, from, to),
            groupHaving is null ? [] : [groupHaving]
        )
    {
        ValidateBounds(from, to);
    }

    /// <summary>Reference whose histogram the range applies to.</summary>
    public string ReferenceName => (string) Arguments[0]!;

    /// <summary>Histogram name within the reference, or null when the reference hosts a single histogram.</summary>
    public string? HistogramName => Arguments.Length == 4 ? Arguments[1] as string : null;

    public decimal? From => Arguments[Arguments.Length == 4 ? 2 : 1] as decimal?;

    public decimal? To => Arguments[Arguments.Length == 4 ? 3 : 2] as decimal?;

    /// <summary>Optional group selector for grouped references.</summary>
    public GroupHaving? GroupHaving => Children.Length == 0 ? null : Children[0] as GroupHaving;

    public new bool Applicable => Arguments.Length is 3 or 4 && (From is not null || To is not null);

    /// <summary>
    /// Builds the argument list in one of the two shapes evitaQL accepts:
    /// <c>histogramHaving(reference, from, to)</c> when the reference hosts a single histogram, and
    /// <c>histogramHaving(reference, histogram, from, to)</c> when it must be named.
    ///
    /// The absent name is deliberately <b>not</b> stored as a null in the middle of the list. evitaQL has no
    /// null literal and no empty argument slot, so a stored null could only be serialized by omitting it -
    /// and omitting an interior argument is exactly the corruption the pretty printer refuses to perform
    /// (see PrettyPrintingVisitor.PrintLeaf). Choosing the shorter shape up front keeps the printed query
    /// honest without weakening that guard. Java keeps a four-slot array here; C# cannot, because its
    /// printer will not silently drop a hole.
    /// </summary>
    private static object?[] BuildArguments(string referenceName, string? histogramName, decimal? from, decimal? to) =>
        string.IsNullOrEmpty(histogramName)
            ? [referenceName, from, to]
            : [referenceName, histogramName, from, to];

    public new bool Necessary => Applicable;

    public override IFilterConstraint GetCopyWithNewChildren(IFilterConstraint?[] children,
        IConstraint?[] additionalChildren)
    {
        return new HistogramHaving(Arguments, children);
    }

    private static void ValidateBounds(decimal? from, decimal? to)
    {
        if (from is null && to is null)
        {
            throw new EvitaInvalidUsageException(
                "Constraint `histogramHaving` requires at least one of the `from` and `to` bounds to be set.");
        }
        if (from is not null && to is not null && from > to)
        {
            throw new EvitaInvalidUsageException(
                $"Constraint `histogramHaving` has the `from` bound ({from}) greater than the `to` bound ({to}).");
        }
    }
}
