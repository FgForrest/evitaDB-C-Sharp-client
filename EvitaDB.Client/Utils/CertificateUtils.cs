namespace EvitaDB.Client.Utils;

internal static class CertificateUtils
{
    private const string GeneratedCertificateExtension = ".crt";
    private const string GeneratedCertificateKeyExtension = ".key";
    public const string GeneratedCertificateFileName = $"server{GeneratedCertificateExtension}";
    public const string GeneratedClientCertificateFileName = $"client{GeneratedCertificateExtension}";
    public const string GeneratedClientCertificateKeyFileName = $"client{GeneratedCertificateKeyExtension}";
}
