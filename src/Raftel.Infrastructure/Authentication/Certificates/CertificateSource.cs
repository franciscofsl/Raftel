using System.Security.Cryptography.X509Certificates;

namespace Raftel.Infrastructure.Authentication.Certificates;

internal sealed record CertificateSource(string Purpose, string Thumbprint, string Path, string Password)
{
    private bool HasThumbprint => !string.IsNullOrWhiteSpace(Thumbprint);

    private bool HasPath => !string.IsNullOrWhiteSpace(Path);

    public bool IsConfigured => HasThumbprint || HasPath;

    public X509Certificate2 Load(StoreLocation storeLocation)
    {
        if (HasThumbprint && HasPath)
        {
            throw new InvalidOperationException(
                $"{Purpose} certificate is configured both by thumbprint and by file path in " +
                $"'{CertificateOptions.SectionName}'. Configure only one source.");
        }

        return HasPath ? LoadFromFile() : LoadFromStore(storeLocation);
    }

    private X509Certificate2 LoadFromFile()
    {
        if (!File.Exists(Path))
        {
            throw new InvalidOperationException($"{Purpose} certificate file '{Path}' was not found.");
        }

        var keyStorageFlags = OperatingSystem.IsMacOS()
            ? X509KeyStorageFlags.DefaultKeySet
            : X509KeyStorageFlags.EphemeralKeySet;

        return X509CertificateLoader.LoadPkcs12FromFile(Path, Password, keyStorageFlags);
    }

    private X509Certificate2 LoadFromStore(StoreLocation storeLocation)
    {
        using var store = new X509Store(StoreName.My, storeLocation);
        store.Open(OpenFlags.ReadOnly);

        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, Thumbprint, validOnly: false);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"{Purpose} certificate with thumbprint '{Thumbprint}' was not found in the " +
                $"{storeLocation}/{StoreName.My} certificate store.");
        }

        return matches[0];
    }
}
