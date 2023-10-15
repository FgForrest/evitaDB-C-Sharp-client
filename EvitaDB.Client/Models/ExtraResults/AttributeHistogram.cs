using System.Collections.Immutable;

namespace EvitaDB.Client.Models.ExtraResults;

public class AttributeHistogram : IEvitaResponseExtraResult
{
    private readonly IDictionary<string, IHistogram> _histograms;

    public IHistogram? GetHistogram(string attributeName)
    {
        return _histograms.TryGetValue(attributeName, out IHistogram? histogram) ? histogram : null;
    }
    
    public IDictionary<string, IHistogram> Histograms => _histograms.ToImmutableDictionary();
    
    public AttributeHistogram(Dictionary<string, IHistogram> histograms)
    {
        _histograms = histograms;
    }
    
    public override string ToString()
    {
        return string.Join("\n", _histograms.Select(x => $"{x.Key}: {x.Value}"));
    }
}