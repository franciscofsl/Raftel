using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Raftel.Infrastructure.Authentication.Certificates;

internal static class ServerCertificates
{
    public static void Configure(OpenIddictServerBuilder server, CertificateOptions certificates, IHostEnvironment environment)
    {
        if (certificates.AnyConfigured)
        {
            AddConfiguredCertificates(server, certificates);
            return;
        }

        if (environment?.IsDevelopment() == true)
        {
            // Ephemeral, per-machine certificates: never valid outside Development.
            server.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
    }

    private static void AddConfiguredCertificates(OpenIddictServerBuilder server, CertificateOptions certificates)
    {
        if (certificates.Signing.IsConfigured)
        {
            server.AddSigningCertificate(certificates.Signing.Load(certificates.StoreLocation));
        }

        if (certificates.Encryption.IsConfigured)
        {
            server.AddEncryptionCertificate(certificates.Encryption.Load(certificates.StoreLocation));
        }
    }
}
