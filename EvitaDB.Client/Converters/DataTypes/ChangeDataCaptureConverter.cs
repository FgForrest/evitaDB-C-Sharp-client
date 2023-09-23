using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Data.Mutations;
using EvitaDB.Client.Models.Schemas.Mutations;

namespace EvitaDB.Client.Converters.DataTypes;

public static class ChangeDataCaptureConverter
{
    private static readonly ISchemaMutationConverter<ITopLevelCatalogSchemaMutation, GrpcTopLevelCatalogSchemaMutation>
        TopLevelCatalogSchemaMutationConverter = new DelegatingTopLevelCatalogSchemaMutationConverter();

    private static readonly ISchemaMutationConverter<ILocalCatalogSchemaMutation, GrpcLocalCatalogSchemaMutation>
        LocalCatalogSchemaMutationConverter = new DelegatingLocalCatalogSchemaMutationConverter();

    private static readonly IEntityMutationConverter<IEntityMutation, GrpcEntityMutation> EntityMutationConverter =
        new DelegatingEntityMutationConverter();

    private static readonly DelegatingEntitySchemaMutationConverter EntitySchemaMutationConverter =
        new DelegatingEntitySchemaMutationConverter();

    private static readonly DelegatingLocalMutationConverter EntityLocalMutationConverter =
        new DelegatingLocalMutationConverter();

    public static GrpcChangeSystemCapture ToGrpcChangeSystemCapture(ChangeSystemCapture changeDataCapture)
    {
        GrpcChangeSystemCapture grpcChangeSystemCapture = new GrpcChangeSystemCapture
        {
            Catalog = changeDataCapture.Catalog,
            Operation = EvitaEnumConverter.ToGrpcOperation(changeDataCapture.Operation)
        };
        if (changeDataCapture.Body != null)
        {
            grpcChangeSystemCapture.Mutation =
                TopLevelCatalogSchemaMutationConverter.Convert((ITopLevelCatalogSchemaMutation) changeDataCapture.Body);
        }

        return grpcChangeSystemCapture;
    }

    public static ChangeSystemCapture ToChangeSystemCapture(GrpcChangeSystemCapture grpcChangeSystemCapture)
    {
        return new ChangeSystemCapture(
            1L, //TODO TPO: revise this
            grpcChangeSystemCapture.Catalog,
            EvitaEnumConverter.ToOperation(grpcChangeSystemCapture.Operation),
            grpcChangeSystemCapture.Mutation != null
                ? TopLevelCatalogSchemaMutationConverter.Convert(grpcChangeSystemCapture.Mutation)
                : null
        );
    }
}