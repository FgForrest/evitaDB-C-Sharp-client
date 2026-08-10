using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.Converters.Models.Data.Mutations;
using EvitaDB.Client.Converters.Models.Schema.Mutations;
using EvitaDB.Client.Models.Cdc;
using EvitaDB.Client.Models.Mutations;

namespace EvitaDB.Client.Converters.DataTypes;

public static class ChangeCaptureConverter
{
    public static ChangeCatalogCaptureRequest ToChangeCaptureRequest(GetMutationsHistoryPageRequest request)
    {
        return new ChangeCatalogCaptureRequest(
            request.SinceVersion,
            request.SinceIndex,
            request.Criteria?.Select(ChangeCaptureConverter.ToChangeCaptureCriteria).ToArray(),
            EvitaEnumConverter.ToCaptureContent(request.Content)
        );
    }

    public static ChangeCatalogCaptureRequest ToChangeCaptureRequest(GetMutationsHistoryRequest request)
    {
        return new ChangeCatalogCaptureRequest(
            request.SinceVersion,
            request.SinceIndex,
            request.Criteria?.Select(ChangeCaptureConverter.ToChangeCaptureCriteria).ToArray(),
            EvitaEnumConverter.ToCaptureContent(request.Content)
        );
    }

    public static GetMutationsHistoryRequest ToGrpcChangeCaptureRequest(ChangeCatalogCaptureRequest request)
    {
        var grpcRequest = new GetMutationsHistoryRequest
        {
            Content = EvitaEnumConverter.ToGrpcCaptureContent(request.Content),
            SinceVersion = request.SinceVersion,
            SinceIndex = request.SinceIndex
        };
        if (request.Criteria is not null)
        {
            grpcRequest.Criteria.AddRange(request.Criteria.Select(ToGrpcCaptureCriteria));
        }
        return grpcRequest;
    }

    public static ChangeCatalogCapture ToChangeCatalogCapture(GrpcChangeCatalogCapture changeCatalogCapture)
    {
        IMutation? mutation;
        if (changeCatalogCapture.EntityMutation is not null)
        {
            mutation = DelegatingEntityMutationConverter.Instance.Convert(changeCatalogCapture.EntityMutation);
        }
        else if (changeCatalogCapture.LocalMutation is not null)
        {
            mutation = DelegatingLocalMutationConverter.Instance.Convert(changeCatalogCapture.LocalMutation);
        }
        else if (changeCatalogCapture.SchemaMutation is not null)
        {
            mutation = DelegatingEntitySchemaMutationConverter.Instance.Convert(changeCatalogCapture.SchemaMutation);
        }
        else
        {
            mutation = null;
        }

        return new ChangeCatalogCapture(
                changeCatalogCapture.Version,
                changeCatalogCapture.Index,
                EvitaEnumConverter.ToCaptureArea(changeCatalogCapture.Area),
                changeCatalogCapture.EntityType,
                EvitaEnumConverter.ToOperation(changeCatalogCapture.Operation),
                mutation);
    }

    public static GrpcChangeCatalogCapture ToGrpcChangeCatalogCapture(ChangeCatalogCapture changeCatalogCapture)
    {
        return new GrpcChangeCatalogCapture
        {
            Version = changeCatalogCapture.Version,
            Index = changeCatalogCapture.Index,
            Area = EvitaEnumConverter.ToGrpcCaptureArea(changeCatalogCapture.Area),
            EntityType = changeCatalogCapture.EntityType,
            Operation = EvitaEnumConverter.ToGrpcOperation(changeCatalogCapture.Operation)
        };
    }
    
    public static GrpcCaptureCriteria ToGrpcCaptureCriteria(ChangeCatalogCaptureCriteria criteria)
    {
        GrpcCaptureCriteria grpcCriteria = new GrpcCaptureCriteria
        {
            Area = EvitaEnumConverter.ToGrpcCaptureArea(criteria.Area)
        };
        if (criteria.Site is DataSite dataSite)
        {
            grpcCriteria.DataSite = ToGrpcCaptureDataSite(dataSite);
        }
        else if (criteria.Site is SchemaSite schemaSite)
        {
            grpcCriteria.SchemaSite = ToGrpcCaptureSchema(schemaSite);
        }

        return grpcCriteria;
    }

    private static ChangeCatalogCaptureCriteria ToChangeCaptureCriteria(GrpcCaptureCriteria grpcCaptureCriteria)
    {
        CaptureArea captureArea = EvitaEnumConverter.ToCaptureArea(grpcCaptureCriteria.Area);
        return new ChangeCatalogCaptureCriteria(
            captureArea,
            captureArea == CaptureArea.Schema ? ToSchemaSite(grpcCaptureCriteria.SchemaSite) : ToDataSite(grpcCaptureCriteria.DataSite)
            );
    }
    
    private static DataSite ToDataSite(GrpcCaptureDataSite grpcCaptureDataSite)
    {
        return new DataSite(
            grpcCaptureDataSite.EntityType,
            grpcCaptureDataSite.EntityPrimaryKey,
            grpcCaptureDataSite.Operation.Select(EvitaEnumConverter.ToOperation).ToArray(),
            grpcCaptureDataSite.ContainerType.Select(EvitaEnumConverter.ToContainerType).ToArray(),
            grpcCaptureDataSite.ContainerName.ToArray()
        );
    }
    
    private static GrpcCaptureDataSite ToGrpcCaptureDataSite(DataSite dataSite)
    {
        GrpcCaptureDataSite grpcDataSite = new GrpcCaptureDataSite
        {
            EntityType = dataSite.EntityType,
            EntityPrimaryKey = dataSite.EntityPrimaryKey
        };
        if (dataSite.Operation is not null)
        {
            grpcDataSite.Operation.AddRange(dataSite.Operation.Select(EvitaEnumConverter.ToGrpcOperation));
        }
        if (dataSite.ContainerType is not null)
        {
            grpcDataSite.ContainerType.AddRange(dataSite.ContainerType.Select(EvitaEnumConverter.ToGrpcCaptureContainerType));
        }
        if (dataSite.ContainerName is not null)
        {
            grpcDataSite.ContainerName.AddRange(dataSite.ContainerName);
        }
        return grpcDataSite;
    }
    
    private static SchemaSite ToSchemaSite(GrpcCaptureSchemaSite grpcCaptureSchemaSite)
    {
        return new SchemaSite(
            grpcCaptureSchemaSite.EntityType,
            grpcCaptureSchemaSite.Operation.Select(EvitaEnumConverter.ToOperation).ToArray(),
            grpcCaptureSchemaSite.ContainerType.Select(EvitaEnumConverter.ToContainerType).ToArray()
        );
    }
    
    private static GrpcCaptureSchemaSite ToGrpcCaptureSchema(SchemaSite schemaSite)
    {
        GrpcCaptureSchemaSite grpcSchemaSite = new GrpcCaptureSchemaSite
        {
            EntityType = schemaSite.EntityType
        };
        if (schemaSite.Operation is not null)
        {
            grpcSchemaSite.Operation.AddRange(schemaSite.Operation.Select(EvitaEnumConverter.ToGrpcOperation));
        }
        if (schemaSite.ContainerType is not null)
        {
            grpcSchemaSite.ContainerType.AddRange(schemaSite.ContainerType.Select(EvitaEnumConverter.ToGrpcCaptureContainerType));
        }
        return grpcSchemaSite;
    }
}
