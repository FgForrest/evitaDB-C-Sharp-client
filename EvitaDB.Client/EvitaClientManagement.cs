using EvitaDB.Client.Converters.DataTypes;
using EvitaDB.Client.Converters.Models;
using EvitaDB.Client.DataTypes;
using EvitaDB.Client.Models;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using TaskStatus = EvitaDB.Client.Models.TaskStatus;

namespace EvitaDB.Client;

/// <summary>
/// Provides access to the evitaDB management service - server status and configuration introspection,
/// long-running task tracking, and backup file upload/download. Obtained via <see cref="EvitaClient.Management"/>.
/// </summary>
public class EvitaClientManagement
{
    private readonly EvitaClient _evitaClient;

    internal EvitaClientManagement(EvitaClient evitaClient)
    {
        _evitaClient = evitaClient;
    }

    /// <summary>
    /// Returns the aggregated status information of the evitaDB server.
    /// </summary>
    public ServerStatus GetServerStatus()
    {
        return GetServerStatusAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetServerStatus"/>.
    /// </summary>
    public async Task<ServerStatus> GetServerStatusAsync(CancellationToken cancellationToken = default)
    {
        GrpcEvitaServerStatusResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.ServerStatusAsync(new Empty(), cancellationToken: cancellationToken).ResponseAsync
        ).ConfigureAwait(false);
        return ManagementConverter.ToServerStatus(response);
    }

