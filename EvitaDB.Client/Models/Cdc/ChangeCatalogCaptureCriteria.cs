using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Models.Cdc;

public record ChangeCatalogCaptureCriteria
{
    public CaptureArea? Area { get; init; }
    public ICaptureSite? Site { get; init; }
    
    public ChangeCatalogCaptureCriteria(CaptureArea? area, ICaptureSite? site)
    {
        if (site is not null)
        {
            switch (area)
            {
                case CaptureArea.Schema: Assert.IsTrue(site is SchemaSite, "Site must be SchemaSite for Schema capture");
                    break;
                case CaptureArea.Data: Assert.IsTrue(site is not SchemaSite, "Site must not be SchemaSite for Data capture");
                    break;
                case CaptureArea.Infrastructure: throw new EvitaInvalidUsageException("Infrastructure area is not supported");
            }
        }
        Area = area;
        Site = site;
    }
    
    public static CdcCriteriaBuilder Builder() => new();
    
    public class CdcCriteriaBuilder
    {
        private CaptureArea? Area { get; set; }
        private ICaptureSite? Site { get; set; }
        
        public CdcCriteriaBuilder WithArea(CaptureArea area)
        {
            Area = area;
            return this;
        }
        
        public CdcCriteriaBuilder WithSite(ICaptureSite site)
        {
            Site = site;
            return this;
        }
        
        public ChangeCatalogCaptureCriteria Build()
        {
            return new ChangeCatalogCaptureCriteria(Area, Site);
        }
    }
};
