namespace EvitaDB.Client.Models;

/// <summary>
/// Tracks the progress of a transactional session commit through its three server-side phases. Each phase is
/// exposed as a <see cref="Task{CommitVersions}"/> that completes when the server reports the phase as finished:
/// <list type="number">
///     <item><see cref="OnConflictResolved"/> - the changes were checked for conflicts with other transactions</item>
///     <item><see cref="OnWalAppended"/> - the changes were durably written to the write-ahead log</item>
///     <item><see cref="OnChangesVisible"/> - the changes became visible to other sessions</item>
/// </list>
/// The phases complete in order - a later phase completing implies the earlier ones completed as well. When the
/// commit fails, all remaining phase tasks fault with the causing exception.
/// </summary>
public class CommitProgress
{
    private readonly TaskCompletionSource<CommitVersions> _conflictResolved =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<CommitVersions> _walAppended =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource<CommitVersions> _changesVisible =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completes when the commit passed the conflict resolution phase.
    /// </summary>
    public Task<CommitVersions> OnConflictResolved => _conflictResolved.Task;

    /// <summary>
    /// Completes when the committed changes were durably appended to the write-ahead log.
    /// </summary>
    public Task<CommitVersions> OnWalAppended => _walAppended.Task;

    /// <summary>
    /// Completes when the committed changes became visible to other sessions.
    /// </summary>
    public Task<CommitVersions> OnChangesVisible => _changesVisible.Task;

    /// <summary>
    /// Returns the phase task corresponding to the passed commit behavior.
    /// </summary>
    public Task<CommitVersions> On(EvitaClientTransaction.CommitBehavior commitBehavior)
    {
        return commitBehavior switch
        {
            EvitaClientTransaction.CommitBehavior.WaitForConflictResolution => OnConflictResolved,
            EvitaClientTransaction.CommitBehavior.WaitForWalPersistence => OnWalAppended,
            _ => OnChangesVisible
        };
    }

    /// <summary>
    /// True when all phases reached a terminal state (successfully or exceptionally).
    /// </summary>
    public bool IsDone => OnConflictResolved.IsCompleted && OnWalAppended.IsCompleted && OnChangesVisible.IsCompleted;

    /// <summary>
    /// True when all phases completed successfully.
    /// </summary>
    public bool IsCompletedSuccessfully => OnConflictResolved.IsCompletedSuccessfully
                                           && OnWalAppended.IsCompletedSuccessfully
                                           && OnChangesVisible.IsCompletedSuccessfully;

    /// <summary>
    /// True when at least one phase completed exceptionally.
    /// </summary>
    public bool IsCompletedExceptionally => OnConflictResolved.IsFaulted
                                            || OnWalAppended.IsFaulted
                                            || OnChangesVisible.IsFaulted;

    /// <summary>
    /// Completes the given phase (and implicitly all phases preceding it) with the passed versions.
    /// </summary>
    internal void CompletePhase(GrpcTransactionPhase finishedPhase, CommitVersions versions)
    {
        switch (finishedPhase)
        {
            case GrpcTransactionPhase.ConflictsResolved:
                _conflictResolved.TrySetResult(versions);
                break;
            case GrpcTransactionPhase.WalPersisted:
                _conflictResolved.TrySetResult(versions);
                _walAppended.TrySetResult(versions);
                break;
            case GrpcTransactionPhase.ChangesVisible:
                _conflictResolved.TrySetResult(versions);
                _walAppended.TrySetResult(versions);
                _changesVisible.TrySetResult(versions);
                break;
        }
    }

    /// <summary>
    /// Completes all remaining phases with the passed versions - used when the server ends the progress stream
    /// without reporting every phase explicitly (e.g. when there was nothing to commit).
    /// </summary>
    internal void Complete(CommitVersions versions)
    {
        _conflictResolved.TrySetResult(versions);
        _walAppended.TrySetResult(versions);
        _changesVisible.TrySetResult(versions);
    }

    /// <summary>
    /// Faults all phases that did not complete yet with the passed exception.
    /// </summary>
    internal void Fail(Exception exception)
    {
        _conflictResolved.TrySetException(exception);
        _walAppended.TrySetException(exception);
        _changesVisible.TrySetException(exception);
    }
}
