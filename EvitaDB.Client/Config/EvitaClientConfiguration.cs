using System.Net;
using System.Net.Sockets;

namespace EvitaDB.Client.Config;

public record EvitaClientConfiguration(
    string ClientId, string Host, int Port, int SystemApiPort, bool UseGeneratedCertificate,
    bool UsingTrustedRootCaCertificate, bool MtlsEnabled, string? ServerCertificatePath, string? CertificateFileName, 
    string? CertificateKeyFileName, string? CertificateKeyPassword, string? CertificateFolderPath, string? TraceEndpointUrl,
    string? TraceEndpointProtocol
)
{
    private const int DefaultGrpcApiPort = 5555;
    private const int DefaultSystemApiPort = 5555;

    public class Builder
    {
        private string ClientId { get; set; }
        private string Host { get; set; } = "localhost";
        private int Port { get; set; } = DefaultGrpcApiPort;
        private int SystemApiPort { get; set; } = DefaultSystemApiPort;
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool UsingTrustedRootCaCertificate { get; set; }
        private bool MtlsEnabled { get; set; }
        private string? ServerCertificatePath { get; set; }
        private string? CertificateFileName { get; set; }
        private string? CertificateKeyFileName { get; set; }
        private string? CertificateKeyPassword { get; set; }
        private string? CertificateFolderPath { get; set; }
        private string? TraceEndpointUrl { get; set; }
        private string? TraceEndpointProtocol { get; set; }

        public Builder()
        {
            try
            {
                ClientId = "gRPC client at " + Dns.GetHostName();
            }
            catch (SocketException)
            {
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

        public EvitaClientConfiguration Build()
        {
            return new EvitaClientConfiguration(
                ClientId, Host, Port, SystemApiPort, UseGeneratedCertificate, UsingTrustedRootCaCertificate,
                MtlsEnabled,
                ServerCertificatePath, CertificateFileName, CertificateKeyFileName,
                CertificateKeyPassword, CertificateFolderPath, TraceEndpointUrl, TraceEndpointProtocol
            );
        }
    }
}
