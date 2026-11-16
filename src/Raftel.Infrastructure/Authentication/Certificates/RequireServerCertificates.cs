using Microsoft.Extensions.Options;
using OpenIddict.Server;

namespace Raftel.Infrastructure.Authentication.Certificates;

/// <summary>
/// Fails when the OpenIddict server has no signing or encryption credentials, with a message that points to
/// <see cref="CertificateOptions"/>. Registered before OpenIddict's own validation so this message wins.
/// </summary>
internal sealed class RequireServerCertificates : IPostConfigureOptions<OpenIddictServerOptions>
{
    public void PostConfigure(string name, OpenIddictServerOptions options)
    {
        if (options.SigningCredentials.Count > 0 && options.EncryptionCredentials.Count > 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "The OpenIddict server needs both a signing and an encryption certificate. " +
            $"Configure them in the '{CertificateOptions.SectionName}' section (thumbprint or .pfx path) " +
            "or through the configureCertificates callback of AddRaftelData. " +
            "Development certificates are generated only in the Development environment.");
    }
}
