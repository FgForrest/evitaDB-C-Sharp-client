namespace EvitaDB.Client.Models;

/// <summary>
/// Record describing the state of a long-running server-side task.
/// </summary>
public record TaskStatus(
    string TaskType,
    string TaskName,
    Guid TaskId,
    string? CatalogName,
    DateTimeOffset? Created,
    DateTimeOffset? Issued,
    DateTimeOffset? Started,
    DateTimeOffset? Finished,
    TaskSimplifiedState SimplifiedState,
    int Progress,
    string? Settings,
    string? TextResult,
    FileForFetch? FileResult,
    string? PublicExceptionMessage,
    TaskTrait[] Traits
)
{
    /// <summary>
    /// Returns true when the task reached a terminal state.
    /// </summary>
    public bool IsCompleted => SimplifiedState is TaskSimplifiedState.Finished or TaskSimplifiedState.Failed;
}