    /// <summary>
    /// Returns the actual configuration of the evitaDB server in YAML format.
    /// </summary>
    public string GetConfiguration()
    {
        return GetConfigurationAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetConfiguration"/>.
    /// </summary>
    public async Task<string> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        GrpcEvitaConfigurationResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.GetConfigurationAsync(new Empty(), cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return response.Configuration;
    }

    /// <summary>
    /// Returns statistics of all catalogs known to the server.
    /// </summary>
    public CatalogStatistics[] GetCatalogStatistics()
    {
        return GetCatalogStatisticsAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetCatalogStatistics"/>.
    /// </summary>
    public async Task<CatalogStatistics[]> GetCatalogStatisticsAsync(CancellationToken cancellationToken = default)
    {
        GrpcEvitaCatalogStatisticsResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.GetCatalogStatisticsAsync(new Empty(), cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return response.CatalogStatistics.Select(ManagementConverter.ToCatalogStatistics).ToArray();
    }

    /// <summary>
    /// Returns a page of statuses of long-running tasks, optionally filtered by task type and simplified state.
    /// </summary>
    public PaginatedList<TaskStatus> ListTaskStatuses(int pageNumber, int pageSize,
        string[]? taskTypes = null, TaskSimplifiedState[]? states = null)
    {
        return ListTaskStatusesAsync(pageNumber, pageSize, taskTypes, states).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ListTaskStatuses"/>.
    /// </summary>
    public async Task<PaginatedList<TaskStatus>> ListTaskStatusesAsync(int pageNumber, int pageSize,
        string[]? taskTypes = null, TaskSimplifiedState[]? states = null,
        CancellationToken cancellationToken = default)
    {
        GrpcTaskStatusesRequest request = new GrpcTaskStatusesRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        if (taskTypes is not null)
        {
            request.TaskType.AddRange(taskTypes);
        }
        if (states is not null)
        {
            request.SimplifiedState.AddRange(states.Select(EvitaEnumConverter.ToGrpcTaskSimplifiedState));
        }
        GrpcTaskStatusesResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.ListTaskStatusesAsync(request, cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return new PaginatedList<TaskStatus>(
            response.PageNumber,
            response.PageSize,
            response.TotalNumberOfRecords,
            response.TaskStatus.Select(ManagementConverter.ToTaskStatus).ToList()
        );
    }

    /// <summary>
    /// Returns the status of the task with the given id or null when the task is not found.
    /// </summary>
    public TaskStatus? GetTaskStatus(Guid taskId)
    {
        return GetTaskStatusAsync(taskId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetTaskStatus"/>.
    /// </summary>
    public async Task<TaskStatus?> GetTaskStatusAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        GrpcTaskStatusResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.GetTaskStatusAsync(
                new GrpcTaskStatusRequest { TaskId = EvitaDataTypesConverter.ToGrpcUuid(taskId) },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return response.TaskStatus is not null ? ManagementConverter.ToTaskStatus(response.TaskStatus) : null;
    }

    /// <summary>
    /// Returns the statuses of the tasks with the given ids.
    /// </summary>
    public TaskStatus[] GetTaskStatuses(params Guid[] taskIds)
    {
        return GetTaskStatusesAsync(taskIds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetTaskStatuses"/>.
    /// </summary>
    public async Task<TaskStatus[]> GetTaskStatusesAsync(Guid[] taskIds,
        CancellationToken cancellationToken = default)
    {
        GrpcSpecifiedTaskStatusesRequest request = new GrpcSpecifiedTaskStatusesRequest();
        request.TaskIds.AddRange(taskIds.Select(EvitaDataTypesConverter.ToGrpcUuid));
        GrpcSpecifiedTaskStatusesResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.GetTaskStatusesAsync(request, cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return response.TaskStatus.Select(ManagementConverter.ToTaskStatus).ToArray();
    }

    /// <summary>
    /// Cancels the task with the given id. Returns true when the task was found and its cancellation was requested.
    /// </summary>
    public bool CancelTask(Guid taskId)
    {
        return CancelTaskAsync(taskId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="CancelTask"/>.
    /// </summary>
    public async Task<bool> CancelTaskAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        GrpcCancelTaskResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.CancelTaskAsync(
                new GrpcCancelTaskRequest { TaskId = EvitaDataTypesConverter.ToGrpcUuid(taskId) },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return response.Success;
    }

    /// <summary>
    /// Waits until the task with the given id reaches a terminal state (finished or failed) and returns its
    /// final status. Polls the server on the passed interval (defaults to 500 ms).
    /// </summary>
    public async Task<TaskStatus> WaitForTaskCompletionAsync(Guid taskId, TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        TimeSpan interval = pollInterval ?? TimeSpan.FromMilliseconds(500);
        while (true)
        {
            TaskStatus? status = await GetTaskStatusAsync(taskId, cancellationToken).ConfigureAwait(false);
            if (status is null)
            {
                throw new Exceptions.EvitaInvalidUsageException($"Task `{taskId}` is not known to the server!");
            }
            if (status.IsCompleted)
            {
                return status;
            }
            await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns a page of files stored on the server that are available for download, optionally filtered by origin.
    /// </summary>
    public PaginatedList<FileForFetch> ListFilesToFetch(int pageNumber, int pageSize, string? origin = null)
    {
        return ListFilesToFetchAsync(pageNumber, pageSize, origin).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="ListFilesToFetch"/>.
    /// </summary>
    public async Task<PaginatedList<FileForFetch>> ListFilesToFetchAsync(int pageNumber, int pageSize,
        string? origin = null, CancellationToken cancellationToken = default)
    {
        GrpcFilesToFetchRequest request = new GrpcFilesToFetchRequest
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        if (origin is not null)
        {
            request.Origin.Add(origin);
        }
        GrpcFilesToFetchResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.ListFilesToFetchAsync(request, cancellationToken: cancellationToken)
                .ResponseAsync
        ).ConfigureAwait(false);
        return new PaginatedList<FileForFetch>(
            response.PageNumber,
            response.PageSize,
            response.TotalNumberOfRecords,
            response.FilesToFetch.Select(ManagementConverter.ToFileForFetch).ToList()
        );
    }

    /// <summary>
    /// Returns the descriptor of the server file with the given id or null when it is not found.
    /// </summary>
    public FileForFetch? GetFileToFetch(Guid fileId)
    {
        return GetFileToFetchAsync(fileId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="GetFileToFetch"/>.
    /// </summary>
    public async Task<FileForFetch?> GetFileToFetchAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        GrpcFileToFetchResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.GetFileToFetchAsync(
                new GrpcFileToFetchRequest { FileId = EvitaDataTypesConverter.ToGrpcUuid(fileId) },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return response.FileToFetch is not null ? ManagementConverter.ToFileForFetch(response.FileToFetch) : null;
    }

    /// <summary>
    /// Downloads the server file with the given id and writes its contents to the passed stream.
    /// </summary>
    public void FetchFile(Guid fileId, Stream destination)
    {
        FetchFileAsync(fileId, destination).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="FetchFile"/>.
    /// </summary>
    public async Task FetchFileAsync(Guid fileId, Stream destination, CancellationToken cancellationToken = default)
    {
        await _evitaClient.ExecuteWithEvitaManagementServiceAsync<object?>(async management =>
        {
            using var call = management.FetchFile(
                new GrpcFetchFileRequest { FileId = EvitaDataTypesConverter.ToGrpcUuid(fileId) },
                cancellationToken: cancellationToken
            );
            while (await call.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                call.ResponseStream.Current.FileContents.WriteTo(destination);
            }
            return null;
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes the server file with the given id. Returns true when the file was found and deleted.
    /// </summary>
    public bool DeleteFile(Guid fileId)
    {
        return DeleteFileAsync(fileId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="DeleteFile"/>.
    /// </summary>
    public async Task<bool> DeleteFileAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        GrpcDeleteFileToFetchResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.DeleteFileAsync(
                new GrpcDeleteFileToFetchRequest { FileId = EvitaDataTypesConverter.ToGrpcUuid(fileId) },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return response.Success;
    }

    /// <summary>
    /// Uploads the backup file from the passed stream to the server and starts a restore task for a new catalog
    /// with the given name. Returns the status of the restoration task.
    /// </summary>
    public TaskStatus RestoreCatalog(string catalogName, Stream backupFile)
    {
        return RestoreCatalogAsync(catalogName, backupFile).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RestoreCatalog"/> - streams the backup file to the server in chunks.
    /// </summary>
    public async Task<TaskStatus> RestoreCatalogAsync(string catalogName, Stream backupFile,
        CancellationToken cancellationToken = default)
    {
        GrpcRestoreCatalogResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            async management =>
            {
                using var call = management.RestoreCatalog(cancellationToken: cancellationToken);
                byte[] buffer = new byte[65536];
                int read;
                while ((read = await backupFile.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    await call.RequestStream.WriteAsync(new GrpcRestoreCatalogRequest
                    {
                        CatalogName = catalogName,
                        BackupFile = ByteString.CopyFrom(buffer, 0, read)
                    }, cancellationToken).ConfigureAwait(false);
                }
                await call.RequestStream.CompleteAsync().ConfigureAwait(false);
                return await call.ResponseAsync.ConfigureAwait(false);
            }
        ).ConfigureAwait(false);
        return ManagementConverter.ToTaskStatus(response.Task);
    }

    /// <summary>
    /// Starts a restore task for a new catalog with the given name from a backup file already present on
    /// the server. Returns the status of the restoration task.
    /// </summary>
    public TaskStatus RestoreCatalogFromServerFile(string catalogName, Guid fileId)
    {
        return RestoreCatalogFromServerFileAsync(catalogName, fileId).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Async variant of <see cref="RestoreCatalogFromServerFile"/>.
    /// </summary>
    public async Task<TaskStatus> RestoreCatalogFromServerFileAsync(string catalogName, Guid fileId,
        CancellationToken cancellationToken = default)
    {
        GrpcRestoreCatalogResponse response = await _evitaClient.ExecuteWithEvitaManagementServiceAsync(
            management => management.RestoreCatalogFromServerFileAsync(
                new GrpcRestoreCatalogFromServerFileRequest
                {
                    CatalogName = catalogName,
                    FileId = EvitaDataTypesConverter.ToGrpcUuid(fileId)
                },
                cancellationToken: cancellationToken
            ).ResponseAsync
        ).ConfigureAwait(false);
        return ManagementConverter.ToTaskStatus(response.Task);
    }
}
