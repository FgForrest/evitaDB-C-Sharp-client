namespace EvitaDB.Client.Models.Schemas;

public interface IAssociatedDataSchema : INamedSchemaWithDeprecation
{
    bool Nullable();
    bool Localized();
    Type Type { get; }
}
