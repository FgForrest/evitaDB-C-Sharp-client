namespace Client.Config;

public record EvitaClientConfiguration(
    string Host, int Port, int SystemApiPort, bool UseGeneratedCertificate, bool UsingTrustedRootCaCertificate,
    bool MtlsEnabled, string? ServerCertificatePath, string? CertificateFileName, string? CertificateKeyFileName, 
    string? CertificateKeyPassword, string? CertificateFolderPath
)
{
    private const int DefaultGrpcApiPort = 5556;
    private const int DefaultSystemApiPort = 5557;
    public class Builder
    {
        private string Host { get; set; } = "localhost";
        private int Port { get; set; } = DefaultGrpcApiPort;
        private int SystemApiPort { get; set; } = DefaultSystemApiPort;
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool UsingTrustedRootCaCertificate { get; set; } = false;
        private bool MtlsEnabled { get; set; } = false;
        private string? ServerCertificatePath { get; set; } = null;
        private string? CertificateFileName { get; set; } = null;
        private string? CertificateKeyFileName { get; set; } = null;
        private string? CertificateKeyPassword { get; set; } = null;
        private string? CertificateFolderPath { get; set; } = null;
        
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
        
        public EvitaClientConfiguration Build()
        {
            return new EvitaClientConfiguration(
                Host, Port, SystemApiPort, UseGeneratedCertificate, UsingTrustedRootCaCertificate, MtlsEnabled,
                ServerCertificatePath, CertificateFileName, CertificateKeyFileName, 
                CertificateKeyPassword, CertificateFolderPath
            );
        }
    }
}




