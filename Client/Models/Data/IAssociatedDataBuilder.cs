using Client.Models.Data.Mutations.AssociatedData;
using Client.Models.Data.Structure;
using Client.Models.Mutations;
using Client.Models.Schemas;
using Client.Models.Schemas.Dtos;

namespace Client.Models.Data;

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