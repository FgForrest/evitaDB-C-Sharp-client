using System.Collections.Immutable;

namespace EvitaDB.Client.Models.ExtraResults;

public class AttributeHistogram : IEvitaResponseExtraResult
{
    private readonly IDictionary<string, IHistogramContract> _histograms;

    public IHistogramContract? GetHistogram(string attributeName)
    {
        return _histograms.TryGetValue(attributeName, out IHistogramContract? histogram) ? histogram : null;
    }
    
    public IDictionary<string, IHistogramContract> Histograms => _histograms.ToImmutableDictionary();
    
    public AttributeHistogram(Dictionary<string, IHistogramContract> histograms)
    {
        _histograms = histograms;
    }
    
    public override string ToString()
    {
        return string.Join("\n", _histograms.Select(x => $"{x.Key}: {x.Value}"));
    }
}