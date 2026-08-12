using System.Net;

namespace EvitaDB.Client.Config;

public record EvitaClientConfiguration(
    string ClientId, string Host, int Port, int SystemApiPort, bool UseGeneratedCertificate,
    bool UsingTrustedRootCaCertificate, bool TlsEnabled, bool MtlsEnabled, string? ServerCertificatePath, string? CertificateFileName,
    string? CertificateKeyFileName, string? CertificateKeyPassword, string? CertificateFolderPath, string? TraceEndpointUrl,
    string? TraceEndpointProtocol, int PingIntervalMilliseconds, int IdleTimeoutMilliseconds
)
{
    private const int DefaultGrpcApiPort = 5555;
    private const int DefaultSystemApiPort = 5555;
    /// <summary>
    /// Default interval of the HTTP/2 keepalive ping. The evitaDB server (Armeria) closes idle connections, so the
    /// client keeps them alive by pinging - mirrors the Java driver's `pingIntervalMillis` default.
    /// </summary>
    private const int DefaultPingIntervalMilliseconds = 30_000;
    /// <summary>
    /// Default idle timeout after which a pooled connection is closed - mirrors the Java driver's
    /// `idleTimeoutMillis` default.
    /// </summary>
    private const int DefaultIdleTimeoutMilliseconds = 300_000;
    /// <summary>
    /// Default number of gRPC channels kept for unary calls.
    /// </summary>
    private const int DefaultChannelPoolSize = 10;

    /// <summary>
    /// Optional transport handler used for every gRPC channel this client creates. When set, the client uses it
    /// verbatim and does <b>not</b> construct a <see cref="System.Net.Http.SocketsHttpHandler"/>, does not apply the
    /// keep-alive/idle-timeout tuning and does not build a <c>ClientCertificateManager</c> - the caller owns the
    /// transport and, with it, TLS.
    ///
    /// This exists for hosts where <see cref="System.Net.Http.SocketsHttpHandler"/> is unavailable - most notably
    /// Blazor WebAssembly, which must reach the server over gRPC-Web:
    /// <code>
    /// .SetHttpHandler(new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()))
    /// </code>
    /// This is a C#-only addition with no counterpart in the Java driver - see
    /// <c>documentation/architecture.md</c>.
    /// </summary>
    public HttpMessageHandler? HttpHandler { get; init; }

    /// <summary>
    /// Number of gRPC channels pre-created for unary calls. Defaults to <see cref="DefaultChannelPoolSize"/>.
    /// Browser hosts should set this to 1 - the browser multiplexes requests itself and every channel allocates
    /// its own <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    public int ChannelPoolSize { get; init; } = DefaultChannelPoolSize;

    public class Builder
    {
        private string ClientId { get; set; }
        private string Host { get; set; } = "localhost";
        private int Port { get; set; } = DefaultGrpcApiPort;
        private int SystemApiPort { get; set; } = DefaultSystemApiPort;
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool UsingTrustedRootCaCertificate { get; set; }
        private bool TlsEnabled { get; set; } = true;
        private bool MtlsEnabled { get; set; }
        private string? ServerCertificatePath { get; set; }
        private string? CertificateFileName { get; set; }
        private string? CertificateKeyFileName { get; set; }
        private string? CertificateKeyPassword { get; set; }
        private string? CertificateFolderPath { get; set; }
        private string? TraceEndpointUrl { get; set; }
        private string? TraceEndpointProtocol { get; set; }
        private int PingIntervalMilliseconds { get; set; } = DefaultPingIntervalMilliseconds;
        private int IdleTimeoutMilliseconds { get; set; } = DefaultIdleTimeoutMilliseconds;
        private HttpMessageHandler? HttpHandler { get; set; }
        private int ChannelPoolSize { get; set; } = DefaultChannelPoolSize;

        public Builder()
        {
            try
            {
                ClientId = "gRPC client at " + Dns.GetHostName();
            }
            catch (Exception)
            {
                // name resolution is unavailable on some hosts - `browser-wasm` throws
                // PlatformNotSupportedException rather than SocketException - and the client id is cosmetic,
                // so no failure here may prevent a configuration from being built
                ClientId = "Generic gRPC client";
            }
        }

        public Builder SetClientId(string clientId)
        {
            ClientId = clientId;
            return this;
        }

        public Builder SetHost(string host)
        {
            Host = host;
            return this;
        }

        public Builder SetPort(int port)
        {
            Port = port;
            return this;
        }

        public Builder SetSystemApiPort(int systemApiPort)
        {
            SystemApiPort = systemApiPort;
            return this;
        }

        public Builder SetUseGeneratedCertificate(bool useGeneratedCertificate)
        {
            UseGeneratedCertificate = useGeneratedCertificate;
            return this;
        }

        public Builder SetUsingTrustedRootCaCertificate(bool usingTrustedRootCaCertificate)
        {
            UsingTrustedRootCaCertificate = usingTrustedRootCaCertificate;
            return this;
        }

        public Builder SetTlsEnabled(bool tlsEnabled)
        {
            TlsEnabled = tlsEnabled;
            return this;
        }

        public Builder SetMtlsEnabled(bool mtlsEnabled)
        {
            MtlsEnabled = mtlsEnabled;
            return this;
        }

        public Builder SetServerCertificatePath(string serverCertificatePath)
        {
            ServerCertificatePath = serverCertificatePath;
            return this;
        }

        public Builder SetCertificateFileName(string certificateFileName)
        {
            CertificateFileName = certificateFileName;
            return this;
        }

        public Builder SetCertificateKeyFileName(string certificateKeyFileName)
        {
            CertificateKeyFileName = certificateKeyFileName;
            return this;
        }

        public Builder SetCertificateKeyPassword(string certificateKeyPassword)
        {
            CertificateKeyPassword = certificateKeyPassword;
            return this;
        }

        public Builder SetCertificateFolderPath(string certificateFolderPath)
        {
            CertificateFolderPath = certificateFolderPath;
            return this;
        }

        public Builder SetTraceEndpointUrl(string traceEndpointUrl)
        {
            TraceEndpointUrl = traceEndpointUrl;
            return this;
        }

        public Builder SetTraceEndpointProtocol(string traceEndpointProtocol)
        {
            TraceEndpointProtocol = traceEndpointProtocol;
            return this;
        }

        /// <summary>
        /// Sets the interval of the HTTP/2 keepalive ping. Values &lt;= 0 disable the keepalive ping.
        /// </summary>
        public Builder SetPingIntervalMilliseconds(int pingIntervalMilliseconds)
        {
            PingIntervalMilliseconds = pingIntervalMilliseconds;
            return this;
        }

        /// <summary>
        /// Sets the idle timeout after which a pooled connection is closed. Values &lt;= 0 keep the runtime default.
        /// </summary>
        public Builder SetIdleTimeoutMilliseconds(int idleTimeoutMilliseconds)
        {
            IdleTimeoutMilliseconds = idleTimeoutMilliseconds;
            return this;
        }

        /// <summary>
        /// Supplies the transport handler used for every gRPC channel - see
        /// <see cref="EvitaClientConfiguration.HttpHandler"/>. Required for Blazor WebAssembly hosts, which must
        /// pass a gRPC-Web handler here.
        /// </summary>
        public Builder SetHttpHandler(HttpMessageHandler? httpHandler)
        {
            HttpHandler = httpHandler;
            return this;
        }

        /// <summary>
        /// Sets the number of gRPC channels pre-created for unary calls - see
        /// <see cref="EvitaClientConfiguration.ChannelPoolSize"/>.
        /// </summary>
        public Builder SetChannelPoolSize(int channelPoolSize)
        {
            ChannelPoolSize = channelPoolSize;
            return this;
        }

        public EvitaClientConfiguration Build()
        {
            return new EvitaClientConfiguration(
                ClientId, Host, Port, SystemApiPort, UseGeneratedCertificate, UsingTrustedRootCaCertificate,
                TlsEnabled, MtlsEnabled,
                ServerCertificatePath, CertificateFileName, CertificateKeyFileName,
                CertificateKeyPassword, CertificateFolderPath, TraceEndpointUrl, TraceEndpointProtocol,
                PingIntervalMilliseconds, IdleTimeoutMilliseconds
            )
            {
                HttpHandler = HttpHandler,
                ChannelPoolSize = ChannelPoolSize
            };
        }
    }
}
