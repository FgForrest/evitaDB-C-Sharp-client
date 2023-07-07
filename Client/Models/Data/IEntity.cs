using System.Globalization;
using Client.Models.Data.Structure;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

public interface IEntity : IEntityClassifier, IAttributes, IAssociatedData, IPrices, IVersioned
{
    EntitySchema Schema { get; }
    int? Parent { get; }
    ICollection<Reference> GetReferences();
    Reference? GetReference(string referenceName, int referencedEntityId);
    Reference? GetReference(ReferenceKey referenceKey);
    ISet<CultureInfo> GetAllLocales();
}