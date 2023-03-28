using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Client.Exceptions;
using Client.Utils;

namespace Client.Certificate;

public class ClientCertificateManager
{
    private static readonly string DefaultClientCertificateFolderPath = Path.GetTempPath() + "evita-client-certificates";

    private string ClientCertificateFolderPath { get; }
    private string? ClientCertificatePath { get; }
    private string? ClientCertificateKeyPath { get; }
    private string? ClientCertificateKeyPassword { get; }
    private bool UseGeneratedCertificate { get; } = true;
    private bool TrustedServerCertificate { get; }
    ClientCertificateManager(string clientCertificateFolderPath, string? clientCertificatePath,
        string? clientCertificateKeyPath, string? clientCertificateKeyPassword, bool useGeneratedCertificate,
        bool trustedServerCertificate)
    {
        ClientCertificateFolderPath = clientCertificateFolderPath;
        ClientCertificatePath = clientCertificatePath;
        ClientCertificateKeyPath = clientCertificateKeyPath;
        ClientCertificateKeyPassword = clientCertificateKeyPassword;
        UseGeneratedCertificate = useGeneratedCertificate;
        TrustedServerCertificate = trustedServerCertificate;
    }

    public ClientCertificateManager()
    {
        ClientCertificateFolderPath = DefaultClientCertificateFolderPath;
    }

    public class Builder
    {
        private string ClientCertificateFolderPath { get; set; } = DefaultClientCertificateFolderPath;
        private string? ClientCertificatePath { get; set; }
        private string? ClientCertificateKeyPath { get; set; }
        private string? ClientCertificateKeyPassword { get; set; }
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool TrustedServerCertificate { get; set; }

        public ClientCertificateManager Build()
        {
            return new ClientCertificateManager(ClientCertificateFolderPath, ClientCertificatePath,
                ClientCertificateKeyPath, ClientCertificateKeyPassword, UseGeneratedCertificate, TrustedServerCertificate);
        }

        public Builder SetClientCertificateFolderPath(string? clientCertificateFolderPath)
        {
            ClientCertificateFolderPath = clientCertificateFolderPath ?? DefaultClientCertificateFolderPath;
            return this;
        }

        public Builder SetClientCertificatePath(string? clientCertificatePath)
        {
            ClientCertificatePath = clientCertificatePath;
            return this;
        }

        public Builder SetClientCertificateKeyPath(string? clientCertificateKeyPath)
        {
            ClientCertificateKeyPath = clientCertificateKeyPath;
            return this;
        }

        public Builder SetClientCertificateKeyPassword(string? clientCertificateKeyPassword)
        {
            ClientCertificateKeyPassword = clientCertificateKeyPassword;
            return this;
        }
        
        public Builder SetUseGeneratedCertificate(bool useGeneratedCertificate)
        {
            UseGeneratedCertificate = useGeneratedCertificate;
            return this;
        }
        
        public Builder SetTrustedServerCertificate(bool trustedServerCertificate)
        {
            TrustedServerCertificate = trustedServerCertificate;
            return this;
        }
    }

    public async void GetCertificatesFromServer(string host, int systemApiPort)
    {
        var apiEndpoint = $"http://{host}:{systemApiPort}/system/";
        if (!Directory.Exists(ClientCertificateFolderPath))
        {
            Directory.CreateDirectory(DefaultClientCertificateFolderPath);
        }
        else
        {
            try
            {
                await DownloadFile(apiEndpoint, CertificateUtils.GeneratedRootCaCertificateFileName);
                await DownloadFile(apiEndpoint, CertificateUtils.GeneratedClientCertificateFileName);
                await DownloadFile(apiEndpoint, CertificateUtils.GeneratedClientCertificateKeyFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new EvitaInvalidUsageException(ex.Message,"Failed to download certificates from the server", ex);
            }
        }
    }

    private async Task DownloadFile(string apiEndpoint, string fileName)
    {
        using var client = new HttpClient();
        await using var stream = await client.GetStreamAsync(apiEndpoint + fileName);
        await using var fileStream = new FileStream($"{ClientCertificateFolderPath}{Path.DirectorySeparatorChar}{fileName}", FileMode.Create);
        await stream.CopyToAsync(fileStream);
    }

    public HttpClientHandler BuildHttpClientHandler()
    {
        var handler = new HttpClientHandler();
        if (!TrustedServerCertificate)
        {
            handler.ServerCertificateCustomValidationCallback = RemoteCertificateValidationCallback;
        }
        return handler;
    }

    private bool RemoteCertificateValidationCallback(
        HttpRequestMessage message, X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
    {
        var usedCert = new X509Certificate2(File.ReadAllBytes($"{ClientCertificateFolderPath}/{CertificateUtils.GeneratedCertificateFileName}"));
        if (cert == null)
            return false;
        return cert.Thumbprint == usedCert.Thumbprint;
    }
}