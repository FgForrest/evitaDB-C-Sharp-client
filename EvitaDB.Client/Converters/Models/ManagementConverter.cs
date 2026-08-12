using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Models;
using TaskStatus = EvitaDB.Client.Models.TaskStatus;

namespace EvitaDB.Client.Converters.Models;

/// <summary>
/// Converts management-related gRPC messages to their client model counterparts.
/// </summary>
public static class ManagementConverter
{
    public static ServerStatus ToServerStatus(GrpcEvitaServerStatusResponse grpcStatus)
    {
        return new ServerStatus(
            grpcStatus.Version,
            grpcStatus.StartedAt is not null
                ? EvitaDataTypesConverter.ToDateTimeOffset(grpcStatus.StartedAt)
                : null,
            grpcStatus.Uptime,
            grpcStatus.InstanceId,
            grpcStatus.CatalogsCorrupted,
            grpcStatus.CatalogsActive,
            grpcStatus.CatalogsInactive,
            grpcStatus.HealthProblems.Select(EvitaEnumConverter.ToHealthProblem).ToArray(),
            EvitaEnumConverter.ToReadiness(grpcStatus.Readiness),
            grpcStatus.ReadOnly
        );
    }

    public static FileForFetch ToFileForFetch(GrpcFile grpcFile)
    {
        return new FileForFetch(
            EvitaDataTypesConverter.ToGuid(grpcFile.FileId),
            grpcFile.Name,
            grpcFile.Description,
            grpcFile.ContentType,
            grpcFile.TotalSizeInBytes,
            EvitaDataTypesConverter.ToDateTimeOffset(grpcFile.Created),
            grpcFile.Origin
        );
    }

    public static TaskStatus ToTaskStatus(GrpcTaskStatus grpcTaskStatus)
    {
        return new TaskStatus(
            grpcTaskStatus.TaskType,
            grpcTaskStatus.TaskName,
            EvitaDataTypesConverter.ToGuid(grpcTaskStatus.TaskId),
            grpcTaskStatus.CatalogName,
            grpcTaskStatus.Created is not null
                ? EvitaDataTypesConverter.ToDateTimeOffset(grpcTaskStatus.Created)
                : null,
            grpcTaskStatus.Issued is not null
                ? EvitaDataTypesConverter.ToDateTimeOffset(grpcTaskStatus.Issued)
                : null,
            grpcTaskStatus.Started is not null
                ? EvitaDataTypesConverter.ToDateTimeOffset(grpcTaskStatus.Started)
                : null,
            grpcTaskStatus.Finished is not null
                ? EvitaDataTypesConverter.ToDateTimeOffset(grpcTaskStatus.Finished)
                : null,
            EvitaEnumConverter.ToTaskSimplifiedState(grpcTaskStatus.SimplifiedState),
            grpcTaskStatus.Progress,
            grpcTaskStatus.Settings,
            grpcTaskStatus.ResultCase == GrpcTaskStatus.ResultOneofCase.Text ? grpcTaskStatus.Text : null,
            grpcTaskStatus.ResultCase == GrpcTaskStatus.ResultOneofCase.File
                ? ToFileForFetch(grpcTaskStatus.File)
                : null,
            grpcTaskStatus.Exception,
            grpcTaskStatus.Trait.Select(EvitaEnumConverter.ToTaskTrait).ToArray()
        );
    }

    public static CatalogStatistics ToCatalogStatistics(GrpcCatalogStatistics grpcStatistics)
    {
        return new CatalogStatistics(
            grpcStatistics.CatalogId is not null
                ? EvitaDataTypesConverter.ToGuid(grpcStatistics.CatalogId)
                : null,
            grpcStatistics.CatalogName,
            EvitaEnumConverter.ToCatalogState(grpcStatistics.CatalogState),
            grpcStatistics.CatalogVersion,
            grpcStatistics.TotalRecords,
            grpcStatistics.IndexCount,
            grpcStatistics.SizeOnDiskInBytes,
            grpcStatistics.EntityCollectionStatistics
                .Select(it => new EntityCollectionStatistics(
                    it.EntityType, it.TotalRecords, it.IndexCount, it.SizeOnDiskInBytes
                ))
                .ToArray(),
            grpcStatistics.ReadOnly,
            grpcStatistics.Unusable
        );
    }
}
