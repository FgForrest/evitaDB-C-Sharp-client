using EvitaDB.Client.Models.Data.Mutations.Reference;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Interface that simply combines writer and builder contracts together.
/// </summary>
public interface IReferenceBuilder : IReferenceEditor<IReferenceBuilder>, IBuilder<IReference, ReferenceMutation>
{
}