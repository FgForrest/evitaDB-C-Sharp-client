using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Schemas.Mutations.Catalogs;

public class MutationEntitySchemaAccessor : IEntitySchemaProvider
{
    public static readonly MutationEntitySchemaAccessor Instance = new();
    public IEntitySchemaProvider? BaseAccessor { get; private set; }
    public Dictionary<string, IEntitySchema?>? EntitySchemas { get; private set; }
    public HashSet<string>? RemovedEntitySchemas { get; private set; }

    private MutationEntitySchemaAccessor()
    {
        // immutable version of the schema accessor (shared static instance)
        BaseAccessor = null;
    }

    public MutationEntitySchemaAccessor(IEntitySchemaProvider entitySchemaProvider)
    {
        // mutable version of the schema accessor
        BaseAccessor = entitySchemaProvider;
    }

    public IEnumerable<IEntitySchema?> GetEntitySchemas()
    {
        var x = EntitySchemas is null ? [] : EntitySchemas.Values.ToList();
        var y = BaseAccessor is null ? [] : BaseAccessor.GetEntitySchemas()
            .Where(y => y is null || !EntitySchemas!.ContainsKey(y.Name))
            .ToList();
        return x.Concat(y);
    }

    public IEntitySchema? GetEntitySchema(string name)
    {
        if (RemovedEntitySchemas is not null && RemovedEntitySchemas.Contains(name))
        {
            return null;
        }
        if (EntitySchemas is not null && EntitySchemas.TryGetValue(name, out IEntitySchema? entitySchema))
        {
            return entitySchema;
        }
        return BaseAccessor?.GetEntitySchema(name);
    }

    public void AddUpsertedEntitySchema(IEntitySchema entitySchema)
    {
        if (BaseAccessor == null)
        {
            // do nothing - this instance is immutable
        }
        else
        {
            if (EntitySchemas == null)
            {
                EntitySchemas = new Dictionary<string, IEntitySchema?>(8);
            }

            // the indexer keeps the operation idempotent - the catalog schema builder may reapply its mutation
            // list multiple times when materializing the updated schema
            EntitySchemas[entitySchema.Name] = entitySchema;
        }
    }

    public void RemoveEntitySchema(string name)
    {
        if (BaseAccessor == null)
        {
            // do nothing - this instance is immutable
        }
        else
        {
            if (RemovedEntitySchemas == null)
            {
                RemovedEntitySchemas = new HashSet<string>();
            }

            EntitySchemas?.Remove(name);

            if (BaseAccessor.GetEntitySchema(name) is not null)
            {
                RemovedEntitySchemas.Add(name);
            }
        }
    }

    public void ReplaceEntitySchema(string oldName, IEntitySchema entitySchema)
    {
        if (BaseAccessor == null)
        {
            // do nothing - this instance is immutable
        }
        else
        {
            if (EntitySchemas == null)
            {
                EntitySchemas = new Dictionary<string, IEntitySchema?>();
            }

            if (RemovedEntitySchemas == null)
            {
                RemovedEntitySchemas = new HashSet<string>();
            }

            EntitySchemas[entitySchema.Name] = entitySchema;
            if (BaseAccessor.GetEntitySchema(oldName) is not null)
            {
                RemovedEntitySchemas.Add(oldName);
            }
        }
    }
}
