using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models.Schemas.Mutations.AssociatedData;

namespace EvitaDB.Client.Converters.Models.Schema.Mutations.AssociatedData;

public class CreateAssociatedDataSchemaMutationConverter : ISchemaMutationConverter<CreateAssociatedDataSchemaMutation,
    GrpcCreateAssociatedDataSchemaMutation>
{
    public GrpcCreateAssociatedDataSchemaMutation Convert(CreateAssociatedDataSchemaMutation mutation)
    {
        return new GrpcCreateAssociatedDataSchemaMutation
        {
            Name = mutation.Name,
            Description = mutation.Description,
            DeprecationNotice = mutation.DeprecationNotice,
            Type = EvitaDataTypesConverter.ToGrpcEvitaAssociatedDataDataType(mutation.Type),
            Localized = mutation.Localized,
            Nullable = mutation.Nullable
        };
    }

    public CreateAssociatedDataSchemaMutation Convert(GrpcCreateAssociatedDataSchemaMutation mutation)
    {
        return new CreateAssociatedDataSchemaMutation(mutation.Name, mutation.Description, mutation.DeprecationNotice,
            EvitaDataTypesConverter.ToEvitaDataType(mutation.Type), mutation.Localized, mutation.Nullable);
    }
}