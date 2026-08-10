using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Models.Cdc;

public record SchemaSite(
    string? EntityType,
    Operation[]? Operation,
    ContainerType[]? ContainerType) : ICaptureSite
{
    private static readonly SchemaSite All = new(string.Empty, [], null);

    public class Builder
    {
        private string? EntityType { get; set; }
        private Operation[]? Operation { get; set; }
        private ContainerType[]? ContainerType { get; set; }
        
        public Builder WithEntityType(string entityType)
        {
            EntityType = entityType;
            return this;
        }
        
        public Builder WithOperation(params Operation[] operation)
        {
            Operation = operation;
            return this;
        }
        
        public Builder WithContainerType(params ContainerType[] containerType)
        {
            ContainerType = containerType;
            return this;
        }
        
        public SchemaSite Build()
        {
            return new SchemaSite(EntityType, Operation, ContainerType);
        }
    }
};
