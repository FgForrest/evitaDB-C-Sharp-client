namespace Client.Models.Schemas;

public interface IAssociatedDataSchema : INamedSchemaWithDeprecation
{
    bool Nullable { get; }
    bool Localized { get; }
    Type Type { get; }
}