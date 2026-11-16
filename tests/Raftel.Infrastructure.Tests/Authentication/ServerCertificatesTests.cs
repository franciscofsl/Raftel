using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Server;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Infrastructure.Authentication.Certificates;

namespace Raftel.Infrastructure.Tests.Authentication;

public sealed class ServerCertificatesTests : IDisposable
{
    private const string CertificatePassword = "test-only";

    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"raftel-certs-{Guid.NewGuid():N}");

    public ServerCertificatesTests()
    {
        Directory.CreateDirectory(_directory);
    }

    [Fact]
    public void Development_WithoutConfiguredCertificates_ShouldUseDevelopmentCertificates()
    {
        var options = ResolveServerOptions(Environments.Development);

        options.SigningCredentials.ShouldNotBeEmpty();
        options.EncryptionCredentials.ShouldNotBeEmpty();
    }

    [Fact]
    public void Production_WithoutConfiguredCertificates_ShouldFailWithExplicitMessage()
    {
        var exception = Should.Throw<InvalidOperationException>(() => ResolveServerOptions(Environments.Production));

        exception.Message.ShouldContain("Authentication:Certificates");
    }

    [Fact]
    public async Task Production_WithoutConfiguredCertificates_ShouldFailAtHostStartup()
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production
        });
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["ConnectionStrings:Default"] = "Server=unused;Database=unused",
            ["Database:Provider"] = "SqlServer"
        });
        builder.Services.AddRaftelData<TestingRaftelDbContext>(builder.Configuration);
        using var host = builder.Build();

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => host.StartAsync());

        exception.Message.ShouldContain(CertificateOptions.SectionName);
    }

    [Fact]
    public void WithoutHostEnvironment_ShouldNotFallBackToDevelopmentCertificates()
    {
        Should.Throw<InvalidOperationException>(() => ResolveServerOptions(environmentName: null));
    }

    [Fact]
    public void Production_WithCertificateFiles_ShouldUseOnlyConfiguredCertificates()
    {
        var signing = CreateCertificateFile("CN=Raftel Signing");
        var encryption = CreateCertificateFile("CN=Raftel Encryption");

        var options = ResolveServerOptions(Environments.Production, new Dictionary<string, string>
        {
            ["Authentication:Certificates:SigningCertificatePath"] = signing.Path,
            ["Authentication:Certificates:SigningCertificatePassword"] = CertificatePassword,
            ["Authentication:Certificates:EncryptionCertificatePath"] = encryption.Path,
            ["Authentication:Certificates:EncryptionCertificatePassword"] = CertificatePassword
        });

        ThumbprintsOf(options.SigningCredentials.Select(credentials => credentials.Key))
            .ShouldBe([signing.Thumbprint]);
        ThumbprintsOf(options.EncryptionCredentials.Select(credentials => credentials.Key))
            .ShouldBe([encryption.Thumbprint]);
    }

    [Fact]
    public void Development_WithConfiguredCertificates_ShouldNotAddDevelopmentCertificates()
    {
        var signing = CreateCertificateFile("CN=Raftel Signing");
        var encryption = CreateCertificateFile("CN=Raftel Encryption");

        var options = ResolveServerOptions(Environments.Development, configureCertificates: certificates =>
        {
            certificates.SigningCertificatePath = signing.Path;
            certificates.SigningCertificatePassword = CertificatePassword;
            certificates.EncryptionCertificatePath = encryption.Path;
            certificates.EncryptionCertificatePassword = CertificatePassword;
        });

        options.SigningCredentials.Count.ShouldBe(1);
        options.EncryptionCredentials.Count.ShouldBe(1);
    }

    [Fact]
    public void Production_WithOnlySigningCertificate_ShouldFail()
    {
        var signing = CreateCertificateFile("CN=Raftel Signing");

        Should.Throw<InvalidOperationException>(() => ResolveServerOptions(
            Environments.Production,
            configureCertificates: certificates =>
            {
                certificates.SigningCertificatePath = signing.Path;
                certificates.SigningCertificatePassword = CertificatePassword;
            }));
    }

    [Fact]
    public void ConfiguredCertificateFileThatDoesNotExist_ShouldFailNamingThePath()
    {
        var missingPath = Path.Combine(_directory, "missing.pfx");

        var exception = Should.Throw<InvalidOperationException>(() => ResolveServerOptions(
            Environments.Production,
            configureCertificates: certificates =>
            {
                certificates.SigningCertificatePath = missingPath;
                certificates.EncryptionCertificatePath = missingPath;
            }));

        exception.Message.ShouldContain(missingPath);
    }

    [Fact]
    public void ConfiguredThumbprintThatIsNotInTheStore_ShouldFailNamingTheThumbprint()
    {
        const string thumbprint = "0000000000000000000000000000000000000000";

        var exception = Should.Throw<InvalidOperationException>(() => ResolveServerOptions(
            Environments.Production,
            configureCertificates: certificates =>
            {
                certificates.SigningCertificateThumbprint = thumbprint;
                certificates.EncryptionCertificateThumbprint = thumbprint;
            }));

        exception.Message.ShouldContain(thumbprint);
    }

    [Fact]
    public void CertificateWithPathAndThumbprint_ShouldFailAsAmbiguous()
    {
        var signing = CreateCertificateFile("CN=Raftel Signing");

        var exception = Should.Throw<InvalidOperationException>(() => ResolveServerOptions(
            Environments.Production,
            configureCertificates: certificates =>
            {
                certificates.SigningCertificatePath = signing.Path;
                certificates.SigningCertificateThumbprint = signing.Thumbprint;
            }));

        exception.Message.ShouldContain("Signing");
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    private static OpenIddictServerOptions ResolveServerOptions(
        string environmentName,
        IDictionary<string, string> settings = null,
        Action<CertificateOptions> configureCertificates = null)
    {
        var configurationValues = new Dictionary<string, string>
        {
            ["ConnectionStrings:Default"] = "Server=unused;Database=unused",
            ["Database:Provider"] = "SqlServer"
        };
        foreach (var setting in settings ?? new Dictionary<string, string>())
        {
            configurationValues[setting.Key] = setting.Value;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configurationValues).Build();

        var services = new ServiceCollection();
        if (environmentName is not null)
        {
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environmentName));
        }

        services.AddRaftelData<TestingRaftelDbContext>(
            configuration,
            configureCertificates: configureCertificates);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<OpenIddictServerOptions>>().CurrentValue;
    }

    private (string Path, string Thumbprint) CreateCertificateFile(string subject)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

        var path = Path.Combine(_directory, $"{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, CertificatePassword));
        return (path, certificate.Thumbprint);
    }

    private static string[] ThumbprintsOf(IEnumerable<SecurityKey> keys)
    {
        return keys.Select(key => key.ShouldBeOfType<X509SecurityKey>().Certificate.Thumbprint).ToArray();
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Raftel.Infrastructure.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
