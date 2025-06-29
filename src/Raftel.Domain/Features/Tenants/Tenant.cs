using Raftel.Domain.Abstractions;
using Raftel.Domain.BaseTypes;
using Raftel.Domain.Features.Tenants.ValueObjects;
using Raftel.Domain.ValueObjects;

namespace Raftel.Domain.Features.Tenants;

public sealed class Tenant : AggregateRoot<TenantId>
{
    private Tenant()
    {
    }

    private Tenant(string name, Code code, string description, EncryptedString connectionString) : base(TenantId.New())
    {
        Name = name;
        Code = code;
        Description = description;
        ConnectionString = connectionString;
    }

    public string Name { get; set; }
    public Code Code { get; set; }
    public string Description { get; set; }
    public EncryptedString ConnectionString { get; set; }

    public static Result<Tenant> Create(string name, string code, string description, string connectionString = null)
    {
        var codeResult = Code.Create(code);
        if (codeResult.IsFailure)
        {
            return Result.Failure<Tenant>(codeResult.Error);
        }

        var encryptedConnectionString = EncryptedString.Create(connectionString ?? string.Empty);
        if (encryptedConnectionString.IsFailure)
        {
            return Result.Failure<Tenant>(encryptedConnectionString.Error);
        }

        return Result.Success(new Tenant(name, codeResult.Value, description, encryptedConnectionString.Value));
    }

    /// <summary>
    /// Gets the decrypted connection string for database operations.
    /// Returns null if no connection string is configured.
    /// </summary>
    /// <returns>The decrypted connection string, or null if empty.</returns>
    public string GetConnectionString()
    {
        if (ConnectionString is null || !ConnectionString.HasValue())
        {
            return null;
        }

        var decryptedValue = ConnectionString.GetDecryptedValue();
        return string.IsNullOrEmpty(decryptedValue) ? null : decryptedValue;
    }
} 