namespace Client.Models.Schemas.Mutations;

public interface IAssociatedDataSchemaMutation : ISchemaMutation
{
    string Name { get; }
    IAssociatedDataSchema? Mutate(IAssociatedDataSchema? associatedDataSchema);
}