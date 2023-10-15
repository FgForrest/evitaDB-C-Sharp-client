using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using EvitaDB.Client.Exceptions;
using EvitaDB.Client.Utils;

namespace EvitaDB.Client.Certificate;

public class ClientCertificateManager
{
    private static readonly string DefaultClientCertificateFolderPath =
        Path.Combine(Path.GetTempPath(), "evita-client-certificates");

    private string ClientCertificateFolderPath { get; }
    private string? ClientCertificatePath { get; }
    private string? ClientCertificateKeyPath { get; }
    private string? ClientCertificateKeyPassword { get; }
    private bool UseGeneratedCertificate { get; }
    private bool TrustedServerCertificate { get; }

    private ClientCertificateManager(string clientCertificateFolderPath, string? clientCertificatePath,
        string? clientCertificateKeyPath, string? clientCertificateKeyPassword, bool useGeneratedCertificate,
        bool trustedServerCertificate, string host, int port)
    {
        string certificateDirectory;
        if (useGeneratedCertificate)
        {
            certificateDirectory = GetCertificatesFromServer(host, port, clientCertificateFolderPath).GetAwaiter().GetResult();
        }
        else
        {
            certificateDirectory = IdentifyServerDirectory(host, port, clientCertificateFolderPath).GetAwaiter().GetResult();
        }

        ClientCertificateFolderPath = certificateDirectory;
        ClientCertificatePath = clientCertificatePath;
        ClientCertificateKeyPath = clientCertificateKeyPath;
        ClientCertificateKeyPassword = clientCertificateKeyPassword;
        UseGeneratedCertificate = useGeneratedCertificate;
        TrustedServerCertificate = trustedServerCertificate;
    }

    public class Builder
    {
        private string ClientCertificateFolderPath { get; set; } = DefaultClientCertificateFolderPath;
        private string? ClientCertificatePath { get; set; }
        private string? ClientCertificateKeyPath { get; set; }
        private string? ClientCertificateKeyPassword { get; set; }
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool TrustedServerCertificate { get; set; }
        private string? Host { get; set; }
        private int Port { get; set; }

        public ClientCertificateManager Build()
        {
            return new ClientCertificateManager(ClientCertificateFolderPath, ClientCertificatePath,
                ClientCertificateKeyPath, ClientCertificateKeyPassword, UseGeneratedCertificate,
                TrustedServerCertificate, Host!, Port);
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

        public Builder SetUseGeneratedCertificate(bool useGeneratedCertificate, string host, int port)
        {
            UseGeneratedCertificate = useGeneratedCertificate;
            Host = host;
            Port = port;
            return this;
        }

        public Builder SetTrustedServerCertificate(bool trustedServerCertificate)
        {
            TrustedServerCertificate = trustedServerCertificate;
            return this;
        }
    }

    private async Task<string> GetCertificatesFromServer(string host, int systemApiPort,
        string certificateClientFolderPath)
    {
        string apiEndpoint = $"http://{host}:{systemApiPort}/system/";
        string serverName = await GetServerName(apiEndpoint);
        string serverSpecificDirectory = Path.Combine(certificateClientFolderPath, serverName);
        if (!Directory.Exists(serverSpecificDirectory))
        {
            Assert.IsTrue(Directory.CreateDirectory(serverSpecificDirectory).Exists,
                "Cannot create folder `" + serverSpecificDirectory + "`!");
        }
        else
        {
            string rootCaCert =
                Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedRootCaCertificateFileName);
            string cert = Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateFileName);
            string key = Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateKeyFileName);
            if (Path.Exists(rootCaCert) && Path.Exists(cert) && Path.Exists(key))
            {
                return serverSpecificDirectory;
            }
        }

        try
        {
            DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedCertificateFileName);
            DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateFileName);
            DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateKeyFileName);

            return serverSpecificDirectory;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new EvitaInvalidUsageException(ex.Message, "Failed to download certificates from the server", ex);
        }
    }

    private void DownloadFile(string apiEndpoint, string baseDir, string fileName)
    {
        using var client = new HttpClient();
        var response = client.GetAsync(apiEndpoint + fileName).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        using Stream contentStream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult(),
            stream = new FileStream(Path.Combine(baseDir, fileName), FileMode.Create);
        contentStream.CopyTo(stream);
    }

    private static async Task<string> IdentifyServerDirectory(string host, int port, string certificateClientFolderPath)
    {
        string apiEndpoint = "http://" + host + ":" + port + "/system/";
        try
        {
            string serverName = await GetServerName(apiEndpoint);
            return Path.Combine(certificateClientFolderPath, serverName);
        }
        catch (IOException e)
        {
            throw new EvitaInvalidUsageException("Failed to download certificates from server", e);
        }
    }

    private static async Task<string> GetServerName(string apiEndpoint)
    {
        using HttpClient client = new HttpClient();
        try
        {
            return await client.GetStringAsync(apiEndpoint + "server-name");
        }
        catch (HttpRequestException ex)
        {
            throw new EvitaInvalidUsageException(ex.Message, "Failed to get server name", ex);
        }
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
        var usedCert =
            new X509Certificate2(
                File.ReadAllBytes($"{ClientCertificateFolderPath}/{CertificateUtils.GeneratedCertificateFileName}"));
        if (cert == null)
            return false;
        return cert.Thumbprint == usedCert.Thumbprint;
    }
}