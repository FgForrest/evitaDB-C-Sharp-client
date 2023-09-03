using EvitaDB.Client.Models.Mutations;
using EvitaDB.Client.Models.Data.Mutations.AssociatedData;
using EvitaDB.Client.Models.Data.Structure;
using EvitaDB.Client.Models.Schemas;
using EvitaDB.Client.Models.Schemas.Dtos;

namespace EvitaDB.Client.Models.Data;

public interface IAssociatedDataBuilder : IBuilder<AssociatedData, AssociatedDataMutation>
{
    public static IAssociatedDataSchema CreateImplicitSchema(AssociatedDataValue associatedDataValue) {
        return AssociatedDataSchema.InternalBuild(
            associatedDataValue.Key.AssociatedDataName,
            null, null,
            associatedDataValue.Key.Localized,
            true,
            associatedDataValue.Value?.GetType()
        );
    }
}