# Async API conventions

Every network-touching method has an async counterpart. The rules below keep the two surfaces in sync
without duplicating logic.

## Async core, sync facade

The **async method is the implementation**; the sync method is a thin facade:

```csharp
public async Task<ISealedEntity?> GetEntityAsync(string entityType, int primaryKey,
    IEntityContentRequire[] require, CancellationToken cancellationToken = default)
{
    // the actual gRPC call via the generated async stub (.ResponseAsync)
}

public ISealedEntity? GetEntity(string entityType, int primaryKey, params IEntityContentRequire[] require)
{
    return GetEntityAsync(entityType, primaryKey, require).GetAwaiter().GetResult();
}
```

Rules:

* Async methods are named `XxxAsync` and always take a `CancellationToken` with a default value as the
  **last** parameter. Because of that, sync overloads keep `params` arrays while async variants take a
  plain array.
* Sync facades use `.GetAwaiter().GetResult()` (not `.Result`/`.Wait()`) so exceptions surface
  unwrapped rather than inside `AggregateException`.
* The async core calls the generated async gRPC stub (`call.ResponseAsync`), never the blocking stub.
* Exception translation (`TranslateRpcException`) lives in the async core so both surfaces share it.
* Session-level plumbing exists in paired form too: `ExecuteWithBlockingEvitaSessionService` /
  `ExecuteWithBlockingEvitaSessionServiceAsync`, `ExecuteInTransactionIfPossible(Async)` — new calls
  should reuse these rather than opening channels manually.

## Client-level catalog access

`EvitaClient` exposes paired catalog lambdas; the async variants accept async delegates:

```csharp
Task<T> QueryCatalogAsync<T>(string catalogName, Func<EvitaClientSession, Task<T>> queryLogic, ...);
Task<T> UpdateCatalogAsync<T>(string catalogName, Func<EvitaClientSession, Task<T>> updater, ...);
```

Both create an ad-hoc session, run the delegate and close the session safely — including on exceptions.

## Streaming

Server-streaming RPCs are exposed as `IAsyncEnumerable<T>` iterator methods:

* `RegisterChangeCatalogCaptureAsync`, `RegisterSystemChangeCaptureAsync`, `GetMutationsHistoryAsync`.
* `yield` cannot live inside a `catch` block, so stream advancement goes through the
  `MoveNextTranslated` helper, which wraps `MoveNext` and translates `RpcException`.
* Cancellation flows in via `[EnumeratorCancellation]` on the token parameter.
* When a sync counterpart is warranted (Java returns `Stream<T>`), it is a lazy blocking enumerator
  over the async stream (see `GetMutationsHistory`) — it must dispose the async enumerator and cancel
  the underlying call when the caller abandons the enumeration.

## Long-running operations

Catalog lifecycle RPCs (`…WithProgress`) return progress streams. Async workflow helpers:

* `DrainProgressStreamAsync` — consumes a progress stream, tolerating server-side aborts that occur
  even though the operation continues.
* `WaitForCatalogNamesConditionAsync` / `WaitForCatalogStateAsync` — effect-polling fallbacks that
  confirm the operation's outcome when the stream dies prematurely.
* `CommitProgress` — three `TaskCompletionSource`-backed phase tasks
  (`OnConflictResolved`, `OnWalAppended`, `OnChangesVisible`) completed while draining the
  transactional close stream. Java's `Progress<T>`/`CompletionStage` maps to `Task<T>` (+
  `IProgress<int>` where percentage callbacks exist, e.g. `GoLiveAndCloseWithProgressAsync`).

## Testing

Async counterparts are covered by `EvitaDB.Test/Tests/EvitaClientAsyncTest.cs`, which mirrors the most
valuable sync scenarios (query trio, CRUD, archiving, commit progress, cancellation). New async APIs
should get a mirror test there when they carry logic beyond a trivial facade.
