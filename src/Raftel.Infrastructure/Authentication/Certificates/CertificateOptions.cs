using System.Security.Cryptography.X509Certificates;

namespace Raftel.Infrastructure.Authentication.Certificates;

/// <summary>
/// Signing and encryption certificates used by the OpenIddict server to issue tokens.
/// </summary>
/// <remarks>
/// <para>
/// Bound from the <c>Authentication:Certificates</c> configuration section and then from the
/// <c>configureCertificates</c> callback of <c>AddRaftelData</c>.
/// </para>
/// <para>
/// Each certificate is loaded either from the X.509 store by thumbprint or from a PKCS#12 (.pfx) file.
/// Set one source per certificate, never both. Keep certificate passwords in a secret store
/// (user secrets, environment variables, a vault), never in a committed <c>appsettings.json</c>.
/// </para>
/// <para>
/// When no certificate is configured, ephemeral development certificates are generated only in the
/// <c>Development</c> environment. In any other environment the application fails at startup until both
/// certificates are configured, because development certificates are not shared between instances and
/// tokens issued by one replica would not validate on another.
/// </para>
/// </remarks>
public sealed class CertificateOptions
{
    /// <summary>Configuration section the options are bound from.</summary>
    public const string SectionName = "Authentication:Certificates";

    /// <summary>Thumbprint of the signing certificate in the X.509 store.</summary>
    public string SigningCertificateThumbprint { get; set; }

    /// <summary>Path to a PKCS#12 (.pfx) file containing the signing certificate and its private key.</summary>
    public string SigningCertificatePath { get; set; }

    /// <summary>Password of the signing certificate file.</summary>
    public string SigningCertificatePassword { get; set; }

    /// <summary>Thumbprint of the encryption certificate in the X.509 store.</summary>
    public string EncryptionCertificateThumbprint { get; set; }

    /// <summary>Path to a PKCS#12 (.pfx) file containing the encryption certificate and its private key.</summary>
    public string EncryptionCertificatePath { get; set; }

    /// <summary>Password of the encryption certificate file.</summary>
    public string EncryptionCertificatePassword { get; set; }

    /// <summary>Store searched for certificates configured by thumbprint. Defaults to <see cref="StoreLocation.CurrentUser"/>.</summary>
    public StoreLocation StoreLocation { get; set; } = StoreLocation.CurrentUser;

    internal CertificateSource Signing =>
        new("Signing", SigningCertificateThumbprint, SigningCertificatePath, SigningCertificatePassword);

    internal CertificateSource Encryption =>
        new("Encryption", EncryptionCertificateThumbprint, EncryptionCertificatePath, EncryptionCertificatePassword);

    internal bool AnyConfigured => Signing.IsConfigured || Encryption.IsConfigured;
}
