using Client.Models.Schemas.Mutations.AssociatedData;
using EvitaDB;

namespace Client.Converters.Models.Schema.Mutations.AssociatedData;

public class SetAssociatedDataSchemaLocalizedMutationConverter : ISchemaMutationConverter<SetAssociatedDataSchemaLocalizedMutation, GrpcSetAssociatedDataSchemaLocalizedMutation>
{
    public GrpcSetAssociatedDataSchemaLocalizedMutation Convert(SetAssociatedDataSchemaLocalizedMutation mutation)
    {
        return new GrpcSetAssociatedDataSchemaLocalizedMutation
        {
            Name = mutation.Name,
            Localized = mutation.Localized
        };
    }

    public SetAssociatedDataSchemaLocalizedMutation Convert(GrpcSetAssociatedDataSchemaLocalizedMutation mutation)
    {
        return new SetAssociatedDataSchemaLocalizedMutation(mutation.Name, mutation.Localized);
    }
}