using EvitaDB.Client.DataTypes;

namespace EvitaDB.Client.Models.Cdc;

public record DataSite(
    string? EntityType,
    int? EntityPrimaryKey,
    Operation[]? Operation,
    ContainerType[]? ContainerType,
    string[]? ContainerName) : ICaptureSite
{
    private static readonly SchemaSite All = new(string.Empty, [], null);

    public class Builder
    {
        private string? EntityType { get; set; }
        private int? EntityPrimaryKey { get; set; }
        private Operation[]? Operation { get; set; }
        private ContainerType[]? ContainerType { get; set; }
        private string[]? ContainerName { get; set; }
        
        public Builder WithEntityType(string entityType)
        {
            EntityType = entityType;
            return this;
        }
        
        public Builder WithEntityPrimaryKey(int entityPrimaryKey)
        {
            EntityPrimaryKey = entityPrimaryKey;
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
        
        public Builder WithContainerName(params string[] containerName)
        {
            ContainerName = containerName;
            return this;
        }
        
        public DataSite Build()
        {
            return new DataSite(EntityType, EntityPrimaryKey, Operation, ContainerType, ContainerName);
        }
    }
};
