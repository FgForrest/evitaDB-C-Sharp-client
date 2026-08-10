namespace EvitaDB.Client.Models.Mutations;

public class MutationPredicateContext
{
    public IMutation.StreamDirection Direction { get; }
    public long Version { get; private set; } = 0L;
    public int Index { get; private set; } = 0;
    public string? EntityType { get; private set; }
    private bool _primaryKeyIdentified = false;
    private int _primaryKey = int.MinValue;
    private int _mutationCount = 0;

    public MutationPredicateContext(IMutation.StreamDirection direction)
    {
        Direction = direction;
    }

    public int? PrimaryKey => _primaryKeyIdentified ? _primaryKey : null;

    public void SetPrimaryKey(int entityPrimaryKey)
    {
        _primaryKeyIdentified = true;
        _primaryKey = entityPrimaryKey;
    }

    public void ResetPrimaryKey()
    {
        _primaryKeyIdentified = false;
        _primaryKey = int.MinValue;
    }

    public void SetEntityType(string entityType)
    {
        EntityType = entityType;
        _primaryKeyIdentified = false;
    }
    
    public void ResetEntityType()
    {
        EntityType = null;
        _primaryKeyIdentified = false;
    }
    
    public bool MatchEntityType(string entityType)
    {
        return EntityType is not null && EntityType == entityType;
    }
    
    public void SetVersion(long version, int mutationCount)
    {
        Version = version;
        _primaryKeyIdentified = false;
        EntityType = null;
        _mutationCount = mutationCount;
        Index = 0;
    }

    public void Advance()
    {
        if (Direction == IMutation.StreamDirection.Forward)
        {
            Index++;
        }
        else if (Index == 0)
        {
            Index = _mutationCount;
        }
        else
        {
            Index--;
        }
    }
}
