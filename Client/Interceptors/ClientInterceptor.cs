using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Client.Interceptors;

/// <summary>
/// This class is used to intercept client calls prior their sending to the server. If client did set sessionId and sessionType
/// SessionIdHolder, then these two values will be added to the call metadata.
/// </summary>
public class ClientInterceptor : Interceptor
{
    private const string SessionIdHeader = "sessionId";
    private const string CatalogNameHeader = "catalogName";

    /// <summary>
    /// This method is intercepting client unary calls prior their sending to the server. When target method requires a session, then
    /// the requested information set by the client will be checked in SessionIdHolder. If there is a set sessionId, then
    /// it will be added together with sessionType to the call metadata.
    /// </summary>
    public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var sessionId = SessionIdHolder.GetSessionId();
        if (sessionId == null)
            return base.BlockingUnaryCall(request, context, continuation);
        Metadata metadata = new()
        {
            {SessionIdHeader, sessionId},
            {CatalogNameHeader, SessionIdHolder.GetCatalogName()!}
        };
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            context.Options.WithHeaders(metadata)
        );
        return base.BlockingUnaryCall(request, newContext, continuation);
    }

    /// <summary>
    /// This method is intercepting client unary calls prior their sending to the server. When target method requires a session, then
    /// the requested information set by the client will be checked in SessionIdHolder. If there is a set sessionId, then
    ///  it will be added together with sessionType to the call metadata.
    /// </summary>
    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var sessionId = SessionIdHolder.GetSessionId();
        if (sessionId == null)
            return base.AsyncUnaryCall(request, context, continuation);
        Metadata metadata = new()
        {
            {SessionIdHeader, sessionId},
            {CatalogNameHeader, SessionIdHolder.GetCatalogName()!}
        };
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method,
            context.Host,
            context.Options.WithHeaders(metadata)
        );
        return base.AsyncUnaryCall(request, newContext, continuation);
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
    private static ThreadLocal<SessionDescriptor> _sessionDescriptor = new();

    /// <summary>
    /// Sets sessionId to the context.
    /// </summary>
    /// <param name="catalogName">session to set</param>
    /// <param name="sessionId">session to set</param>
    public static void SetSessionId(string catalogName, string sessionId)
    {
        _sessionDescriptor = new ThreadLocal<SessionDescriptor>(() => new SessionDescriptor(catalogName, sessionId));
    }

    /// <summary>
    /// Resets information about session.
    /// </summary>
    public static void Reset()
    {
        _sessionDescriptor = new ThreadLocal<SessionDescriptor>();
    }

    /// <summary>
    /// Returns sessionId from the context.
    /// </summary>
    /// <returns>sessionId if it exists</returns>
    public static string? GetSessionId()
    {
        SessionDescriptor? descriptor = _sessionDescriptor.Value;
        return descriptor == null ? null : _sessionDescriptor.Value?.SessionId;
    }

    /// <summary>
    /// Returns catalog name from the context.
    /// </summary>
    /// <returns>catalogName if a session is currently in use</returns>
    public static string? GetCatalogName()
    {
        SessionDescriptor? descriptor = _sessionDescriptor.Value;
        return descriptor == null ? null : _sessionDescriptor.Value?.CatalogName;
    }

    /// <summary>
    /// Record that holds information about currently used session.
    /// </summary>
    /// <param name="CatalogName">Name of a catalog.</param>
    /// <param name="SessionId">SessionID that is used internally in evitaDB to perform client's calls.</param>
    private record SessionDescriptor(string CatalogName, string SessionId);
}