namespace Client.Utils;

public class CertificateUtils
{
    private const string GeneratedCertificateExtension = ".crt";
    private const string GeneratedCertificateKeyExtension = ".key";
    public const string GeneratedRootCaCertificateFileName = $"evitaDB-CA-selfSigned{GeneratedCertificateExtension}";
    public const string GeneratedCertificateFileName = $"server{GeneratedCertificateExtension}";
    public const string GeneratedClientCertificateFileName = $"client{GeneratedCertificateExtension}";
    public const string GeneratedClientCertificateKeyFileName = $"client{GeneratedCertificateKeyExtension}";
}