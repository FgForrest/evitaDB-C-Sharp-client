using EvitaDB.Client.Config;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EvitaDB.Client.Interceptors;

/// <summary>
/// This class is used to intercept client calls prior their sending to the server. It enriches the call metadata
/// with the client identification, the advertised client version and - when a session is bound to the current
/// execution context - the session id.
/// </summary>
public class ClientInterceptor : Interceptor
{
    private const string SessionIdHeader = "sessionId";
    private const string ClientIdHeader = "clientId";
    private const string ClientVersionHeader = "clientVersion";

    /// <summary>
    /// evitaDB protocol version advertised to the server with every call. The server gates certain wire formats on
    /// this value - most notably clients advertising `2025.4` or newer exchange associated data as a structured
    /// `GrpcDataItem` tree instead of the legacy JSON string (both forms are supported by this client on read,
    /// the structured form is used for writes).
    /// </summary>
    public const string AdvertisedClientVersion = "2026.2.4";

    private readonly EvitaClientConfiguration? _configuration;

    public ClientInterceptor(EvitaClientConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ClientInterceptor()
    {
        _configuration = null;
    }

    private Metadata BuildMetadata()
    {
        Metadata metadata = new Metadata { { ClientVersionHeader, AdvertisedClientVersion } };
        if (_configuration != null)
        {
            metadata.Add(ClientIdHeader, _configuration.ClientId);
        }
        string? sessionId = SessionIdHolder.GetSessionId();
        if (sessionId != null)
        {
            metadata.Add(SessionIdHeader, sessionId);
        }
        return metadata;
    }

    private ClientInterceptorContext<TRequest, TResponse> EnrichContext<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context)
        where TRequest : class
        where TResponse : class
    {
        return new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            context.Options.WithHeaders(BuildMetadata())
        );
    }

    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return base.BlockingUnaryCall(request, EnrichContext(context), continuation);
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        return base.AsyncUnaryCall(request, EnrichContext(context), continuation);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return base.AsyncServerStreamingCall(request, EnrichContext(context), continuation);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return base.AsyncClientStreamingCall(EnrichContext(context), continuation);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        return base.AsyncDuplexStreamingCall(EnrichContext(context), continuation);
    }
}

/// <summary>
/// Class used by client to set sessionId in context. The value is used by the server session interceptor to bind
/// the call to an existing session. Sessions are identified solely by their id - they are no longer scoped by
/// the catalog name.
/// </summary>
public static class SessionIdHolder
{
    /// <summary>
    /// Context that holds current session id. AsyncLocal is used (instead of ThreadLocal) so the value flows
    /// correctly through async/await continuations that may hop threads.
    /// </summary>
    private static readonly AsyncLocal<string?> CurrentSessionId = new();

    /// <summary>
    /// Sets sessionId to the context.
    /// </summary>
    /// <param name="sessionId">session id to set</param>
    public static void SetSessionId(string sessionId)
    {
        CurrentSessionId.Value = sessionId;
    }

    /// <summary>
    /// Resets information about session.
    /// </summary>
    public static void Reset()
    {
        CurrentSessionId.Value = null;
    }

    /// <summary>
    /// Returns sessionId from the context.
    /// </summary>
    /// <returns>sessionId if it exists</returns>
    public static string? GetSessionId()
    {
        return CurrentSessionId.Value;
    }
}
