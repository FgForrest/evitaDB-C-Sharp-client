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
    private bool UsingMtls { get; }

    private ClientCertificateManager(string clientCertificateFolderPath, string? clientCertificatePath,
        string? clientCertificateKeyPath, string? clientCertificateKeyPassword, bool useGeneratedCertificate,
        bool trustedServerCertificate, bool usingMtls)
    {
        ClientCertificateFolderPath = clientCertificateFolderPath;
        ClientCertificatePath = clientCertificatePath;
        ClientCertificateKeyPath = clientCertificateKeyPath;
        ClientCertificateKeyPassword = clientCertificateKeyPassword;
        UseGeneratedCertificate = useGeneratedCertificate;
        TrustedServerCertificate = trustedServerCertificate;
        UsingMtls = usingMtls;
    }

    private static async ValueTask<string> GetServerDirectoryPath(string host, int port, string clientCertificateFolderPath, bool useGeneratedCertificate, bool usingMtls)
    {
        if (useGeneratedCertificate)
        {
            return await GetCertificatesFromServer(host, port, clientCertificateFolderPath, usingMtls);
        }
        return await IdentifyServerDirectory(host, port, clientCertificateFolderPath);
    }

    public class Builder
    {
        private string ClientCertificateFolderPath { get; set; } = DefaultClientCertificateFolderPath;
        private string? ClientCertificatePath { get; set; }
        private string? ClientCertificateKeyPath { get; set; }
        private string? ClientCertificateKeyPassword { get; set; }
        private bool UseGeneratedCertificate { get; set; } = true;
        private bool TrustedServerCertificate { get; set; }
        private bool UsingMtls { get; set; }
        private string? Host { get; set; }
        private int Port { get; set; }

        public async Task<ClientCertificateManager> Build()
        {
            string certificatePath = await GetServerDirectoryPath(Host!, Port, ClientCertificateFolderPath, UseGeneratedCertificate, UsingMtls);
            return new ClientCertificateManager(certificatePath, ClientCertificatePath,
                ClientCertificateKeyPath, ClientCertificateKeyPassword, UseGeneratedCertificate,
                TrustedServerCertificate, UsingMtls);
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

        public Builder SetUsingMtls(bool usingMtls)
        {
            UsingMtls = usingMtls;
            return this;
        }
    }

    private static async Task<string> GetCertificatesFromServer(string host, int systemApiPort,
        string certificateClientFolderPath, bool usingMtls)
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
            string serverCert =
                Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedCertificateFileName);
            string cert = Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateFileName);
            string key = Path.Combine(serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateKeyFileName);
            if (Path.Exists(serverCert) && Path.Exists(cert) && Path.Exists(key))
            {
                return serverSpecificDirectory;
            }
        }

        try
        {
            await DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedCertificateFileName);
            if (usingMtls)
            {
                await DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateFileName);
                await DownloadFile(apiEndpoint, serverSpecificDirectory, CertificateUtils.GeneratedClientCertificateKeyFileName);
            }
            return serverSpecificDirectory;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            throw new EvitaInvalidUsageException(ex.Message, $"Failed to download {(usingMtls ? "client" : "")} certificates from the server", ex);
        }
    }

    private static async Task DownloadFile(string apiEndpoint, string baseDir, string fileName)
    {
        using var client = new HttpClient();
        var response = client.GetAsync(apiEndpoint + fileName).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        await using Stream contentStream = await response.Content.ReadAsStreamAsync(),
            stream = new FileStream(Path.Combine(baseDir, fileName), FileMode.Create);
        await contentStream.CopyToAsync(stream);
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
