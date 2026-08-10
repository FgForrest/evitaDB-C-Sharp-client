using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Models.Cdc;

public record ChangeCatalogCapture(
    long Version,
    int Index,
    CaptureArea Area,
    string? EntityType,
    Operation Operation,
    IMutation? Body) : IChangeCapture
{
    public static ChangeCatalogCapture DataCapture(
        MutationPredicateContext context,
        Operation operation,
        IMutation? mutation)
    {
        return new ChangeCatalogCapture(
            context.Version,
            context.Index,
            CaptureArea.Data,
            context.EntityType,
            operation,
            mutation);
    }
    
    public static ChangeCatalogCapture SchemaCapture(
        MutationPredicateContext context,
        Operation operation,
        IMutation? mutation)
    {
        return new ChangeCatalogCapture(
            context.Version,
            context.Index,
            CaptureArea.Schema,
            context.EntityType,
            operation,
            mutation);
    }
    
    public static ChangeCatalogCapture InfrastructureCapture(
        MutationPredicateContext context,
        Operation operation,
        IMutation? mutation)
    {
        return new ChangeCatalogCapture(
            context.Version,
            context.Index,
            CaptureArea.Infrastructure,
            context.EntityType,
            operation,
            mutation);
    }
};
