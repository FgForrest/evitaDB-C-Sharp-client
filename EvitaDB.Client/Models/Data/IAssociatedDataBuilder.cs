using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data;

/// <summary>
/// Interface that simply combines writer and builder contracts together.
/// </summary>
public interface IAssociatedDataBuilder : IAssociatedDataEditor<IAssociatedDataBuilder>,
    IBuilder<AssociatedData, AssociatedDataMutation>
{
    internal static IAssociatedDataSchema CreateImplicitSchema(AssociatedDataValue associatedDataValue)
    {
        return AssociatedDataSchema.InternalBuild(
            associatedDataValue.Key.AssociatedDataName,
            null, null,
            associatedDataValue.Key.Localized,
            true,
            associatedDataValue.Value.GetType()
        );
    }
}