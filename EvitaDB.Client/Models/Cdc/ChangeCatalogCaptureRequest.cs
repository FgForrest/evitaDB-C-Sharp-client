namespace EvitaDB.Client.Models.Cdc;

public record ChangeCatalogCaptureRequest(
    long? SinceVersion,
    int? SinceIndex,
    ChangeCatalogCaptureCriteria[]? Criteria,
    CaptureContent Content) : IChangeCaptureRequest
{
    public static ChangeCatalogCaptureRequestBuilder Builder() => new();

    public class ChangeCatalogCaptureRequestBuilder
    {
        public long? SinceVersion { get; set; }
        public int? SinceIndex { get; set; }
        public ChangeCatalogCaptureCriteria[]? Criteria { get; set; }
        public CaptureContent Content { get; set; } = CaptureContent.Header;
        
        public ChangeCatalogCaptureRequestBuilder WithSinceVersion(long? sinceVersion)
        {
            SinceVersion = sinceVersion;
            return this;
        }
        
        public ChangeCatalogCaptureRequestBuilder WithSinceIndex(int? sinceIndex)
        {
            SinceIndex = sinceIndex;
            return this;
        }
        
        public ChangeCatalogCaptureRequestBuilder WithCriteria(ChangeCatalogCaptureCriteria[]? criteria)
        {
            Criteria = criteria;
            return this;
        }
        
        public ChangeCatalogCaptureRequestBuilder WithContent(CaptureContent content)
        {
            Content = content;
            return this;
        }
        
        public ChangeCatalogCaptureRequest Build()
        {
            return new ChangeCatalogCaptureRequest(SinceVersion, SinceIndex, Criteria, Content);
        }
    }
};
