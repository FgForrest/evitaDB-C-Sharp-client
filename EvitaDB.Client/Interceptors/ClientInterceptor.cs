using EvitaDB.Client.Config;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace EvitaDB.Client.Interceptors;

/// <summary>
/// This class is used to intercept client calls prior their sending to the server. If client did set sessionId and sessionType
/// SessionIdHolder, then these two values will be added to the call metadata.
/// </summary>
public class ClientInterceptor : Interceptor
{
    private const string SessionIdHeader = "sessionId";
    private const string CatalogNameHeader = "catalogName";
    private const string ClientIdHeader = "clientId";

    private readonly EvitaClientConfiguration? _configuration;

    public ClientInterceptor(EvitaClientConfiguration configuration)
    {
        _configuration = configuration;
    }

    public ClientInterceptor()
    {
        _configuration = null;
    }

    /// <summary>
    /// This method is intercepting client unary calls prior their sending to the server. When target method requires a session, then
    /// the requested information set by the client will be checked in SessionIdHolder. If there is a set sessionId, then
    /// it will be added together with sessionType to the call metadata.
    /// </summary>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        Metadata metadata = new Metadata();
        if (_configuration != null)
        {
            string clientId = _configuration.ClientId;
            metadata.Add(ClientIdHeader, clientId);
        }
        var sessionId = SessionIdHolder.GetSessionId();
        if (sessionId != null)
        {
            metadata.Add(SessionIdHeader, sessionId);
            metadata.Add(CatalogNameHeader, SessionIdHolder.GetCatalogName()!);
        }
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            context.Options.WithHeaders(metadata)
        );
        return base.BlockingUnaryCall(request, newContext, continuation);
    }
}

/// <summary>
/// Class used by client to set sessionId and sessionType in context. These values are used in server session interceptor
/// to set session to the call. It is here used for testing purposes, client can be using some kind of similar approach to this
/// to pass his credentials in form of sessionId to be able to properly use this gRPC API.
/// </summary>
public static class SessionIdHolder
{
    /// <summary>
    /// Context that holds current session in thread-local space.
    /// </summary>
    private static readonly ThreadLocal<SessionDescriptor?> ThreadLocalSessionDescriptor = new();

    /// <summary>
    /// Sets sessionId to the context.
    /// </summary>
    /// <param name="catalogName">session to set</param>
    /// <param name="sessionId">session to set</param>
    public static void SetSessionId(string catalogName, string sessionId)
    {
        ThreadLocalSessionDescriptor.Value = new SessionDescriptor(catalogName, sessionId);
    }

    /// <summary>
    /// Resets information about session.
    /// </summary>
    public static void Reset()
    {
        ThreadLocalSessionDescriptor.Value = null;
    }

    /// <summary>
    /// Returns sessionId from the context.
    /// </summary>
    /// <returns>sessionId if it exists</returns>
    public static string? GetSessionId()
    {
        SessionDescriptor? descriptor = ThreadLocalSessionDescriptor.Value;
        return descriptor == null ? null : ThreadLocalSessionDescriptor.Value?.SessionId;
    }

    /// <summary>
    /// Returns catalog name from the context.
    /// </summary>
    /// <returns>catalogName if a session is currently in use</returns>
    public static string? GetCatalogName()
    {
        SessionDescriptor? descriptor = ThreadLocalSessionDescriptor.Value;
        return descriptor == null ? null : ThreadLocalSessionDescriptor.Value?.CatalogName;
    }

    /// <summary>
    /// Record that holds information about currently used session.
    /// </summary>
    /// <param name="CatalogName">Name of a catalog.</param>
    /// <param name="SessionId">SessionID that is used internally in evitaDB to perform client's calls.</param>
    private record SessionDescriptor(string CatalogName, string SessionId);
}
